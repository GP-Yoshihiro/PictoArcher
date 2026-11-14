# Root Scene と重ねシーンの設計

日付：2026-11-14　対象：計画 P1-00 の追加（PR #5）
もとの規則：`docs/superpowers/specs/2026-11-14-coding-rules-design.md`（実行順と Main）

## 1. なぜ変えるか

今は `Main` を `Resources/Main.prefab` から作り、シーンを1つずつ丸ごと入れ替えている（`LoadSceneMode.Single`）。このやり方では、今のシーンを残したまま一時停止やダイアログを上に重ねられない。C さんの指示で、Unity でよく使う **Root Scene 方式**にする。消えない Root Scene を1つ置き、ほかのシーンはすべてその上に加算で読み込む。

## 2. 決めたこと（C さんが選んだもの）

| 項目 | 決定 |
|---|---|
| 方式 | Root Scene を作り、そこからシーンを読み込む・重ねる |
| スクリプト | `Main`、`SceneTransform`（シーンの管理）、`SceneBase`（親）、各シーン（`SceneBase` を継承）。必要なら足してよい |
| シーン | Root、Home、Battle、Result が最低限。今の Boot は Root に、Run は Battle に名前を変える |
| Result | Battle と入れ替える（Battle を閉じて Result を開く）。Result から 🏠 で Home、↻ で Battle |
| 重ねシーンの例 | 一時停止（Pause）を作る。Battle に ⏸、Pause に ▶（戻る）と 🏠（ホームへ） |
| 重ねている間の下のシーン | 止める（Tick を呼ばない、`Time.timeScale` を 0）。重ねシーンの側で「下を動かしたまま」にもできる |
| 結果の返し方 | 重ねシーンは閉じるときに整数を1つ渡せる。下のシーンは「どの重ねシーンが、どの結果で閉じたか」を受け取る |

## 3. 方式（検討したもの）

| 方式 | 内容 | 判断 |
|---|---|---|
| A. Root Scene ＋ 加算読み込み | Root を消さずに置き、Home などを加算で入れ替え、Pause などを上に積む | **採用**（C さんの指示） |
| B. 今の方式に重ねるだけ足す | `Main` は `DontDestroyOnLoad`、土台は `Single` で入れ替え、重ねシーンだけ加算 | 動くが、共有のカメラや EventSystem を置く場所がなく、Root がないので採らない |
| C. ダイアログをプレハブで出す | 今のシーンの中で表示を切り替える | A と両立する。カード選びなどの小さなものはこちらでもよい |

## 4. 仕組み

### 4.1 シーンの3つの段

```
重ねシーン（Pause など）   … 加算。いくつでも積める（同じものは1つまで）
中身のシーン（Home／Battle／Result） … 加算。同時に1つ。入れ替える。アクティブなシーン
Root（消えない）           … Main、RootScene、カメラ、EventSystem
```

- **Root.unity**（Build Settings の 0 番）：ルートに `Main`、`RootScene`、`MainCamera`（プレハブ）、`EventSystem` を置く。ここだけがカメラ、`AudioListener`、`EventSystem` を持つ。Root は消さない。
- **中身のシーン**（`SceneId { Home, Battle, Result }`）：入れ替えるたびに、前のシーンを外して（`UnloadSceneAsync`）、次を加算で読み込み、**アクティブなシーン**にする（`Instantiate` したものが中身のシーンに入り、入れ替えで一緒に消えるため）。
- **重ねシーン**（`OverlayId { Pause }`）：加算で読み込む。アクティブなシーンは変えない。
- Build Settings の順は Root、`SceneId` の順、`OverlayId` の順（Root、Home、Battle、Result、Pause）。
- どのシーンも、一番上の階層に `SceneBase` をちょうど1つ置く（Root には `RootScene`）。名前は `Scene` で終わる。

### 4.2 スクリプトの役割

| クラス | 置き場所 | 役割 |
|---|---|---|
| `Main` | Root.unity のルート（`Scripts/Game/Main/`） | Unity の標準ライフサイクルを書く唯一のクラス。時計を進め、`SceneTransform` を呼び、`Time.timeScale` を決める |
| `SceneTransform` | `Main` が作る普通のクラス（`sealed`、`Scripts/Game/Main/`） | シーンの読み込み・外し・重ね、`SceneBase` の `Init`／`Tick`／`Dispose` を呼ぶ。`Main` と並んで `SceneManager` を使ってよい唯一のクラス |
| `SceneFlow<TScene, TOverlay>` | `Scripts/Core/Common/`（テスト対象） | 依頼の受け付けと状態（空／動いている／読み込み中、開いている重ねシーンの一覧）。Unity を使わない |
| `GameContext` | `Scripts/Game/Main/` | シーンから使う窓口。`RequestScene`、`OpenOverlay`、`CloseOverlay`、`Clock` |
| `SceneBase` | `Scripts/Game/Main/` | 親クラス。`Init`／`Tick`／`Dispose`（必須）、`FixedTick`／`LateTick`／`KeepsBelowRunning`／`OnOverlayClosed`（書かなくてよい） |
| `RootScene` | Root.unity のルート（`Scripts/Game/Scenes/`） | **初期化用のシーン**。どこから再生しても、ほかのどのシーンより先に `Init` される（設定やセーブの読み込みなど、ゲーム全体の準備はここに書く）。最初に開く中身のシーン（`_firstScene`）を持つ。Root と一緒にずっと動き、ゲームの終わりに最後に `Dispose` される |
| `HomeScene`、`BattleScene`、`ResultScene`、`PauseScene` | `Scripts/Game/Scenes/` | 各シーン |

- `Resources/Main.prefab` と、起動時に `Main` を作る仕組みはやめる。`Main` は Root.unity に置く。Root は消えないので `DontDestroyOnLoad` も要らない。
- 呼ぶ順は今までどおり `Main` → シーン → System（間に `SceneTransform` が入る）。

### 4.3 依頼（`GameContext`）
- `RequestScene(SceneId)`：中身のシーンを入れ替える。開いている重ねシーンは全部閉じる（上から順に `Dispose`。`OnOverlayClosed` は呼ばない）。
- `OpenOverlay(OverlayId)`：重ねシーンを開く。
- `CloseOverlay(int result = 0)`：一番上の重ねシーンを閉じ、すぐ下のシーン（重ねシーンまたは中身のシーン）の `OnOverlayClosed(OverlayId, int)` を呼ぶ。
- 依頼は1フレームに1つ。読み込み中、同じ重ねシーンがもう開いているとき、閉じる重ねシーンがないときは false を返す。

### 4.4 毎フレームの順番（`Main.Update`）
1. 時計を進める。
2. `SceneTransform`：読み込みが終わったシーンを始める（`Init`）。外からアクティブなシーンが変えられていたら、それに合わせる（4.6）。
3. `SceneTransform`：動かす範囲のシーンを、下から順に Tick（Root → 中身 → 重ね1 → 重ね2）。始めたばかりのシーンはそのフレームは Tick しない。
4. `SceneTransform`：依頼を1つ処理する（入れ替え・開く・閉じる）。
5. `Time.timeScale`：中身のシーンが止まっている、または読み込み中なら 0。それ以外は `GameClock` の倍率。

- **動かす範囲**：Root はいつも動かす。一番上の重ねシーンはいつも動かす。その下は、上の重ねシーンの `KeepsBelowRunning` が true の間だけ動かす。
- **渡す時間**：中身のシーンには `Time.deltaTime`（止まると 0）。Root と重ねシーンには `Time.unscaledDeltaTime`（止まっていても動ける）。
- `FixedTick` は中身のシーンだけ（`Time.timeScale` が 0 のあいだは Unity が `FixedUpdate` を呼ばないため）。`LateTick` は Tick と同じ範囲。

### 4.5 閉じる・外す
- 外すシーンは、`Dispose`（例外は記録して先へ進む）→ ルートを非表示 → `UnloadSceneAsync`（終わるのを待たない）。
- 読み込みに失敗したとき（Build Settings にない）はエラーを出し、重ねシーンなら取り消し、中身のシーンなら何もない状態で Root だけ動かす。
- 読み込んだシーンの一番上の階層に `SceneBase` がちょうど1つないときは、エラーを出す（重ねシーンなら外して取り消す）。

### 4.6 エディタで Root 以外から再生したとき
- `Main` の `[RuntimeInitializeOnLoadMethod(AfterSceneLoad)]` が、Root が読み込まれていなければ Root を加算で読み込む。
- どこから再生しても、最初に `RootScene.Init` が終わってから、ほかのシーンの `Init` が呼ばれる（必ず Root から始まる）。
- Root の `SceneTransform` は、すでに開いているシーン（Home や `Sandbox_A` など）を中身のシーンとして始める。Root から再生したとき（ビルドも同じ）は Home を読み込む（最初のシーンは `RootScene` の `[SerializeField] SceneId _firstScene` で、既定は Home）。
- 再生中に外から（テストやエディタが）アクティブなシーンを変えたら、今の中身と重ねシーンを片付けて外し、新しいアクティブなシーンを中身として始める。保存していない無題のシーンで `SceneBase` がないときは警告だけ（今と同じ）。

### 4.7 シーンを作るときの規則
- Root 以外のシーンには、カメラ、`AudioListener`、`EventSystem` を置かない（テストで止める）。
- UI は Screen Space Overlay の Canvas。重ねシーンは Sort Order を大きくし（Pause は 100）、全面に半透明の板を敷いて下のボタンを押せないようにする。
- 重ねシーンで `Instantiate` したものは、そのままだと中身のシーンに入る。親に自分のルートの Transform を渡して作る。
- 重ねシーンと Root の絵の動き（Animator など）は Unscaled Time にする。
- `Global Light 2D` は今の Run と同じく Battle に置く（UI だけのシーンには要らない）。

## 5. 作らないもの
- Battle から Result へ進む条件（戦いの終わり）。それを作るタスクで `RequestScene(SceneId.Result)` を1行足す。それまでは PlayMode テストで Result を確かめる。
- Esc キーで一時停止、シーンを替えるときのフェードなどの演出、整数より大きな結果。

## 6. 確かめ方
- EditMode：`SceneFlowTests`（重ねる・閉じる・結果・入れ替えで全部閉じる・同じものは開けない・空で閉じられない・取り消し・やり直し）、`SceneSetupTests`（Build Settings の順、各シーンの `SceneBase`、Root にだけ `Main` とカメラと `EventSystem`、ボタンのつなぎと文字の子がない）、`ArchitectureGuardTests`（`SceneManager` は `Main` と `SceneTransform` だけ）。
- PlayMode：Root → Home → ▶ → Battle。Battle で ⏸ → Pause（Battle が止まり `timeScale` 0）→ ▶ で戻る。⏸ → 🏠 で Home（Pause も消える）。Result の 🏠 と ↻。重ねシーンの結果が下に届く。`Dispose` の例外があっても次へ進む。

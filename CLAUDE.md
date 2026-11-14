# PictoArcher：Claude へのルール

3人の Claude が同じルールで動くための文書。Unity 6000.5.10f1（URP 2D）。
開発ルールの正本は `docs/development-rules.md`。仕様の一覧・工程・工数は `docs/PictoArcher_仕様書.xlsx`。

## 作業を始める前に（毎回・必ず）

3人とも Claude を使うので、同じ作業を2つの Claude が始めないように、コードやファイルを触る前に次を行う。

1. 次を実行して結果を見る。
   ```bash
   git fetch --prune
   gh issue list --state open
   gh pr list --state open
   git branch -r
   ```
2. 頼まれた作業の Issue を探す（タイトルは `[作業工程の ID] タスク名`）。
   - Issue の担当（assignee）がほかの人なら、作業を始めずに人に伝える。
   - 同じファイルを触る PR（下書きを含む）が開いているなら、作業を始めずに人に伝える。
   - Issue がないなら、作ってよいか人に聞く。
3. 自分の Issue の担当になってから、`develop` から `<プレフィックス>/t<Issue番号>-<短い英語>` のブランチを切り（下の「シーンとブランチ」）、下書きの PR（本文に `Closes #<番号>` と触るファイル）を開く。push と PR の作成は人の OK をもらってから。
4. 仕様書「作業工程」シートの「先に要るもの」がマージ済みか確かめる。まだなら人に伝える。
5. 作業中は Issue に書いてあること以外をしない。担当外のファイル（ほかの人のプレハブ、`Battle.unity` など）を直したくなったら、直さずに人に伝える。

1セッション＝1タスク。別のタスクに移るときは `/clear` してから。計画が要る作業は Plan Mode で計画を作り、人が読んでから進める。

## 実行順と設計の規則

C# を書く・直す・レビューする前に、スキル `pictoarcher-coding-rules` を読む。正本は `docs/superpowers/specs/2026-11-14-coding-rules-design.md`。Unity の落とし穴は `docs/unity-pitfalls.md`。

- Unity の標準ライフサイクル（`Awake`、`Start`、`Update`、`FixedUpdate`、`OnEnable`、`OnDestroy` など）を書くのは `Main` だけ。`Main` → シーン（`SceneBase` の `Init`／`Tick`／`FixedTick`／`LateTick`／`Dispose`）→ System の順に、ソースに書いた順で呼ぶ。
- シーンはシーンの一番上の階層にちょうど1つ置く。`Root.unity` は消えない土台で、ほかのシーンは `SceneTransform` が加算で読み込む（入れ替えは `RequestScene`、重ねるのは `OpenOverlay`／`CloseOverlay`）。`SceneManager` を使ってよいのは `Main` と `SceneTransform` だけ。System はシーンの `Init` で作り、`Tick` で1行ずつ呼び、`Dispose` で逆順に片付ける。
- 当たり判定は `CollisionRelay2D`、ボタンは `UiButtonRelay` が受け取るだけ。処理は Tick の中。入力は Tick の中で読む。
- 禁止：`Find` 系、`Camera.main`、コルーチン、`Invoke`、`async`、シングルトン、static の書き換えられる値、インスペクターで onClick に関数をつなぐこと、`Time.` を `Main` 以外で読むこと。
- 継承は骨組み（`SceneBase`、`DefinitionAsset<T>`）だけ。ほかは `sealed`。敵は定義（ScriptableObject）と動く部品の組み合わせで作る。
- ScriptableObject は `[SerializeField] _camelCase` ＋ 読み取り専用のプロパティ。実行中に書き換えない。
- 古い計画（計画1 のタスク8〜10 など）のコードの `Update`、`OnTriggerEnter2D`、static の `EnemyRegistry`、public のフィールドは写さない。

## 文字ゼロのルール

画面に文字を出さない。設計書 §5 のとおり。

| 項目 | ルール |
|---|---|
| 禁止 | アルファベット・ひらがな・カタカナ・漢字すべて。`LV` `HP` `BOSS` `x2` `s`（秒）などの略語・単位も禁止 |
| 代わりの表記 | レベル `★5`／倍率 `×2`／時間 `⏱ 30`／割合 `+20%`／進行 `23/50` |
| 色の意味 | 赤＝危険・攻撃／緑＝回復／黄＝コイン・経験値／青＝防御・氷／紫＝レア。この5色は UI・拾うもの・エフェクトの意味だけに使い、敵の体の主な色やフィールドの照明には使わない（敵は灰・白・黒・茶・桃などから選ぶ。プレイヤーの弾は白） |
| 予兆 | 敵の攻撃は必ず先に赤い線・赤い円で予告する |
| 操作の案内 | 手・矢印キー・マウスの絵のアニメで見せる。キーの絵に英字（W A S D、Esc など）を描かない |
| 絵文字 | フォントで出さない。すべてスプライト画像（UI の絵）で用意する |
| フォント | TextMeshPro のフォントアセットには数字と記号だけを登録する。UI の絵に合う、読みやすいフォントを使う（ピクセルフォントでなくてよい） |
| ボタン | ▶ 開始／⏸ 一時停止／↻ やり直し／🏠 ホーム／⚙ 設定／🔒 ロック中／🔊 音量 |
| 素材 | 取り込む前に文字が入っていないか確認する（UIパックの「PLAY」、看板、ロゴ、背景の張り紙や画面の表示など） |
| ウィンドウ・起動画面 | ウィンドウタイトルとアプリ名は OS が表示するもので画面内ではないため対象外。Unity のスプラッシュ画面は、出せない場合の扱いをチームで判断する |

- 開発ビルドやデバッグ表示でも文字を出さない。ログはコンソールだけ。
- 描画の基準解像度は 480×270。1ユニット＝16px（Assets PPU 16）。キャラとフリー素材の画像は Filter Mode を Point、圧縮なしで取り込む。
  - 注：設計書 第3版で画面は縦長（基準 270×480）に決まった。切り替えは計画2のタスク0（作業工程 `P2-00`、C さん）で行い、そのときこの行も直す。それが `develop` に入るまでは今の 480×270 のまま作業し、縦長を前提にしたプレハブを作らない。
- ドット絵にするのはキャラクター（プレイヤー、敵、中ボス、ボス）だけ。すべて 32×32 で描く（2×2 ユニット）。フリー素材（タイル、障害物、エフェクト）は大きさを縛らない。
- カード、アイコン、ボタン、ピクトグラムなどの UI は、ドット絵ではない普通の2Dの絵（Filter Mode は Bilinear）。カードは縦長。UI は uGUI の Canvas（Canvas Scaler は Scale With Screen Size、基準 1080×1920）に置く。
- のちに iPad・iPhone へ対応させるので、画面は 9:16 で作り、UI は画面の端を基準に置いて端に余白をとる（対応そのものは8週間の範囲外）。
- コードの名前は世界観に依存させず、役割で付ける（弾は `Projectile`、追う敵は `Chaser`）。

## フォルダ構成

```
CLAUDE.md                  全員の Claude が読むルール
.claude/                   共有の Claude 設定（settings.json、skills/、agents/）
.github/                   PR と Issue のテンプレート
Assets/_Project/
  Scripts/Core/            PictoArcher.Core（純粋な C#、テスト対象）
    PictoArcher.Core.asmdef
    Common/                ITickable, IFixedTickable, ILateTickable, TickList, SceneFlow, SceneFlowPhase
    Combat/                TargetSelector, FireTimer, Health, HitInvulnerability
    Movement/              MoveVectorResolver
    Waves/                 WaveLayout, WaveKind, WaveProgress, WaveSequence, WavePhase
    Skills/                強化の計算（重ね取り、カードの抽選など）
    Extensions/            拡張メソッド（必要になったら作る）
  Scripts/Game/            PictoArcher.Game（MonoBehaviour と ScriptableObject）
    PictoArcher.Game.asmdef
    Main/                  Main, GameContext, GameClock, SceneId, OverlayId, SceneBase, SceneTransform
    Scenes/                RootScene, HomeScene, BattleScene, ResultScene, PauseScene
      Sandbox/             各自の試し用のシーンのクラス（SandboxAScene など）
    Common/                CollisionRelay2D, UiButtonRelay, DefinitionAsset, Layers, IHittable
    Data/                  PlayerStats, EnemyDefinition, WaveDefinition, SpawnEntry, SkillId, SkillDefinition
    Enemies/               EnemyRegistry、敵の動く部品（Chaser、Shooter など）
    Player/                プレイヤーの移動・射撃
    Projectiles/           弾（Projectile、敵の弾）
    Field/                 フィールド・障害物の配置
    Run/                   1回のプレイの進行（ウェーブ、レベルアップ、経験値）
    UI/                    HUD、カード選択、View
    Extensions/            拡張メソッド（必要になったら作る）
  Scripts/Editor/          PictoArcher.Editor（エディタ専用）
    PictoArcher.Editor.asmdef
    PixelArtImportPostprocessor.cs  ドット絵（キャラとフリー素材）の取り込み設定
  Tests/EditMode/          PictoArcher.Tests.EditMode（ArchitectureGuardTests を含む）
  Tests/PlayMode/          PictoArcher.Tests.PlayMode（Root → Home → Battle → Result と Pause の流れ）
  Data/                    ScriptableObject のアセット
    Player/                PlayerStats
    Enemies/ Waves/ Skills/ Obstacles/   EnemyDefinition、WaveDefinition、SkillDefinition、ObstacleLayout
  Prefabs/
    System/                MainCamera
    Player/ Enemies/ Projectiles/ Field/ UI/   それぞれのタスクで足す
  Scenes/Root.unity          消えない土台（Main、RootScene、カメラ、EventSystem）。Build の 0 番
  Scenes/Home.unity, Battle.unity, Result.unity, Pause.unity   Root に加算で読み込む。Cさんだけが編集する（Build Settings は Root から この順）
  Scenes/Sandbox/          Sandbox_A.unity, Sandbox_B.unity（各自の試し用、他の人は触らない）
  Fonts/                   数字と記号だけのフォント
  Art/                     絵
    Characters/            キャラのドット絵 32×32（プレイヤー、敵、中ボス、ボス。Point）
    Tiles/                 床・壁・障害物のフリー素材
    Effects/               エフェクト
    UI/                    カード、アイコン、ボタン、ピクトグラム（ドット絵ではない。Bilinear）
  Audio/
    BGM/ SE/
tools/run-editmode-tests.sh
tools/run-playmode-tests.sh
tools/spec/build_spec_xlsx.py  仕様書 .xlsx を作るスクリプト
docs/assets-list.md
docs/development-rules.md  開発ルールの正本
docs/unity-pitfalls.md     Unity の落とし穴（初めての人向け）
docs/PictoArcher_仕様書.xlsx  仕様書（スクリプトで作る。直接書き換えない）
docs/superpowers/          設計書（specs/）と計画（plans/）
```

ルール：
- `Scripts/Core` は純粋な C#。Unity の部品を使わない（使ってよいのは `Vector2`、`Mathf` などの数学の型だけ）。名前空間は `PictoArcher.Core`。
- Unity の部品（`MonoBehaviour`、`ScriptableObject`）は `Scripts/Game` に置く。名前空間は `PictoArcher.Game`。
- エディタ専用のコードは `Scripts/Editor` に置く。名前空間は `PictoArcher.Editor`。
- 新しいファイルは上のどれかのフォルダに入れる。`Assets/_Project` や `Scripts/Game` の直下に直接置かない。合うフォルダがなければ人に聞く。空のフォルダには `.gitkeep` を入れて git に残す（中身ができたら消してよい）。
- 名前空間はフォルダで分けない（`Scripts/Game/Main/` の中も `PictoArcher.Game`）。1ファイルに1つの型、ファイル名＝型名。
- レイヤー：`Player`=6、`Enemy`=7、`PlayerShot`=8、`Wall`=9（障害物も `Wall`）。
- `Prefabs/System/MainCamera.prefab` の位置は (0, 0, -10) から動かさない。カメラは動かない。
- 数値（HP、攻撃力、速度、出現数）は ScriptableObject の調整値にする。コードに直接書かない。

## テストの書き方

- `Core` のルールは、先に EditMode テストを書いて失敗を確認してから実装する。
- NUnit を使う。置き場所は `Assets/_Project/Tests/EditMode/<クラス名>Tests.cs`。
- 実行方法：Unity のエディタを閉じて、`tools/run-editmode-tests.sh`。失敗があると終了コードが 0 以外になる。結果は `Logs/editmode-results.xml`。
- シーンの流れ（Root → Home → Battle → Result、Pause の開け閉め）の確認は `tools/run-playmode-tests.sh`。結果は `Logs/playmode-results.xml`。シーンや `Main` を触ったときに実行する。
- `ArchitectureGuardTests` は実行順・やり取り・命名の規則を確かめる。落ちたらメッセージの直し方に従う。テストの例外を足して通さない（足すなら人に伝え、PR に理由を書く）。
- `NoTextGuardTests` を含む全テストが通ってからプルリクエストを出す。

## シーンとブランチ

- `Root.unity`・`Home.unity`・`Battle.unity`・`Result.unity`・`Pause.unity` を編集するのは Cさんだけ。ほかの人はプレハブと `Scenes/Sandbox/Sandbox_<担当>.unity` で作業する。Sandbox のシーンにも自分の `SceneBase`（例 `SandboxAScene`）を1つ置く（置かないと何も動かない。書き方はスキル `pictoarcher-coding-rules`）。
- ブランチは `develop` から切り、PR も `develop` 向けに出す。`main` はリリースのときだけ `develop` から入れる（C さん）。
- ブランチ名は `<プレフィックス>/t<Issue番号>-<短い英語>`（例 `feature/t12-weapon-select`）。作業の種類がわかるようにする。1タスク＝1 Issue＝1ブランチ。
  - `feature/` 新機能／`fix/`（または `bugfix/`）バグの修正／`hotfix/` 本番の緊急修正／`release/` リリースに向けた調整・バージョンアップ／`refactor/` 動きを変えずにコードを整理／`docs/` 文書の修正・追加
- `develop` と `main` へは直接入れない。ほかの1人が確認してからマージする。`develop` へのマージでは Issue が自動で閉じないので、マージしたら `gh issue close <番号>` で閉じる。
- Claude が書いたコードも、マージ前に人が読む。
- PR は `.github/pull_request_template.md` の項目を埋める。コミットの種類、レビュー、マージの方法は `docs/development-rules.md` §6〜§8。
- 仕様を変えるときは、設計書（`.md`）と `tools/spec/build_spec_xlsx.py` を同じ PR で直し、`.xlsx` を作り直す（§10）。

## Claude の動かし方（ハイブリッド運用）

- メインのセッションは1タスクの計画・判断・統合をする。サブエージェントは同時に2〜3体まで。
  - `log-triage`（haiku）：長いログやテスト結果から失敗と原因だけを抜き出す。
  - `transcriber`（haiku）：計画にコードが全部書いてあるタスクを写して、テストして、コミットする。
  - `task-reviewer`（sonnet）：1タスクが終わるごとに、仕様に合っているかとコードの質を見る。
- 同じファイルを触るサブエージェントを同時に動かさない。
- スキル `minimal-read-edit`（必要な所だけ読んで直す）と `lean-token-workflow`（長いログやサブエージェントの使い方）を使う。プラグインは `superpowers`。
- フックは共有しない。
- UnityYAMLMerge は各自で設定する（`README.md` を見る）。

# コーディング規則と実行順の土台 設計書

- 日付：2026-11-14
- 作業工程：P1-00（Issue #4）
- 担当：C（山田）
- 関係する文書：`docs/development-rules.md`（開発ルールの正本）、`CLAUDE.md`、設計書 第3版（`2026-10-31-pictoarcher-design-v3.md`）

## 1. 意図

PictoArcher は3人のプログラマーで作る。山田さん以外の2人（肴場さん・洞さん）は Unity が初めてで、Claude を使って書く。プログラムの基礎はあっても、Unity の仕組み（Update の呼ばれ方、シーン、プレハブ、ScriptableObject、当たり判定の通知など）を知らない。

この状態で各自が Claude にコードを書かせると、次のことが起きやすい。

- 人によって（Claude によって）作り方が違い、つなぐときに合わない
- Unity のよくある書き方（各スクリプトの Update、Find、シングルトン、コルーチン）が混ざり、実行順が読めなくなる
- 敵の種類が多いので、敵ごとにクラスをコピーして増やし、直すところが散らばる

そこで、開発の前に「考え方」をそろえる。細かい仕様（敵ごとの部品の割り当てなど）はここでは決めない。決めるのは、どの人・どの Claude が書いても同じ形になるための骨組みと規則と、規則が破られたらすぐに分かる仕組み。

さらに、実行順の土台（Main と Scene の仕組み）は今日の時点で動く形まで作る。

## 2. ゴール・やらないこと・失敗とみなすこと

ゴール
- 実行順が1か所から読める。Unity の標準ライフサイクルを使うのは `Main` だけで、`Main → シーン → 各クラス` の順に上から呼ぶ。
- 規則違反は EditMode テストで自動的に見つかり、直し方が日本語で出る。
- Claude が規則とお手本を読んでから書く（スキル、CLAUDE.md、レビュー担当の確認項目）。
- Unity の落とし穴を一覧にして、初めての人が同じ穴に落ちない。
- Main とシーンの切り替え（Boot → Home → Run）が動く。

やらないこと
- 敵ごとの部品の割り当て、ウェーブの中身、UI の画面設計などの仕様の細部
- 敵の移動・攻撃などの interface の具体的な形（敵のタスクで決める。ここでは考え方とお手本だけ）
- 外部ライブラリ（DI コンテナ、UniTask、DOTween など）の導入
- 縦長画面への切り替え（P2-00 の仕事）

失敗とみなすこと
- 規則が文書にしかなく、破っても誰も気づかない
- 規則が多すぎ、または厳しすぎて、普通の作業のたびに例外を足すことになる
- 骨組みが重く、初めての人が1つの敵を足すのに多くのファイルを読まないといけない

## 3. 制約

- Unity 6000.5.10f1、URP 2D。asmdef は Core ← Game ← Editor（Tests は全部を参照）。
- ドメインのリロードが無効（`EnterPlayModeOptions` = 1）。static の値が再生をまたいで残る。
- `Run.unity` を編集するのは C さんだけ。ほかの人はプレハブと Sandbox シーンで作業する。
- 数値は ScriptableObject の調整値にする。Core は純粋な C# で、先にテストを書く。
- 文字ゼロのルール（画面に文字を出さない）。
- テストは Unity のエディタを閉じて `tools/run-editmode-tests.sh` で回す。

## 4. やり方の比較（Approaches）

| 案 | 中身 | よい点 | 弱い点 |
|---|---|---|---|
| A. 文書だけ | 規則を書き、レビューで守る | すぐできる | 初めての人や Claude は文書を読み飛ばす。破られてもレビューまで気づかない |
| **B. 文書＋自動チェック＋お手本＋落とし穴＋レビュー項目** | 規則を書き、破れば EditMode テストが落ちる。お手本のコードと Unity の落とし穴の一覧を Claude に読ませる | 破った時点で分かる。Claude が同じ形で書く | ガードテストを作る手間。誤検知が出たら直す必要がある |
| C. 外部の仕組みを入れる | DI コンテナ（VContainer など）や UniTask で実行順と依存を管理する | 仕組みとしては強い | Unity が初めての人に、Unity と別の仕組みを同時に覚えさせることになる。中で何が起きているか見えにくい |

**B を選んだ**（山田さんの選択）。理由：初めての人が Claude を使うので、「読めば分かる」だけでは足りず、「破ればテストが落ちる」が要る。一方で外部の仕組みは学ぶものを増やすので入れない。

## 5. 考え方（8項目）

ここが規則の中心。開発ルール（`docs/development-rules.md`）にも同じ8項目を短く書く。

### 5.1 層と依存の向き
- `Core`（純粋な C#）← `Game`（Unity の部品）← `Editor`。逆向きの参照は asmdef で禁止されている。
- Core で使ってよい UnityEngine のものは、数学の型（`Vector2`、`Vector2Int`、`Vector3`、`Mathf`、`Rect`）だけ。`MonoBehaviour`、`GameObject`、`Transform`、`Time`、`Physics2D`、入力、`Debug` などは使わない。
- 名前空間は3つだけ：`PictoArcher.Core`、`PictoArcher.Game`、`PictoArcher.Editor`（テストは `PictoArcher.Tests.EditMode`）。フォルダを分けても名前空間は分けない。

### 5.2 実行順
- Unity の標準ライフサイクル（`Awake`、`Start`、`Update`、`FixedUpdate`、`LateUpdate`、`OnEnable`、`OnDisable`、`OnDestroy` など）を書いてよいのは `Main` だけ。
- `Main` が今のシーンの `SceneBase` を呼び、シーンが自分の持つクラスを呼ぶ。呼ぶ順番はソースに1行ずつ書く。
- 初期化（`Init`）は上から下へ、片付け（`Dispose`）はその逆順。
- 待ち時間はコルーチンや async ではなく、Tick で進めるタイマー（Core）で作る。
- 時間（`Time.deltaTime` など）を読むのは `Main` だけ。`deltaTime` は引数で上から渡す。

### 5.3 Unity が直接呼ぶものの扱い
Unity は当たり判定・UI のクリック・入力のコールバックを、こちらの実行順と関係なく呼ぶ。そこで「受け取るだけ」の部品を挟み、処理はシーンの Tick の中で行う。
- 当たり判定：`CollisionRelay2D` が記録するだけ。System が Tick で取り出して処理する。
- UI のボタン：`UiButtonRelay` が「押された」を記録するだけ。シーンや System が Tick で取り出す。インスペクターで onClick に関数をつなぐのは禁止。
- 入力（Input System）：Tick の中で値を読む（`ReadValue`、`WasPressedThisFrame`）。`performed +=` などのコールバックは使わない。

### 5.4 組み合わせで作る（継承は骨組みだけ）
- 自分たちのクラスを継承してよいのは骨組みだけ：`SceneBase`、`DefinitionAsset<T>`、`DefinitionAsset<T>` を直接継承した役割ごとの抽象クラス。
- それ以外のクラスは `sealed`。似たものを増やすときは、継承ではなく部品を組み合わせる。
- 敵は「データで組み合わせる」：部品は「定義」（ScriptableObject。`Create()` で動く部品を作る）と「動く部品」（普通の C#。自分の状態を持ち、Tick で動く）の2つで作る。敵の定義が移動・攻撃などの定義を持ち、共通の流れを回すクラスがそれを動かす。どの敵がどの部品を持つかは敵のタスクで決める。
- interface は小さく、1つの役割に1つ。拡張メソッドは状態を持たない関数だけにして `XxxExtensions` にまとめる。

### 5.5 データと状態を分ける
- ScriptableObject は読み取り専用のデータ。実行中に書き換えない（エディタでは書き換えた値がアセットに残る）。
- フィールドは `[SerializeField] private` にして、読み取り専用のプロパティで公開する。public のフィールドと public の setter は禁止。
- 実行中に変わる値（今の HP、残り時間など）は、データから作った普通の C# のオブジェクトが持つ。

### 5.6 やり取り
- 参照は上から渡す：普通の C# はコンストラクターで、MonoBehaviour は `Init(...)` で受け取る。シーンの中の MonoBehaviour は、シーンのインスペクターでつなぐ。
- 禁止：`Find` 系、`Camera.main`、`SendMessage`、シングルトン、static の書き換えられる値（Main も static の状態を持たない）。
- 下から上へはイベント（C# の `event`）か戻り値で伝える。イベントは `Init` で登録し、`Dispose` で外す。

### 5.7 名前とフォルダ
§9 と §10 にまとめる。

### 5.8 今あるコードへの影響
§14 にまとめる。

## 6. 実行順の仕組み（今日作るもの）

### 6.1 全体

```
Unity
 └ Main（MonoBehaviour。標準ライフサイクルを使う唯一のクラス）
     Update      → 今のシーン.Tick(dt)
     FixedUpdate → 今のシーン.FixedTick(fixedDt)
     LateUpdate  → 今のシーン.LateTick(dt)
     └ BootScene / HomeScene / RunScene / Sandbox のシーン（SceneBase を継承）
         Init(context) → 持っている System を作り、上から順に Init
         Tick(dt)      → System を1行ずつ順に Tick
         Dispose()     → 逆順に Dispose
         └ 各クラス（System、View、Relay、Core のルール）
```

### 6.2 Main
- `Assets/_Project/Resources/Main.prefab` に置く。`[RuntimeInitializeOnLoadMethod(BeforeSceneLoad)]` で、どのシーンから再生しても最初に作られ、`DontDestroyOnLoad` で残る。
- `[DefaultExecutionOrder(-10000)]` を付け、ほかの Unity の部品（EventSystem など）より先に Update する。
- static の状態を持たない（ドメインのリロードが無効でも問題が出ない）。
- 今のシーンの `SceneBase` は、シーンの一番上の階層の GameObject から探す（`Scene.GetRootGameObjects()` と `GetComponent`）。1つでなければエラーをコンソールに出し、何もしない。
- 1フレームの流れ（Update）
  1. 時計（`GameClock`）を更新する（スケールなしの経過時間など）
  2. 読み込み中なら、読み込みが終わったかを見る。終わっていればシーンを探して `Init(context)` を呼ぶ（Tick は次のフレームから）
  3. シーンが動いていれば `Tick(Time.deltaTime)`
  4. 切り替えの依頼があれば、今のシーンを `Dispose()` し、`SceneManager.LoadSceneAsync(名前, Single)` を始める
  5. `Time.timeScale` を時計の値（一時停止なら 0）に合わせる
- `FixedUpdate` と `LateUpdate` は、シーンが動いているときだけ `FixedTick`／`LateTick` を呼ぶ。
- `Main` が破棄されるとき（再生の終了など）は、今のシーンを `Dispose()` する。
- アプリの一時停止など、ほかに Unity から来る通知が必要になったら、`Main` だけが受け取り、context を通して渡す。

### 6.3 シーンの切り替え（Core の `SceneFlow`）
- 状態は Core の `SceneFlow<TId>` が持ち、テストを書く。段階は「シーンなし」「動いている」「読み込み中」の3つ。
- 依頼は1フレームに1つ。最初の依頼だけを受け付け、それ以降と読み込み中の依頼は受け付けない（`false` を返す）。
- 切り替えは、そのフレームのシーンの Tick が終わってから行う（Tick の途中でシーンが消えない）。
- `SceneId` は `Boot`、`Home`、`Run`。シーンのファイル名と同じ名前にする。Build Settings の順は Boot、Home、Run。
- Boot は `Init` ですぐ Home を依頼する（起動時の準備をここに足していく）。
- Sandbox のシーンは、各自が `SceneBase` を継承したシーンのクラス（例 `SandboxAScene`）を `Scripts/Game/Scenes/Sandbox/` に書いて置く（Game のアセンブリなので規則はすべて当てはまる）。`SceneId` には足さない（直接再生するだけ）。
- どのシーンを直接再生しても同じように動く。Main は Boot を通ったかどうかを気にせず、今のシーンの `SceneBase` を探して `Init` する。
  - そのため、全体の準備（`GameContext` を作る、将来のセーブの読み込みなど）は `BootScene` ではなく `Main` が、最初のシーンの `Init` より前に行う。
  - `BootScene` の役目は、起動の演出など「起動したときだけ」のことと、Home への切り替えだけ。

### 6.4 SceneBase と GameContext
```csharp
public abstract class SceneBase : MonoBehaviour
{
    public abstract void Init(GameContext context);
    public abstract void Tick(float deltaTime);
    public virtual void FixedTick(float deltaTime) { }
    public virtual void LateTick(float deltaTime) { }
    public abstract void Dispose();
}
```
- `GameContext` は `Main` が作り、シーンに渡す。中身は最初は2つだけ。
  - シーンの切り替えを依頼する口（`RequestScene(SceneId)`）
  - 時計 `GameClock`：一時停止（`Paused`）、時間の倍率（`Scale`）、スケールなしの経過時間（`UnscaledDeltaTime`。一時停止中に動く UI 用）
- セーブや設定などの全体で使うものは、必要になったときに `GameContext` に足す。

### 6.5 Core に置く共通の部品
- `ITickable`（`Tick(float deltaTime)`）、`IFixedTickable`、`ILateTickable`。片付けは .NET の `IDisposable`。
- `TickList<T>`：途中で増えたり減ったりするもの（敵、弾など）の一覧。`Add`／`Remove` は予約だけして、`ApplyPending()` でまとめて反映する。一覧を回している最中に増減しても壊れない。同じフレームで足してから消したものは入らない。同じものを2回足しても1つ。

### 6.6 Game に置く共通の部品
- `CollisionRelay2D`：`OnTriggerEnter2D`／`OnTriggerExit2D`（と Collision 版）を受け取り、「入ってきた相手」と「今触れている相手」を記録するだけ。`int DrainEntered(List<Collider2D> into)` で取り出して空にする。
- `UiButtonRelay`：`Init` で `Button.onClick` に登録し、押されたら記録するだけ。`bool ConsumeClicked()` で取り出す。`Dispose` で登録を外す。
- `DefinitionAsset<TRuntime>`：`ScriptableObject` を継承した抽象クラス。`public abstract TRuntime Create();` だけを持つ。
- `Layers`：レイヤー番号とマスクの定数（Player=6、Enemy=7、PlayerShot=8、Wall=9）。
- `IHittable`（今あるもの）を `Common` に移す。

## 7. 1つの機能の作り方（お手本の形）

規則を守るとき、機能は次の3種類の部品に分かれる。

| 部品 | 何か | 例 |
|---|---|---|
| System（普通の C#） | 処理と状態を持つ。コンストラクターで参照を受け取り、Tick で動く | `EnemySystem`、`WaveSystem` |
| View（MonoBehaviour） | プレハブに付く。部品（Transform、SpriteRenderer、Rigidbody2D など）への参照を持ち、それを動かすメソッドを出す。自分では動かない | `PlayerView`、`EnemyView` |
| Definition（ScriptableObject） | 調整値。読み取り専用。`DefinitionAsset<T>` なら `Create()` で動く部品を作る | `EnemyDefinition`、`ChaseMovementDefinition` |

- シーンが System を作り、System が View を（プレハブから `Instantiate` して）持つ。
- 片付けは持ち主が行う。View や Relay は `OnDestroy` を使えないので、持ち主の System が `Dispose` の中で、登録の解除（`UiButtonRelay.Dispose()` など）と `Destroy` を呼ぶ。
- `GetComponent` は Init（作ったとき）に1回だけ呼び、結果を持っておく。
- お手本のコードはスキル（§12）に置く。実物のお手本は `Main.cs`、`RunScene.cs`、`FireTimer.cs`、`CollisionRelay2D.cs`。

## 8. 敵の組み合わせの考え方

- 敵の定義（`EnemyDefinition`）が、役割ごとの定義（移動・攻撃・予兆・倒れたときなど）をフィールドに持つ。
- 役割ごとに「役割の抽象定義」（例 `MovementDefinition : DefinitionAsset<IMovement>`）と、その具体的な定義（例 `ChaseMovementDefinition`）を作る。新しい動きは、具体的な定義と動く部品を1組足すだけで、ほかのファイルを直さない。
- 動く部品は自分の状態を持ち、必要なもの（自分の位置、プレイヤーの位置など）は Tick の引数で受け取る。
- 役割の名前と interface の形は、最初の敵のタスクで決める。ここで決めるのはこの考え方だけ。

## 9. 命名

| 対象 | 規則 | 例 |
|---|---|---|
| 型・メソッド・プロパティ・イベント | PascalCase | `FireTimer`、`Tick`、`Interval` |
| interface | `I` ＋ PascalCase | `IHittable`、`ITickable` |
| private のフィールド | `_camelCase`（`private` は書かない） | `_elapsed` |
| const と static readonly | PascalCase（今の「static readonly は camelCase」を変える） | `TotalWaves` |
| 引数・ローカル変数 | camelCase | `deltaTime` |
| ScriptableObject の値 | `[SerializeField] _camelCase` ＋ 読み取り専用のプロパティ | `_moveSpeed` / `MoveSpeed` |
| イベント | 過去形、`On` を付けない | `Died`、`WaveCleared` |
| イベントを受けるメソッド | `Handle` ＋ イベント名 | `HandleDied` |
| テスト | `<クラス名>Tests`、メソッドは `条件_結果` | `NoTarget_DoesNotFire` |

接尾辞（役割を名前で分かるようにする）

| 接尾辞 | 意味 |
|---|---|
| `XxxScene` | `SceneBase` を継承したシーンのクラス（逆に、`SceneBase` を継承したら必ず `Scene` で終わる） |
| `XxxSystem` | シーンが持つ処理（普通の C#） |
| `XxxView` | プレハブに付く MonoBehaviour |
| `XxxDefinition` | ScriptableObject のデータ（`Create()` を持つものを含む） |
| `XxxRelay` | Unity から来るものを受け取るだけの MonoBehaviour |
| `XxxExtensions` | 拡張メソッドをまとめた static クラス |
| `XxxId` | 種類を表す enum |

名前は世界観ではなく役割で付ける（今のルールのまま）。

## 10. フォルダ構成

```
Assets/_Project/
  Resources/Main.prefab
  Scripts/Core/
    Common/       ITickable, IFixedTickable, ILateTickable, TickList, SceneFlow
    Combat/       TargetSelector, FireTimer, Health, HitInvulnerability（今あるものを移す）
    Movement/     MoveVectorResolver（今あるものを移す）
    Waves/        WaveLayout, WaveProgress, WaveSequence（今あるものを移す）
    Extensions/
  Scripts/Game/
    Main/         Main, GameContext, GameClock, SceneId, SceneBase
    Scenes/       BootScene, HomeScene, RunScene
    Common/       CollisionRelay2D, UiButtonRelay, DefinitionAsset, Layers, IHittable
    Extensions/
    Data/         今あるデータ（PlayerStats, EnemyDefinition, WaveDefinition, SkillId, SkillDefinition）
    Player/ Enemies/ Run/ UI/   それぞれのタスクで足す
  Scripts/Editor/
  Scenes/Boot.unity, Home.unity, Run.unity, Sandbox/
```
- 1ファイルに1つの型。ファイル名と型の名前を同じにする。
- ファイルを移すときは `.meta` も一緒に移す（GUID を保つ）。

## 11. 自動チェック（`ArchitectureGuardTests`）

`Assets/_Project/Tests/EditMode/ArchitectureGuardTests.cs`。規則ごとに1つのテストにして、違反を全部まとめて報告する（最初の1件で止めない）。報告には場所（ファイルと行、または型とメンバー）と日本語の直し方を出す。

対象は `PictoArcher.Core` と `PictoArcher.Game` のアセンブリと、`Scripts/Core`・`Scripts/Game` の `.cs` ファイル。`Scripts/Editor` とテストのコードは対象外（エディタの拡張は Unity の書き方に従う必要があるため）。ソースの文字列で調べるときは、先にコメントと文字列の中身を取り除く。

例外は規則ごとに別々に持つ。たとえば `Main` は標準ライフサイクル（`OnApplicationPause` などを含む表の全部）を書いてよいが、当たり判定の通知は書けない。`CollisionRelay2D` は当たり判定の通知だけを書いてよく、`Update` などは書けない。

| 規則 | 調べ方 | 例外 |
|---|---|---|
| 標準ライフサイクルのメソッドを書かない（MonoBehaviour と ScriptableObject の `Awake`、`Start`、`Update`、`FixedUpdate`、`LateUpdate`、`OnEnable`、`OnDisable`、`OnDestroy`、`OnApplicationPause`、`OnApplicationFocus`、`OnApplicationQuit`、`OnGUI`、`OnBecameVisible`、`OnBecameInvisible`） | リフレクション（宣言されたメソッド名） | `Main` |
| 当たり判定の通知（`OnTrigger*2D`、`OnCollision*2D`）を書かない | リフレクション | `CollisionRelay2D` |
| コルーチン・async を使わない（`StartCoroutine`、`StopCoroutine`、`WaitForSeconds`、`async`、`await`、文字列や nameof を使う `Invoke`、`InvokeRepeating`） | ソースの文字列 | なし |
| Find 系を使わない（`GameObject.Find*`、`FindObject*`、`FindFirstObjectByType`、`FindAnyObjectByType`、`FindWithTag`、`FindGameObjectsWithTag`、文字列で探す `transform.Find`、`Camera.main`） | ソースの文字列 | なし |
| `SendMessage`、`BroadcastMessage` を使わない | ソースの文字列 | なし |
| `Time.` を使わない | ソースの文字列 | `Main`、`GameClock` |
| `Resources.Load`、`DontDestroyOnLoad`、`[RuntimeInitializeOnLoadMethod]`、`[DefaultExecutionOrder]`、`[ExecuteAlways]`、`[ExecuteInEditMode]` を使わない | ソースの文字列 | `Main`（前の4つ） |
| static の書き換えられる値を持たない（static のフィールドのうち const でも readonly でもないもの、static readonly のコレクションと配列、static の自動プロパティとイベント） | リフレクション | なし |
| MonoBehaviour・ScriptableObject・`[Serializable]` の型に public のフィールドを持たない | リフレクション | なし |
| ScriptableObject に public の setter を持たない | リフレクション | なし |
| 継承は骨組みだけ、ほかは sealed | リフレクション（§5.4 の条件） | なし |
| Core で Unity の部品を使わない（`MonoBehaviour`、`ScriptableObject`、`GameObject`、`Component`、`Transform`、`Rigidbody2D`、`Collider2D`、`Time`、`Physics2D`、`Input`、`Debug`、`Resources`） | ソースの文字列 | なし |
| 名前空間が決まったもの | リフレクション | なし |
| ファイル名と型名が同じ | ソースの文字列 | なし |
| 命名（private のフィールドは `_camelCase`、const と static readonly は PascalCase、interface は `I` で始まる、`SceneBase` の子は `Scene` で終わる、拡張メソッドの static クラスは `Extensions` で終わる） | リフレクション | なし |
| インスペクターで UnityEvent に関数をつながない | `Assets/_Project` の `.prefab` と `.unity` の YAML で `m_Calls:` に中身があるか | なし |

- 例外の一覧はテストの中だけに持つ。コメントで違反を見逃させる仕組みは作らない。例外を足すのは設計の変更なので、PR に理由を書く。
- エディタでだけ動く `OnValidate`、`Reset`、`OnDrawGizmos` は禁止しない。

## 12. お手本と落とし穴（Claude に読ませるもの）

- スキル `.claude/skills/pictoarcher-coding-rules/SKILL.md` を作る。PictoArcher の C# を書く・直す・レビューするときに読む。中身：
  - 8項目の要点と、迷ったときの判断
  - お手本：シーン、System、View、Definition と動く部品、Relay の使い方、イベントの登録と解除、TickList の使い方
  - 「こう書きたくなったら、代わりにこう書く」の表（Update → シーンの Tick から呼ぶ、Find → インスペクターでつなぐ、コルーチン → タイマー、シングルトン → context で渡す、など）
- 落とし穴の一覧（`docs/unity-pitfalls.md`）。初めての人が読む文書として書き、スキルからも参照する。
  1. ScriptableObject を実行中に書き換えると、エディタでは値がアセットに残る
  2. Destroy したオブジェクトに `?.` や `??` が効かない（`== null` で比べる）
  3. Destroy はすぐには消えない（フレームの終わり）
  4. `[SerializeField]` の名前を変えると値が消える（`[FormerlySerializedAs]` を付ける）
  5. `.meta` は必ずコミットする。ファイルを移すときは `.meta` も
  6. シーンとプレハブは同時に2人で編集すると衝突する（担当を分ける理由）
  7. 物理で動かすものは `Rigidbody2D` を使い、`FixedTick` で動かす
  8. 入力は Tick で読む（FixedTick で読むと押したのを取りこぼす）
  9. `GetComponent` は毎フレーム呼ばず、作ったときに持っておく
  10. 弾のようにたくさん作って消すものはプールで使い回す
  11. ドメインのリロードが無効なので、static の値が再生をまたいで残る
  12. Unity が呼ぶ順番は、オブジェクトどうしでは決まっていない（だから Main から呼ぶ）
  13. タグやレイヤーを文字列や数字で書かない（`Layers` を使う）
  14. プレハブのインスタンスで値を変えると、プレハブ本体には入らない（上書きの扱い）
  15. TextMeshPro を置くと最初に「New Text」が入っている（文字ゼロのルール）
- `CLAUDE.md` に「C# を書く前にスキル `pictoarcher-coding-rules` を読む」と書く。
- `.claude/agents/task-reviewer.md` の確認項目に、8項目と「ガードテストに例外を足していないか」を足す。

## 13. 文書の更新

- `docs/development-rules.md`：§2 命名を §9 に合わせて書き換え、「設計の規則（8項目）」の節を足す。
- `CLAUDE.md`：見出し4つ（文字ゼロのルール／フォルダ構成／テストの書き方／シーンとブランチ）は残す（`ProjectSmokeTests` が見ている）。フォルダ構成を実物に合わせ、実行順の規則とスキルの案内を足す。
- 計画1（`2026-10-26-plan1-core-loop.md`）のタスク8〜10の頭に「この規則に読み替える」注記を足す（中身は書き直さない）。
- 仕様書：`tools/spec/build_spec_xlsx.py` の作業工程に P1-00 の行、開発ルールのシートに命名と8項目、履歴に1行を足し、VERSION と BUILT を上げて `.xlsx` を作り直す。

## 14. 今あるコードへの影響

- `EnemyRegistry`：static をやめ、普通のクラスにする（`RunScene` が作って下に渡す）。`Game/Enemies` に移す。`DataFormatTests` の EnemyRegistry のテストは、重複登録のテストをインスタンスで書き直し、`ResetOnLoad` のテストは消す。
- Data の ScriptableObject（`PlayerStats`、`EnemyDefinition`、`WaveDefinition` と `SpawnEntry`、`SkillDefinition`）：public のフィールドを `[SerializeField, FormerlySerializedAs("今の名前")] _camelCase` と読み取り専用のプロパティに変える。`DataFormatTests` はプロパティを使うように直す。
  - 今あるデータのアセットは `Data/PlayerStats.asset` だけ。値が引き継がれることは、`PlayerStats.asset` を読み込んで値を確かめるテストを足して保証する。
- Core の今あるファイルは中身を変えず、フォルダだけ移す（`.meta` ごと）。今の Core は `UnityEngine` から `Vector2` と `Mathf` しか使っていないので、ガードテストに引っかからない（`TargetSelector` と `MoveVectorResolver` の `using UnityEngine;` は数学の型のため）。
- Editor の asmdef は Core と Game を参照している。テストの asmdef は Core・Game・Editor を参照している。どちらも変えない。
- PR は1つ（Issue #4 の1タスク）。コミットは段階ごとに分ける。
- `Run.unity` に `RunScene` を付けた GameObject を一番上の階層に置く。`Boot.unity`、`Home.unity` を作り、Build Settings を Boot、Home、Run にする。
- Home には仮の開始ボタン（文字なし、Unity の標準の絵）を置き、押すと Run に切り替わるようにする（切り替えが動くことを確かめるため。画面の作り込みはホーム画面のタスクで行う）。

## 15. 私（Claude）の判断の一覧

山田さんが決めたこと：B を選ぶ、当たり判定は「受け取って後で処理」、敵は「データで組み合わせる」、Main とシーンの仕組みを今日動く形にする。

以下は私が決めた。読むときに確かめてほしい。
1. Main は `Resources` のプレハブを `RuntimeInitializeOnLoadMethod` で自動で作る（どのシーンから再生しても動くように）。
2. シーンの `SceneBase` は一番上の階層から探す（Find 系を使わない）。
3. 時間を読むのは Main（と GameClock）だけ。一時停止は Main が `Time.timeScale` を変えて行う。
4. 依頼は1フレームに1つ、最初のものだけ受け付ける。読み込みが終わったフレームは Init だけで、Tick は次から。
5. 入力はコールバックを使わず、Tick で読む。
6. インスペクターで UnityEvent に関数をつなぐのを禁止し、YAML で調べる。
7. 継承は骨組みだけ、ほかは sealed をテストで強制する。
8. const と static readonly を PascalCase に変える（今のルールから変える）。
9. Core の今あるファイルを役割ごとのフォルダに移す。
10. 敵の interface は今は作らず、考え方とお手本だけにする。
11. お手本はスキル、落とし穴は `docs/unity-pitfalls.md` に置く。
12. `Layers` の定数クラスを今作る。
13. Home に文字なしの仮の開始ボタンを置く。
14. エディタでだけ動く `OnValidate`、`Reset`、`OnDrawGizmos` は許す。
15. どのシーンを直接再生しても動くように、全体の準備は Boot ではなく Main が行う。
16. Sandbox のシーンのクラスは `Scripts/Game/Scenes/Sandbox/` に置き、規則を同じように当てる。
17. Editor とテストのコードはガードテストの対象外にする。
18. 1つの PR にまとめる。

## 16. 作る人に任せること

- 各クラスのメソッドの細かい形（引数の並びなど）は、この設計の考え方に合う範囲で作る人が決める。
- ガードテストの正規表現の書き方。誤検知が出たら、規則の意図に合わせて調べ方を直す（例外を足すのではなく）。
- お手本のコードの具体的な題材（設計書 第3版の内容に合うもの）。

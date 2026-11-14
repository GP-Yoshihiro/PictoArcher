# コーディング規則と実行順の土台 実装計画

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Main → SceneBase → 各シーン → 各クラスの実行順の土台（Boot → Home → Run の切り替えまで動く）と、規則を破ると落ちる ArchitectureGuardTests、Claude に読ませる規則の文書を、1つの PR で入れる。

**Architecture:** 標準ライフサイクルを書くのは `Main`（Resources のプレハブを `RuntimeInitializeOnLoadMethod` で作る）だけ。シーンの切り替えの状態は Core の `SceneFlow<TId>`（テストあり）が持ち、`Main` は Unity とのつなぎだけをする。規則はリフレクション・ソースの文字列・YAML の3つの調べ方で EditMode テストにする。

**Tech Stack:** Unity 6000.5.10f1（URP 2D）、C#、NUnit（Unity Test Framework 1.7.0）、uGUI 2.5.0、Input System 1.20.0、Python 3＋openpyxl 3.1.5（仕様書）。

**Spec:** `docs/superpowers/specs/2026-11-14-coding-rules-design.md`（以下「設計書」。§番号は設計書の節）

## Global Constraints

- 名前空間は `PictoArcher.Core`／`PictoArcher.Game`／`PictoArcher.Editor`／`PictoArcher.Tests.EditMode`（PlayMode テストは `PictoArcher.Tests.PlayMode`）だけ。フォルダを分けても名前空間は分けない。
- private のフィールドは `_camelCase`（`private` は書かない）。const と static readonly は PascalCase。
- `Main` 以外は Unity の標準ライフサイクル（設計書 §11 の表の14個）を書かない。`CollisionRelay2D` 以外は当たり判定の通知を書かない。
- `Time.` を書いてよいのは `Main.cs` と `GameClock.cs` だけ。
- 自分たちのクラスで継承してよいのは `SceneBase`、`DefinitionAsset<T>`、`DefinitionAsset<T>` を直接継承した抽象クラスだけ。ほかの具体的なクラスは `sealed`。
- ScriptableObject のフィールドは `[SerializeField, FormerlySerializedAs("今の名前")] _camelCase` と読み取り専用のプロパティ。public のフィールドと public の setter は禁止。
- 1ファイルに1つの型。ファイル名と型の名前は同じ。ファイルを移すときは `git mv` で `.meta` も一緒に移す。
- 画面に文字を出さない。Home の仮のボタンに Text の子を付けない。
- テストは Unity のエディタを閉じて実行する。実行前に `pgrep -x Unity` が何も返さないことを確かめる。動いていたら止めて人に伝える。
- テストの実行は時間がかかるので、Bash の `run_in_background` で動かす。結果は `Logs/editmode-results.xml` をそのまま読まず、`log-triage` か grep で失敗だけを抜き出す。
- コミットは自分が変えたファイルだけを `git add <パス>` で足す（`git add -A` は使わない）。`ProjectSettings/SceneTemplateSettings.json` と空のフォルダ `Assets/_Project/Scripts 2` は自分のものではないので触らない。
- コミットの種類は `feat:`／`test:`／`docs:`／`chore:`、英語の短い説明。末尾に `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`。
- push と下書き PR の作成は、山田さんの OK をもらってから。

## Review Focus

1. **最初のフレームのシーン切り替え（Boot が Init の中で Home を依頼する）**：`Init` より前に `SceneFlow` が「動いている」になっていないと依頼が捨てられ、Boot で止まる。→ Task 1 の `Request_DuringFirstInit_IsAccepted` 相当（`EnterRunning` のあとの `Request` が通る）と Task 5 の PlayMode テストで押さえる。
2. **SceneBase がないシーン（テスト用の一時シーン、作りかけの Sandbox）を再生したとき**：エラーを1回だけ出して止まらずに動き続けるべき。毎フレームエラーを出すのは不可。→ Task 5 の PlayMode テストで、テスト開始時の一時シーンを通っても Boot → Home → Run が通ることで押さえる。
3. **コンパイラが作る型（ラムダのキャッシュ `<>c`、自動プロパティの裏のフィールド `<X>k__BackingField`）**：ガードテストが誤検知してはいけない。→ Task 4 に、ラムダと自動プロパティを使っている今のコード（`TickList`、`GameClock` など）で全テストが通ることを確かめる手順を入れる。
4. **`[SerializeField]` の名前の変更で `PlayerStats.asset` の値が消える**：→ Task 3 の `PlayerStatsAsset_KeepsValuesAfterRename` で押さえる。
5. **同じフレームで2回押す・切り替え中に押す（Home の開始ボタンの連打）**：2回目の依頼は捨てられ、Run が2回読み込まれない。→ Task 1 の `SecondRequestSameFrame_ReturnsFalseAndKeepsFirst` と `Request_WhileLoading_ReturnsFalse` で押さえる。

---

## ファイルの地図

| 場所 | 作る／直す | 役目 |
|---|---|---|
| `Scripts/Core/Common/ITickable.cs` `IFixedTickable.cs` `ILateTickable.cs` | 作る | Tick を受ける口 |
| `Scripts/Core/Common/TickList.cs` | 作る | 増減を予約して反映する一覧 |
| `Scripts/Core/Common/SceneFlow.cs` `SceneFlowPhase.cs` | 作る | シーン切り替えの状態 |
| `Scripts/Core/Combat/` `Movement/` `Waves/` | 移す | 今の Core のファイル |
| `Scripts/Core/Waves/WaveKind.cs` `WavePhase.cs` | 分ける | 1ファイル1型のため（中身はそのまま） |
| `Scripts/Game/Main/Main.cs` `GameContext.cs` `GameClock.cs` `SceneId.cs` `SceneBase.cs` | 作る | 実行順の土台 |
| `Scripts/Game/Scenes/BootScene.cs` `HomeScene.cs` `RunScene.cs` | 作る | シーンのクラス |
| `Scripts/Game/Common/CollisionRelay2D.cs` `UiButtonRelay.cs` `DefinitionAsset.cs` `Layers.cs` | 作る | 共通の部品 |
| `Scripts/Game/Common/IHittable.cs` | 移す | |
| `Scripts/Game/Enemies/EnemyRegistry.cs` | 移して直す | static をやめる |
| `Scripts/Game/Data/*.cs` `SpawnEntry.cs` | 直す・分ける | SO のフィールドの形 |
| `Scripts/Game/PictoArcher.Game.asmdef` | 直す | `UnityEngine.UI` を足す |
| `Resources/Main.prefab`、`Scenes/Boot.unity` `Home.unity`、`Scenes/Run.unity`、`ProjectSettings/EditorBuildSettings.asset` | 作る・直す | シーンとプレハブ |
| `Tests/EditMode/*Tests.cs` | 作る・直す | |
| `Tests/PlayMode/` と `tools/run-playmode-tests.sh` | 作る | 切り替えが実際に動くことの確認 |
| 文書（`.claude/skills/pictoarcher-coding-rules/SKILL.md`、`docs/unity-pitfalls.md`、`docs/development-rules.md`、`CLAUDE.md`、`.claude/agents/task-reviewer.md`、計画1） | 作る・直す | |
| `tools/spec/build_spec_xlsx.py`、`docs/PictoArcher_仕様書.xlsx` | 直す・作り直す | |

設計書からの小さなずれ（私の判断。PR に書く）：
- **1ファイル1型のため、`WaveKind`・`WavePhase`・`SpawnEntry` を別のファイルに分ける。** 設計書 §14 は「Core の中身を変えない」としているが、§10・§11 の「1ファイル1型」と両立しないため。中身（定義）は変えない。
- **テストの asmdef に `UnityEngine.UI` を足す**（`UiButtonRelay` のテストのため）。§14 は「テストの asmdef は変えない」としているが、参照を1つ足すだけ。
- **PlayMode テストを1つ足す**（`Tests/PlayMode/`、`tools/run-playmode-tests.sh`）。「今日の時点で切り替えが動く」をテストで確かめるため。Find 系を使うがテストのコードはガードの対象外（設計書 §11）。
- **作業工程の P1-08〜P1-10 の「先に要るもの」に P1-00 を足す。**

---

### Task 1: Core の共通部品と、今あるファイルの移動

**Files:**
- Create: `Assets/_Project/Scripts/Core/Common/ITickable.cs`、`IFixedTickable.cs`、`ILateTickable.cs`、`TickList.cs`、`SceneFlowPhase.cs`、`SceneFlow.cs`
- Create: `Assets/_Project/Scripts/Core/Waves/WaveKind.cs`、`WavePhase.cs`（`WaveLayout.cs`・`WaveSequence.cs` から enum を切り出す）
- Move（`git mv`、`.meta` も）: `TargetSelector` `FireTimer` `Health` `HitInvulnerability` → `Core/Combat/`、`MoveVectorResolver` → `Core/Movement/`、`WaveLayout` `WaveProgress` `WaveSequence` → `Core/Waves/`
- Test: `Assets/_Project/Tests/EditMode/TickListTests.cs`、`SceneFlowTests.cs`

**Interfaces:**
- Produces:
  - `public interface ITickable { void Tick(float deltaTime); }`、`IFixedTickable { void FixedTick(float deltaTime); }`、`ILateTickable { void LateTick(float deltaTime); }`
  - `public sealed class TickList<T> where T : class`：`int Count`、`T this[int index]`、`void Add(T item)`、`void Remove(T item)`、`void ApplyPending()`、`void Clear()`（予約も含めて全部消す）。`IEnumerable` は実装しない（`for` で回す。回している最中の増減は予約になるので壊れない）。
  - `public enum SceneFlowPhase { Empty, Running, Loading }`
  - `public sealed class SceneFlow<TId> where TId : struct`：`SceneFlowPhase Phase`、`bool HasPending`、`bool Request(TId id)`、`bool TryBeginLoad(out TId id)`、`void EnterRunning()`

- [ ] **Step 1: 今あるファイルを移す**

`git mv` で上の表のとおりに移す（`.cs` と `.cs.meta` の両方）。`WaveKind` を `WaveLayout.cs` から、`WavePhase` を `WaveSequence.cs` から切り出して `Core/Waves/WaveKind.cs`・`WavePhase.cs` に置く（定義の行はそのまま、`namespace PictoArcher.Core` で囲む）。新しいフォルダと新しいファイルの `.meta` は、Step 4 のテスト実行で Unity が作る。

- [ ] **Step 2: 失敗するテストを書く**

`TickListTests`：
```csharp
[Test] public void Add_IsInvisibleUntilApplyPending()         // Add 後 Count==0、ApplyPending 後 Count==1
[Test] public void AddTwice_KeepsOne()                         // 同じものを2回 Add → ApplyPending → Count==1（反映済みのものをもう一度 Add しても1つ）
[Test] public void AddThenRemoveSameFrame_NeverAppears()       // Add→Remove→ApplyPending → Count==0
[Test] public void Remove_IsVisibleAfterApplyPending()         // 反映済み a を Remove → Count==1 のまま → ApplyPending → Count==0
[Test] public void ChangesDuringLoop_DoNotBreakIteration()     // for で回しながら Add と Remove → 例外なし、回した回数は回す前の Count
[Test] public void RemoveUnknown_DoesNothing()
[Test] public void Clear_DropsItemsAndPending()                // 反映済みと予約の両方が消え、ApplyPending 後も Count==0
```

`SceneFlowTests`（`TId` は テスト内の `enum TestId { A, B }`）：
```csharp
[Test] public void NewFlow_IsEmptyWithoutPending()             // Phase==Empty, HasPending==false
[Test] public void Request_WhenEmpty_ReturnsFalse()
[Test] public void Request_AfterEnterRunning_IsAccepted()      // Boot が Init の中で依頼する場面（Review Focus 1）。EnterRunning → Request(A)==true, HasPending
[Test] public void SecondRequestSameFrame_ReturnsFalseAndKeepsFirst() // Request(A) true, Request(B) false, TryBeginLoad(out id) → id==A
[Test] public void TryBeginLoad_WithPending_EntersLoading()    // true, Phase==Loading, HasPending==false
[Test] public void TryBeginLoad_WithoutPending_ReturnsFalse()  // Phase は Running のまま
[Test] public void Request_WhileLoading_ReturnsFalse()
[Test] public void EnterRunning_FromLoading_AcceptsRequestsAgain()
[Test] public void EnterRunning_WhenRunning_Throws()           // InvalidOperationException
```

- [ ] **Step 3: テストが失敗することを確かめる**

Run: `tools/run-editmode-tests.sh`（バックグラウンド）
Expected: コンパイルエラー（`TickList`・`SceneFlow` がない）で終了コードが 0 以外。`Logs/editmode.log` に `error CS0246` が出る。

- [ ] **Step 4: `ITickable` 系、`TickList<T>`、`SceneFlowPhase`、`SceneFlow<TId>` を書く**

`TickList<T>`：反映済みの `List<T>` と、足す予約・消す予約の2つの `List<T>`。`Add` は消す予約にあれば取り除き、なければ足す予約に入れる（反映済み・予約済みなら何もしない）。`Remove` は足す予約にあれば取り除き、なければ消す予約に入れる。`ApplyPending` は消す予約→足す予約の順に反映して空にする。
`SceneFlow<TId>`：`Request` は `Phase == Running && !HasPending` のときだけ受け付ける。`TryBeginLoad` は予約があれば `Loading` にして予約を空にする。`EnterRunning` は `Empty` か `Loading` から `Running` にする（`Running` からは例外）。

- [ ] **Step 5: テストが通ることを確かめる**

Run: `tools/run-editmode-tests.sh`
Expected: 終了コード 0。`Logs/editmode-results.xml` の `<test-run ... failed="0"`。移した Core のテスト（`FireTimerTests` など）も通る。

- [ ] **Step 6: コミット**

`git status --short Assets/_Project/Scripts/Core Assets/_Project/Tests` で、Unity が作った新しいフォルダとファイルの `.meta` も足す。
```bash
git commit -m "feat: add tick interfaces, TickList and SceneFlow; group Core files by role"
```

---

### Task 2: Game の骨組み（Main・シーン・共通の部品）と EnemyRegistry

**Files:**
- Create: `Assets/_Project/Scripts/Game/Main/SceneId.cs`、`GameClock.cs`、`GameContext.cs`、`SceneBase.cs`、`Main.cs`
- Create: `Assets/_Project/Scripts/Game/Scenes/BootScene.cs`、`HomeScene.cs`、`RunScene.cs`
- Create: `Assets/_Project/Scripts/Game/Common/CollisionRelay2D.cs`、`UiButtonRelay.cs`、`DefinitionAsset.cs`、`Layers.cs`
- Move: `Scripts/Game/IHittable.cs` → `Game/Common/`、`Scripts/Game/EnemyRegistry.cs` → `Game/Enemies/`（`git mv`、`.meta` も）
- Modify: `Assets/_Project/Scripts/Game/PictoArcher.Game.asmdef`（references に `"UnityEngine.UI"`）、`Assets/_Project/Tests/EditMode/PictoArcher.Tests.EditMode.asmdef`（references に `"UnityEngine.UI"`）、`Assets/_Project/Tests/EditMode/DataFormatTests.cs`（EnemyRegistry の部分）
- Test: `Assets/_Project/Tests/EditMode/GameClockTests.cs`、`GameContextTests.cs`、`CollisionRelay2DTests.cs`、`UiButtonRelayTests.cs`、`LayersTests.cs`、`EnemyRegistryTests.cs`

**Interfaces:**
- Consumes: `SceneFlow<TId>`、`SceneFlowPhase`（Task 1）
- Produces:
  - `public enum SceneId { Boot, Home, Run }`
  - `public sealed class GameClock`：`bool Paused { get; set; }`、`float Scale { get; set; }`（初期値 1、負なら `ArgumentOutOfRangeException`）、`float UnscaledDeltaTime { get; }`、`float EffectiveTimeScale`（`Paused ? 0 : Scale`）、`void Advance(float unscaledDeltaTime)`（`Main` だけが呼ぶ）
  - `public sealed class GameContext`：`GameContext(SceneFlow<SceneId> flow, GameClock clock)`、`GameClock Clock { get; }`、`bool RequestScene(SceneId id)`
  - `SceneBase`（設計書 §6.4 のコードのとおり）
  - `public sealed class Main : MonoBehaviour`
  - `public sealed class CollisionRelay2D : MonoBehaviour`：`int DrainEntered(List<Collider2D> into)`、`IReadOnlyCollection<Collider2D> Touching`
  - `[RequireComponent(typeof(Button))] public sealed class UiButtonRelay : MonoBehaviour`：`void Init()`、`bool ConsumeClicked()`、`void Dispose()`
  - `public abstract class DefinitionAsset<TRuntime> : ScriptableObject { public abstract TRuntime Create(); }`
  - `public static class Layers`：`const int Player = 6, Enemy = 7, PlayerShot = 8, Wall = 9;` と `PlayerMask`・`EnemyMask`・`PlayerShotMask`・`WallMask`（`1 << 番号`）
  - `public sealed class EnemyRegistry`：`IReadOnlyList<Transform> Active`、`void Register(Transform t)`（重複は無視）、`void Unregister(Transform t)`、`void Clear()`

- [ ] **Step 1: 失敗するテストを書く**

```csharp
// GameClockTests
[Test] public void NewClock_RunsAtScaleOne()                  // Paused==false, Scale==1, EffectiveTimeScale==1
[Test] public void Paused_MakesEffectiveScaleZero()           // Scale=2, Paused=true → 0。Paused=false → 2
[Test] public void Advance_StoresUnscaledDeltaEvenWhenPaused() // Paused=true, Advance(0.25f) → UnscaledDeltaTime==0.25f
[Test] public void NegativeScale_Throws()                     // ArgumentOutOfRangeException
// GameContextTests
[Test] public void RequestScene_ForwardsToFlow()              // flow.EnterRunning(); ctx.RequestScene(Home)==true; flow.TryBeginLoad(out id) && id==Home
// CollisionRelay2DTests（private の OnTriggerEnter2D/Exit2D をリフレクションで呼ぶ。相手は BoxCollider2D を付けた GameObject）
[Test] public void DrainEntered_ReturnsEnteredOnceThenEmpty() // 1回目 1件で into に入る、2回目 0
[Test] public void Exit_RemovesFromTouching()                 // Enter 後 Touching に含む、Exit 後 含まない
// UiButtonRelayTests（Button を付けた GameObject）
[Test] public void Click_IsConsumedOnce()                     // Init → onClick.Invoke() → ConsumeClicked true, もう一度 false
[Test] public void AfterDispose_ClicksAreIgnored()            // Init → Dispose → Invoke → false
// LayersTests
[Test] public void Layers_MatchProjectSettings()              // LayerMask.NameToLayer("Player")==Layers.Player（Enemy・PlayerShot・Wall も）
[Test] public void Masks_AreOneBitPerLayer()                  // Layers.WallMask == 1 << 9 など
// EnemyRegistryTests（DataFormatTests の EnemyRegistry_IgnoresDuplicateRegisterAndUnknownUnregister をここへインスタンスで移す）
[Test] public void IgnoresDuplicateRegisterAndUnknownUnregister()
[Test] public void Instances_DoNotShareState()                // 2つ作って片方に登録 → もう片方は空
```
`DataFormatTests` から EnemyRegistry の2つのテストと TearDown の `EnemyRegistry.Clear()` を消す（`EnemyRegistry_HasSubsystemRegistrationResetMethod` は消すだけ）。テストで作った GameObject は TearDown で `DestroyImmediate` する。

- [ ] **Step 2: テストが失敗することを確かめる**

Run: `tools/run-editmode-tests.sh`
Expected: コンパイルエラー（`GameClock` などがない）で終了コードが 0 以外。

- [ ] **Step 3: `SceneId`、`GameClock`、`GameContext`、`SceneBase`、共通の部品、`EnemyRegistry` を書く**

`CollisionRelay2D`：`OnTriggerEnter2D`／`OnTriggerExit2D`／`OnCollisionEnter2D`／`OnCollisionExit2D`（Collision 版は `collision.collider`）だけを書く。入ってきた相手は `List<Collider2D>`、今触れている相手は `HashSet<Collider2D>`。Destroy された相手が `Touching` に残ることがあるのは、使う側が `== null` で確かめる（XML コメントに書く）。
`UiButtonRelay`：`Init` で `GetComponent<Button>()` を1回呼んで持ち、`onClick.AddListener(HandleClicked)`。`Dispose` で `RemoveListener` して記録を消す。インスペクターの onClick は使わない（実行時の `AddListener` は YAML に残らない）。

- [ ] **Step 4: `Main` を書く**

```csharp
[DefaultExecutionOrder(-10000)]
public sealed class Main : MonoBehaviour
{
    const string PrefabPath = "Main";
    SceneFlow<SceneId> _flow; GameClock _clock; GameContext _context;
    SceneBase _scene; int _sceneHandle; AsyncOperation _loading;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void CreateInstance()   // Resources.Load<Main>(PrefabPath)。null なら Debug.LogError して終わる。Instantiate して DontDestroyOnLoad
    void Awake()                    // _clock、_flow、_context を作る（全体の準備はここ。設計書 §6.3）
    void Update()                   // 下の1〜5
    void FixedUpdate()              // _flow.Phase == Running && _scene != null のときだけ _scene.FixedTick(Time.fixedDeltaTime)
    void LateUpdate()               // 同じ条件で _scene.LateTick(Time.deltaTime)
    void OnDestroy()                // _flow.Phase == Running && _scene != null（Unity の null 比較）なら _scene.Dispose()
}
```
`Update` の順番（設計書 §6.2 のとおり。ソースに1行ずつ書く）：
1. `_clock.Advance(Time.unscaledDeltaTime)`
2. `Phase == Empty` なら、今のシーンで開始（下の「開始」）してこのフレームは Tick しない。`Phase == Running` でアクティブなシーンの `handle` が開始したときの `_sceneHandle` と違う（テストやエディタが `SceneManager.LoadScene` で直接読み込んだ）ときも開始して Tick しない（古いシーンはもう壊れているので Dispose は呼ばない。XML コメントに「外からの読み込みはテストとエディタのためだけ」と書く）。`Phase == Loading` で `_loading.isDone` なら開始して Tick しない。読み込み中で終わっていなければ 5 へ。
3. `_scene != null` なら `_scene.Tick(Time.deltaTime)`
4. `_flow.TryBeginLoad(out var next)` なら、`_scene?.Dispose()` ではなく `if (_scene != null) _scene.Dispose();`、`_scene = null`、`_loading = SceneManager.LoadSceneAsync(next.ToString(), LoadSceneMode.Single)`
5. `Time.timeScale = _clock.EffectiveTimeScale`

「開始」：`Phase != Running` なら `_flow.EnterRunning()` を **先に** 呼んでから（Boot が Init の中で依頼できるように。Review Focus 1）、`_sceneHandle` に今のシーンの `handle` を入れ、`SceneManager.GetActiveScene().GetRootGameObjects()` の各 `GetComponent<SceneBase>()` を数える。ちょうど1つなら `_scene` にして `_scene.Init(_context)`。0 か 2つ以上なら `Debug.LogError`（シーン名と数を入れる）を1回出し、`_scene = null` のまま動き続ける（Review Focus 2）。開始が終わったら 4 の切り替えの確認は同じフレームで行う（Boot は Init→Dispose で次の Home へ進む）。

- [ ] **Step 5: `BootScene`、`HomeScene`、`RunScene` を書く**

- `BootScene`：`Init` で `context.RequestScene(SceneId.Home)`。`Tick`・`Dispose` は空（「起動したときだけのことをここに足す」とコメント）。
- `HomeScene`：`[SerializeField] UiButtonRelay _startButton;`。`Init` で context を持ち `_startButton.Init()`。`Tick` で `if (_startButton.ConsumeClicked()) _context.RequestScene(SceneId.Run);`。`Dispose` で `_startButton.Dispose()`。
- `RunScene`：お手本の形にする。`Init` で `_enemies = new EnemyRegistry()` を作り、`Tick` には「System を作った順に1行ずつ呼ぶ」ことを示すコメント付きの空の並び、`Dispose` は逆順に片付ける（`_enemies.Clear()`）。System はまだ作らない（各タスクで足す）。

- [ ] **Step 6: テストが通ることを確かめる**

Run: `tools/run-editmode-tests.sh`
Expected: 終了コード 0、`failed="0"`。

- [ ] **Step 7: コミット**

新しいフォルダ・ファイルの `.meta` も足す。
```bash
git commit -m "feat: add Main loop, SceneBase, scenes and common relays; make EnemyRegistry an instance"
```

---

### Task 3: ScriptableObject のフィールドの形を変える

**Files:**
- Modify: `Assets/_Project/Scripts/Game/Data/PlayerStats.cs`、`EnemyDefinition.cs`、`WaveDefinition.cs`、`SkillDefinition.cs`
- Create: `Assets/_Project/Scripts/Game/Data/SpawnEntry.cs`（`WaveDefinition.cs` から切り出す）
- Modify: `Assets/_Project/Tests/EditMode/DataFormatTests.cs`

**Interfaces:**
- Produces（プロパティ名＝今のフィールド名を PascalCase にしたもの。すべて get だけ）：
  - `PlayerStats`：`MaxHp`、`MoveSpeed`、`FireInterval`、`MoveFireRateMultiplier`、`ProjectileDamage`、`ProjectileSpeed`、`ProjectileLifetime`、`InvulnerabilitySeconds`、`DragRadiusPixels`（初期値は今のまま：100、6、0.5、1、10、14、3、0.6、80）
  - `EnemyDefinition`：`Prefab`、`MaxHp`、`MoveSpeed`、`ContactDamage`、`XpReward`、`CoinReward`
  - `WaveDefinition`：`IReadOnlyList<SpawnEntry> Spawns`、`TelegraphSeconds`（初期値 0.8）
  - `SpawnEntry`（`[Serializable] public struct`）：`Enemy`、`Position`、コンストラクター `SpawnEntry(EnemyDefinition enemy, Vector2 position)`
  - `SkillDefinition`：`Id`、`Icon`、`MaxStacks`
- 4つの SO のクラスは `sealed` にする。

- [ ] **Step 1: 失敗するテストを書く（DataFormatTests を直す）**

```csharp
[Test] public void PlayerStats_MoveFireRateDefaultsToFull()   // stats.MoveFireRateMultiplier == 1f
[Test] public void WaveDefinition_DefaultsToTelegraph08()      // wave.TelegraphSeconds == 0.8f
[Test] public void PlayerStatsAsset_KeepsValuesAfterRename()   // AssetDatabase.LoadAssetAtPath<PlayerStats>("Assets/_Project/Data/PlayerStats.asset") の9つの値が 100, 6, 0.5, 1, 10, 14, 3, 0.6, 80（Review Focus 4）
[Test] public void SpawnEntry_KeepsConstructorValues()         // new SpawnEntry(null, new Vector2(1, 2)).Position == (1, 2)
```

- [ ] **Step 2: テストが失敗することを確かめる**

Run: `tools/run-editmode-tests.sh`
Expected: コンパイルエラー（`MoveFireRateMultiplier` などがない）。

- [ ] **Step 3: SO のフィールドを `[SerializeField, FormerlySerializedAs("今の名前")] _camelCase` と get だけのプロパティに変え、`SpawnEntry` を切り出す**

`using UnityEngine.Serialization;`。`WaveDefinition` の一覧は `List<SpawnEntry> _spawns = new List<SpawnEntry>()` で、`Spawns => _spawns`。

- [ ] **Step 4: テストが通ることを確かめる**

Run: `tools/run-editmode-tests.sh`
Expected: 終了コード 0、`failed="0"`。`git diff Assets/_Project/Data/PlayerStats.asset` が空のまま（アセットは書き換わらない。Unity が保存し直して名前が `_maxHp` などに変わっていたら、それもコミットに含める）。

- [ ] **Step 5: コミット**

```bash
git commit -m "feat: make ScriptableObject fields private with read-only properties"
```

---

### Task 4: ArchitectureGuardTests

**Files:**
- Create: `Assets/_Project/Tests/EditMode/ArchitectureGuardTests.cs`

**Interfaces:**
- Consumes: Core と Game のアセンブリ（`typeof(SceneFlow<>).Assembly`、`typeof(Main).Assembly`）、`SceneBase`、`DefinitionAsset<>`、`Main`、`CollisionRelay2D`、`GameClock`

規則ごとに1つの `[Test]`（設計書 §11 の表の規則を16個のテストに分ける。名前は下のとおり）。違反は全部集めてから `Assert.IsEmpty(violations, 見出し + "\n" + string.Join("\n", violations))` で出す。見出しには規則と日本語の直し方を書く（例：「標準ライフサイクルは Main だけが書けます。シーンの Tick から呼ばれるメソッドにしてください」）。違反の1行は `型名.メンバー名` か `パス:行`。例外は規則ごとに `static readonly` ではなくテストの中のローカルの配列で持つ（このテストは対象外だが、お手本として）。

| テスト名 | 調べ方の決めごと |
|---|---|
| `NoLifecycleMethodsOutsideMain` | MB と SO の子。`DeclaredOnly`・全アクセスのメソッド名が14個のどれか。例外 `Main` |
| `NoCollisionCallbacksOutsideRelay` | `OnTrigger(Enter|Stay|Exit)(2D)?`、`OnCollision(Enter|Stay|Exit)(2D)?`。例外 `CollisionRelay2D` |
| `NoCoroutinesOrAsync` | `\bStartCoroutine\b`、`\bStopCoroutine\b`、`\bStopAllCoroutines\b`、`\bWaitForSeconds\w*`、`\basync\b`、`\bawait\b`、`\bInvokeRepeating\b`、`\bInvoke\s*\(\s*(nameof\b|")` |
| `NoFindOrCameraMain` | `\bGameObject\s*\.\s*Find\w*`、`\bFind(Object|Objects|FirstObject|AnyObject)\w*`、`\bFindWithTag\b`、`\bFindGameObjectsWithTag\b`、`\w*[Tt]ransform\s*\.\s*Find\s*\(`、`\bCamera\s*\.\s*main\b` |
| `NoSendMessage` | `\b(SendMessage|SendMessageUpwards|BroadcastMessage)\b` |
| `TimeOnlyInMainAndClock` | `\bTime\s*\.`。例外 `Main.cs`、`GameClock.cs` |
| `BootstrapApisOnlyInMain` | `\bResources\s*\.\s*Load`、`\bDontDestroyOnLoad\b`、`\bRuntimeInitializeOnLoadMethod\b`、`\bDefaultExecutionOrder\b`（例外 `Main.cs`）、`\bExecuteAlways\b`、`\bExecuteInEditMode\b`（例外なし） |
| `NoMutableStatics` | static のフィールドで `IsLiteral` でも `IsInitOnly` でもないもの、`IsInitOnly` でも型が配列か `IEnumerable`（`string` を除く）のもの。裏のフィールド `<X>k__BackingField` は「X（自動プロパティかイベント）」として出す |
| `NoPublicFieldsOnSerializedTypes` | MB・SO の子と `[Serializable]` の class／struct（enum を除く）の public のインスタンスのフィールド |
| `NoPublicSettersOnScriptableObjects` | SO の子の `DeclaredOnly` のプロパティで `SetMethod?.IsPublic == true` |
| `OnlyFrameworkTypesAreInheritable` | class だけ（static class を除く）。抽象クラスは `SceneBase`、`DefinitionAsset<>`、`BaseType` の総称型定義が `DefinitionAsset<>` のものだけ。抽象でないクラスは `sealed` でないと違反。基底が自分たちのアセンブリの型なら、その基底が上の3種のどれかでないと違反 |
| `CoreUsesNoUnityComponents` | `Scripts/Core` の `.cs` だけ。`\b(MonoBehaviour|ScriptableObject|GameObject|Component|Transform|Rigidbody2D|Collider2D|Time|Physics2D|Input|Debug|Resources)\b` |
| `NamespacesAreFixed` | Core の型は `PictoArcher.Core`、Game の型は `PictoArcher.Game` |
| `FileNameMatchesTypeName` | 入れ子でない型（`DeclaringType == null`）ごとに、そのアセンブリのフォルダの下に「型名（`` `1 `` を除く）.cs」がちょうど1つある。逆に、型と対応しない `.cs` があれば違反（1ファイルに2つの型を書いた場合も、これで見つかる） |
| `NamingRules` | private のインスタンスのフィールドは `^_[a-z][A-Za-z0-9]*$`、const と static readonly は `^[A-Z][A-Za-z0-9]*$`（enum の値は除く）、interface は `^I[A-Z]`、`SceneBase` の子は `Scene` で終わる、拡張メソッドを持つ static クラスは `Extensions` で終わる |
| `NoPersistentUnityEventCalls` | `Assets/_Project` の下の `.prefab` と `.unity`。正規表現 `m_Calls:[ \t]*\r?\n[ \t]*- ` に当たったら `パス:行` |

共通の決めごと：
- リフレクションで見る型は、`[CompilerGenerated]` が付いたものと名前が `<` で始まるもの（ラムダのキャッシュ `<>c` など）を除く（Review Focus 3）。フィールドも名前が `<` で始まるものは、裏のフィールドとして上の扱いにする以外は除く。
- ソースの文字列は、先にコメント（`//`、`/* */`）と文字列・文字の中身（`"…"`、`@"…"`、`$"…"`、`'…'`）を取り除く。引用符は残して中身だけ空にする（`Invoke("…")` を見つけるため）。行番号がずれないよう改行は残す。
- フォルダは `Path.Combine(Application.dataPath, "_Project/Scripts/Core")` と `.../Game`。

- [ ] **Step 1: ガードテストを書く**

上の16個の `[Test]` と、共通の下ごしらえ（型の一覧、ソースの読み込みとコメント・文字列の除去、違反の出し方）を private のメソッドにする。

- [ ] **Step 2: ガードが本当に違反を見つけることを確かめる**

一時的に `Assets/_Project/Scripts/Game/Common/GuardProbe.cs` を作る（コミットしない）：`public class GuardProbe : MonoBehaviour { public int value; static int count; int bad; void Update() { var c = Camera.main; StartCoroutine(default(System.Collections.IEnumerator)); } }`。
Run: `tools/run-editmode-tests.sh`
Expected: 失敗するのは `NoLifecycleMethodsOutsideMain`、`NoFindOrCameraMain`、`NoCoroutinesOrAsync`、`NoMutableStatics`、`NoPublicFieldsOnSerializedTypes`、`OnlyFrameworkTypesAreInheritable`、`NamingRules`（`bad`）の7つで、どれも `GuardProbe` を指している。ほかは通る。

- [ ] **Step 3: お試しを消して、全部が通ることを確かめる**

`GuardProbe.cs` と `.meta` を消す。
Run: `tools/run-editmode-tests.sh`
Expected: 終了コード 0、`failed="0"`。ラムダ（`TickList` などで使っていれば）と自動プロパティ（`GameClock`、`GameContext`）があっても誤検知がない。誤検知が出たら、例外を足さずに調べ方を直す（設計書 §16）。

- [ ] **Step 4: コミット**

```bash
git commit -m "test: add ArchitectureGuardTests for execution order, naming and structure rules"
```

---

### Task 5: シーンとプレハブ、Build Settings、PlayMode テスト

**Files:**
- Create: `Assets/_Project/Resources/Main.prefab`、`Assets/_Project/Scenes/Boot.unity`、`Assets/_Project/Scenes/Home.unity`
- Modify: `Assets/_Project/Scenes/Run.unity`（一番上の階層に `RunScene` の GameObject を足す）、`ProjectSettings/EditorBuildSettings.asset`
- Create（一時。コミットしない）: `Assets/_Project/Scripts/Editor/SceneScaffold.cs`
- Create: `Assets/_Project/Tests/EditMode/SceneSetupTests.cs`
- Create: `Assets/_Project/Tests/PlayMode/PictoArcher.Tests.PlayMode.asmdef`、`Assets/_Project/Tests/PlayMode/SceneFlowPlayModeTests.cs`、`tools/run-playmode-tests.sh`

**Interfaces:**
- Consumes: `Main`、`BootScene`、`HomeScene`（`_startButton`）、`RunScene`、`UiButtonRelay`、`SceneId`（Task 2）

- [ ] **Step 1: 失敗するテストを書く**

`SceneSetupTests`（EditMode）：
```csharp
[Test] public void BuildSettings_ListBootHomeRunInSceneIdOrder() // EditorBuildSettings.scenes のファイル名（拡張子なし）== Enum.GetNames(typeof(SceneId))、すべて enabled
[Test] public void MainPrefab_HasMain()                          // Resources.Load<Main>("Main") != null
[TestCase("Boot", typeof(BootScene))] [TestCase("Home", typeof(HomeScene))] [TestCase("Run", typeof(RunScene))]
public void Scene_HasOneRootSceneBaseOfType(string name, Type type) // EditorSceneManager.OpenScene(path, Additive) → ルートの SceneBase がちょうど1つで型が type → CloseScene(scene, true)
[Test] public void HomeStartButton_IsWired()                       // Home を開き、HomeScene の SerializedObject の "_startButton" が null でない
[Test] public void HomeStartButton_HasNoTextChild()                // ボタンの子に Text も TMP_Text もない（文字ゼロ）
```
`SceneFlowPlayModeTests`（PlayMode）：
```csharp
[UnityTest] public IEnumerator Boot_GoesToHome_ThenStartButtonGoesToRun()
// LogAssert.ignoreFailingMessages = true（テストの一時シーンに SceneBase がないので Main がエラーを1回出す。Review Focus 2）
// SceneManager.LoadScene("Boot") → 最大300フレーム待って、アクティブなシーンの名前が "Home"
// Object.FindAnyObjectByType<UiButtonRelay>() の Button の onClick.Invoke() → 最大300フレーム待って "Run"
```
PlayMode の asmdef：references に `PictoArcher.Core`、`PictoArcher.Game`、`UnityEngine.UI`、`UnityEngine.TestRunner`、`UnityEditor.TestRunner`。`overrideReferences: true`、`precompiledReferences: ["nunit.framework.dll"]`、`defineConstraints: ["UNITY_INCLUDE_TESTS"]`、`includePlatforms: []`、`autoReferenced: false`。
`tools/run-playmode-tests.sh`：`run-editmode-tests.sh` と同じ形で、`-testPlatform PlayMode`、結果は `Logs/playmode-results.xml`、ログは `Logs/playmode.log`。実行できるようにする（`chmod +x`）。

- [ ] **Step 2: テストが失敗することを確かめる**

Run: `tools/run-editmode-tests.sh`
Expected: `SceneSetupTests` の5つが失敗（シーンとプレハブがない）。ほかは通る。

- [ ] **Step 3: 一時のエディタスクリプトでシーンとプレハブを作る**

`SceneScaffold.Build()`（`public static`、`PictoArcher.Editor`）が行うこと：
1. `Assets/_Project/Resources/Main.prefab`：`Main` を付けた GameObject（名前 `Main`）を `PrefabUtility.SaveAsPrefabAsset` で保存。
2. `Boot.unity`：`EditorSceneManager.NewScene(EmptyScene)`。`MainCamera` プレハブ（`Assets/_Project/Prefabs/MainCamera.prefab`）のインスタンスと、`BootScene` を付けた GameObject（名前 `BootScene`）。
3. `Home.unity`：`MainCamera` のインスタンス、`HomeScene` の GameObject、`Canvas`（Screen Space Overlay、`CanvasScaler` は Scale With Screen Size・1080×1920、`GraphicRaycaster`）、その子に開始ボタン（`Image` に `AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd")`、`Button`、`UiButtonRelay`。中央、400×160。Text の子は付けない）、`EventSystem`（`EventSystem` と `InputSystemUIInputModule`。型は `Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem")` で取り、Editor の asmdef は変えない）。`HomeScene._startButton` に `SerializedObject` でボタンの `UiButtonRelay` を入れる。
4. `Run.unity` を開き、`RunScene` を付けた GameObject（名前 `RunScene`）を足して保存。
5. `EditorBuildSettings.scenes` を Boot、Home、Run の順にする。
6. `AssetDatabase.SaveAssets()`。

Run（エディタを閉じて、バックグラウンド）:
`"/Applications/Unity/Hub/Editor/6000.5.10f1/Unity.app/Contents/MacOS/Unity" -batchmode -nographics -projectPath "$PWD" -executeMethod PictoArcher.Editor.SceneScaffold.Build -quit -logFile Logs/scaffold.log`
Expected: 終了コード 0。`Logs/scaffold.log` に `error` がない。ファイルが5つできて（直って）いる。
そのあと `SceneScaffold.cs` と `.meta` を消す（コミットしない）。

- [ ] **Step 4: テストが通ることを確かめる**

Run: `tools/run-editmode-tests.sh`、そのあと `tools/run-playmode-tests.sh`
Expected: どちらも終了コード 0、`failed="0"`。

- [ ] **Step 5: コミット**

`git status --short` で `Resources/`、`Scenes/`、`Tests/PlayMode/` の `.meta` も足す。`git diff ProjectSettings/` は `EditorBuildSettings.asset` だけを足す（ほかの ProjectSettings の変化はコミットせず、人に伝える）。
```bash
git commit -m "feat: add Boot and Home scenes, Main prefab and scene flow play mode test"
```

---

### Task 6: 文書（スキル、落とし穴、開発ルール、CLAUDE.md、レビュー担当、計画1）

**Files:**
- Create: `.claude/skills/pictoarcher-coding-rules/SKILL.md`、`docs/unity-pitfalls.md`
- Modify: `docs/development-rules.md`（§2 と、新しい節）、`CLAUDE.md`、`.claude/agents/task-reviewer.md`、`docs/superpowers/plans/2026-10-26-plan1-core-loop.md`（タスク8〜10の頭）

- [ ] **Step 1: スキル `pictoarcher-coding-rules` を書く**

frontmatter：`name: pictoarcher-coding-rules`、`description:` は「PictoArcher の C#（Assets/_Project/Scripts）を書く・直す・レビューする前に使う。Update・Find・コルーチン・シングルトン・継承を書きたくなったとき、新しい敵・System・View・Definition を足すとき」の趣旨（英語でよい。ほかのスキルに合わせる）。本文（日本語）に入れるもの：
1. 8項目の要点（設計書 §5 を1項目2〜3行で）と、迷ったときの判断（「Unity が直接呼ぶものは Relay で受けて Tick で処理」「状態は普通の C#、数値は SO」）
2. お手本（短いコード）：シーン（`RunScene` の形）、System（コンストラクター、`ITickable`、`IDisposable`）、View（参照を持ち、動かすメソッドだけ）、`DefinitionAsset<T>` と役割の抽象定義と動く部品の1組（設計書 §8 の `MovementDefinition`／`ChaseMovementDefinition` を例に。仮の名前だと明記）、`CollisionRelay2D` と `UiButtonRelay` の使い方、イベントの登録と解除、`TickList` の使い方
3. 「こう書きたくなったら、代わりにこう書く」の表（Update／Start／Awake、Find・Camera.main、コルーチン・Invoke、シングルトン・static、インスペクターの onClick、`performed +=`、敵ごとのクラスのコピー、`public` のフィールド、文字列のレイヤー）
4. ガードテストが落ちたときの読み方と、「例外を足すのは設計の変更。PR に理由を書く」
5. 落とし穴は `docs/unity-pitfalls.md` を読む、と書く

- [ ] **Step 2: `docs/unity-pitfalls.md` を書く**

初めて Unity を使う人向け。設計書 §12 の15項目を、1項目ずつ「何が起きるか／なぜ／どうする（このプロジェクトの規則）」の3行で書く。

- [ ] **Step 3: `docs/development-rules.md` を直す**

§2 命名：「`static readonly` は `camelCase`」→ const と static readonly は PascalCase。「ScriptableObject の調整値：public のフィールドで camelCase」→ `[SerializeField] _camelCase` と読み取り専用のプロパティ。「クラスは継承させないなら sealed」→ 継承は骨組みだけ、ほかは sealed。イベントの名前（過去形、`On` なし、受けるのは `HandleXxx`）と接尾辞の表（設計書 §9）を足す。
§2 のあとに「§2.1 設計の規則（8項目）」を足す（設計書 §5 を各2行で。正本は設計書とスキル、と書く）。§1 の表にスキルと `docs/unity-pitfalls.md` を足す。

- [ ] **Step 4: `CLAUDE.md` を直す**

見出し4つ（`## 文字ゼロのルール`、`## フォルダ構成`、`## テストの書き方`、`## シーンとブランチ`）は文字を変えずに残す。
- 「作業を始める前に」のあとに短い節「## 実行順と設計の規則」：標準ライフサイクルは `Main` だけ、`Main → SceneBase → シーン → 各クラス`、C# を書く前にスキル `pictoarcher-coding-rules` を読む、規則を破ると `ArchitectureGuardTests` が落ちる、落とし穴は `docs/unity-pitfalls.md`。
- 「フォルダ構成」のツリーを設計書 §10 と今のファイルに合わせる（`Core/Common` など、`Game/Main`・`Scenes`・`Common`・`Enemies`、`Resources/Main.prefab`、`Scenes/Boot.unity`・`Home.unity`、`Tests/PlayMode/`、`tools/run-playmode-tests.sh`、`docs/unity-pitfalls.md`、スキル）。まだないフォルダ（`Player/`、`UI/` など）は「それぞれのタスクで足す」と書く。
- フォルダのルールの「Unity の部品は `Scripts/Game` に置く」の下に、名前空間はフォルダで分けないことを足す。
- 「テストの書き方」に PlayMode テストの実行方法（`tools/run-playmode-tests.sh`、結果は `Logs/playmode-results.xml`）を足す。
- 「シーンとブランチ」に「Boot と Home は C さんが編集する」を足す（Run と同じ扱い）。

- [ ] **Step 5: `.claude/agents/task-reviewer.md` を直す**

「このプロジェクトで必ず見ること」の「`Scripts/Core` が `UnityEngine` を使っていない」を「`Scripts/Core` が Unity の部品を使っていない（数学の型だけ）」に直し、次を足す：
- 実行順：標準ライフサイクルは `Main` だけ。新しい System・View がシーンの Tick から1行ずつ呼ばれている。Init と Dispose が対になっている（登録したものを外している）。
- やり取り：Find・シングルトン・static の書き換えられる値・インスペクターの onClick を使っていない。当たり判定と UI は Relay を通している。入力は Tick で読んでいる。
- 継承は骨組みだけ、ほかは sealed。敵の違いはデータと部品の組み合わせで出している（クラスのコピーで増やしていない）。
- `ArchitectureGuardTests` に例外を足していないか。足しているなら PR に理由があるか。

- [ ] **Step 6: 計画1のタスク8〜10に注記を足す**

`docs/superpowers/plans/2026-10-26-plan1-core-loop.md` の `### Task 8`・`### Task 9`・`### Task 10` の見出しのすぐ下に、同じ1段落を足す（中身は書き直さない）：
> **注（2026-11-14）：** このタスクは `docs/superpowers/specs/2026-11-14-coding-rules-design.md` の規則に読み替えて作る。下のコードの `Update`・`OnTriggerEnter2D`・static の `EnemyRegistry`・public のフィールドは、そのまま写さない。シーンの Tick から呼ばれる System と View、`CollisionRelay2D`、`RunScene` が持つ `EnemyRegistry`、SO のプロパティを使う。書く前にスキル `pictoarcher-coding-rules` を読む。

- [ ] **Step 7: CLAUDE.md の見出しのテストが通ることを確かめる**

Run: `tools/run-editmode-tests.sh`
Expected: 終了コード 0（`ProjectSmokeTests.ClaudeMd_HasFourRules` を含む）。

- [ ] **Step 8: コミット**

```bash
git commit -m "docs: add coding rules skill, Unity pitfalls and update rules for the execution order"
```

---

### Task 7: 仕様書（build_spec_xlsx.py と .xlsx）、設計書と計画のコミット

**Files:**
- Modify: `tools/spec/build_spec_xlsx.py`、`docs/PictoArcher_仕様書.xlsx`（スクリプトで作り直す）
- Add: `docs/superpowers/specs/2026-11-14-coding-rules-design.md`、`docs/superpowers/plans/2026-11-14-coding-rules.md`

- [ ] **Step 1: `build_spec_xlsx.py` を直す**

- `VERSION = "1.2"`（`BUILT` は `"2026-11-14"` のまま）。
- `TASKS` の `P1-01` の前に1行：`("P1-00", "計画1", "コーディング規則と実行順の土台（Main・SceneBase・ArchitectureGuardTests・スキル）", "C", "P1-01", 3, 3, 1, "進行中", "#4", "設計書 2026-11-14-coding-rules-design.md")`（`PROJECT_START` 2026-10-26 から数えて今日は3週目）。
- `P1-08`・`P1-09`・`P1-10` の「先に要るもの」の先頭に `P1-00, ` を足す。`P3-05` のメモに「Boot と Home のシーンの骨組みは P1-00 で作った。ここでは画面を作る」と書く。
- `build_rules` の命名の表：「private のフィールド」の行から「static readonly は camelCase」を消し、「const と static readonly：PascalCase（TotalWaves）」の行を足す。「ScriptableObject の調整値」の行を「[SerializeField] の _camelCase と、読み取り専用のプロパティ（_moveSpeed／MoveSpeed）」にする。「イベント」と「接尾辞」の行を足す（設計書 §9）。命名の表のあとに見出し「設計の規則（8項目）」と、設計書 §5 の8項目を1行ずつの表（項目、要点、参照は「設計書 2026-11-14 §5.x」）で足す。
- `build_history` に1行：版 1.2、日付 2026-11-14、「コーディング規則と実行順の土台（P1-00）を作業工程と開発ルールに足した」。

- [ ] **Step 2: xlsx を作り直す**

Run: `python3 tools/spec/build_spec_xlsx.py`
Expected: 終了コード 0。`python3 -c "import openpyxl; wb=openpyxl.load_workbook('docs/PictoArcher_仕様書.xlsx'); print([c.value for c in next(r for r in wb['作業工程'].iter_rows() if r[0].value=='P1-00')][:5])"` が `['P1-00', '計画1', ...]` を出す（シート名・列が違ったら、スクリプトの作業工程のシート名に合わせて確かめる）。

- [ ] **Step 3: コミット**

```bash
git add tools/spec/build_spec_xlsx.py "docs/PictoArcher_仕様書.xlsx" docs/superpowers/specs/2026-11-14-coding-rules-design.md docs/superpowers/plans/2026-11-14-coding-rules.md
git commit -m "docs: add P1-00 coding rules to spec sheet and commit design and plan"
```

---

### 最後の確認（タスクではない）

- [ ] `tools/run-editmode-tests.sh` と `tools/run-playmode-tests.sh` がどちらも終了コード 0。
- [ ] `git status --short` に自分の変えたファイルが残っていない（`ProjectSettings/SceneTemplateSettings.json` と `Assets/_Project/Scripts 2` は残ってよい）。
- [ ] 山田さんに伝えること：Unity のエディタで Boot を開いて再生すると Home → ボタンで Run に進むこと、`Scripts 2` という空のフォルダがあること、`CLAUDE.md` の「NoTextGuardTests を含む全テスト」は P1-03 が未着手なのでまだ存在しないこと。
- [ ] push と下書き PR（本文に `Closes #4`、触ったファイル、設計書からのずれ4つ）は OK をもらってから。

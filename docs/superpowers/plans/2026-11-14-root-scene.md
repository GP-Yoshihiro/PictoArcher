# Root Scene と重ねシーン 実装計画

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 消えない Root Scene の上に Home／Battle／Result を加算で入れ替え、Pause を重ねられるようにする。

**Architecture:** `Main`（Root.unity に置く）が `SceneTransform` を呼び、`SceneTransform` が `SceneFlow`（Core）の依頼に従ってシーンを読み込み・外し・重ね、各 `SceneBase` を Tick する。

**Tech Stack:** Unity 6000.5.10f1、URP 2D、uGUI、Input System、NUnit（EditMode／PlayMode）

**Spec:** `docs/superpowers/specs/2026-11-14-root-scene-design.md`

## Global Constraints

- Unity の標準ライフサイクルは `Main` だけ。`SceneManager` は `Main` と `SceneTransform` だけ。
- 継承してよい骨組みは `SceneBase` と `DefinitionAsset<T>` だけ。クラスは `sealed`。1ファイル1型。
- `Scripts/Core` は Unity を使わない。
- 画面に文字を出さない。ボタンは絵だけ（▶ ⏸ 🏠 ↻）、文字の子を持たない。
- シーンの `.unity` は C さんの担当（この PR は C さんのもの）。
- コミットは触ったファイルだけをパスで指定する。名前を変えるファイルは `git mv`（`.meta` の GUID を残す）。

## Review Focus

1. Pause を開いたまま 🏠：Pause と Battle が上から Dispose され、残るのは Root と Home だけ。→ Task 3 の PlayMode。
2. ⏸ の連打：2回目の依頼は false、Pause は1つだけ。→ Task 1 の EditMode。
3. 重ねシーンの `Dispose` や `OnOverlayClosed` の例外：閉じる処理は最後まで進む。→ PlayMode で作れない（重ねシーンは Build Settings の名前で読む）ので、レビューで try/catch を確かめる。
4. 閉じたあと `Time.timeScale` が `GameClock` の値に戻る（`Paused` を消さない）。→ Task 3 の PlayMode。
5. エディタで Home や Sandbox から再生：Root が足され、開いていたシーンがそのまま始まる。→ Task 2 の PlayMode（テスト用シーンを外からアクティブにする形で確かめる）と手動確認。

---

### Task 1: Core の SceneFlow に重ねシーンを足す

**Files:** Modify `Scripts/Core/Common/SceneFlow.cs`／Create `SceneRequest.cs`、`SceneRequestKind.cs`（同じフォルダ）／Modify `Tests/EditMode/SceneFlowTests.cs`、`GameContextTests.cs`／型引数の変更だけ `Main.cs`、`GameContext.cs`／Create `Scripts/Game/Main/OverlayId.cs`

**Interfaces（Produces）:**
- `enum SceneRequestKind { Change, OpenOverlay, CloseOverlay }`
- `readonly struct SceneRequest<TScene, TOverlay>`：`Kind`、`Scene`、`Overlay`、`Result`（int）
- `sealed class SceneFlow<TScene, TOverlay>`：`Phase`、`HasPending`、`OverlayCount`、`IsOverlayOpen(TOverlay)`、`OverlayAt(int)`（0 が一番下）、`Request(TScene)`、`RequestOpenOverlay(TOverlay)`、`RequestCloseOverlay(int)`、`TryBegin(out SceneRequest<,>)`、`EnterRunning()`、`Restart()`、`CancelOverlayOpen()`
- `enum OverlayId { Pause }`

- [ ] **Step 1: テストを書く**（`TestOverlay { X, Y }`）
  - `OpenOverlay_WhenRunning_IsAccepted`／`OpenOverlay_WhilePending_ReturnsFalse`／`OpenSameOverlayTwice_SecondReturnsFalse`（Y は true）
  - `TryBegin_Open_PushesOverlayAndEntersLoading`（Kind、Overlay、`OverlayCount` 1、Loading）
  - `CloseOverlay_WithoutOverlay_ReturnsFalse`
  - `TryBegin_Close_PopsAndReturnsResultAndStaysRunning`（結果 7）
  - `TryBegin_Change_ClearsOverlays`（X、Y のあと `Request(B)`：Change、`OverlayCount` 0、Loading）
  - `CancelOverlayOpen_PopsAndReturnsToRunning`、`CancelOverlayOpen_WhenNotLoading_Throws`
  - `Restart_ClearsOverlaysAndPending`、`OverlayAt_IsBottomFirst`
  - 既存のテストは `TryBeginLoad` → `TryBegin` に直し、Kind が Change であることも見る。`GameContextTests` に `OpenOverlay_ForwardsToFlow`、`CloseOverlay_ForwardsResult`（Task 2 で GameContext に足すまでコンパイルしないので、Task 2 の Step 1 で書く）。
- [ ] **Step 2:** `tools/run-editmode-tests.sh`　Expected: コンパイルエラー。
- [ ] **Step 3:** 実装。重ねシーンは `List<TOverlay>`、同じかどうかは `EqualityComparer<TOverlay>.Default`。
- [ ] **Step 4:** Expected: 全部通る。
- [ ] **Step 5:** コミット `feat: let SceneFlow stack overlay scenes`

### Task 2: Root Scene 方式（SceneTransform、Root／Home／Battle／Result）

**Files:**
- Create: `Scripts/Game/Main/SceneTransform.cs`、`Scripts/Game/Scenes/ResultScene.cs`、`Scenes/Result.unity`
- Rename（`git mv`）: `BootScene.cs` → `RootScene.cs`、`Boot.unity` → `Root.unity`、`RunScene.cs` → `BattleScene.cs`、`Run.unity` → `Battle.unity`
- Modify: `Main.cs`、`GameContext.cs`、`SceneBase.cs`、`SceneId.cs`（`Home, Battle, Result`）、`HomeScene.cs`（▶ で `SceneId.Battle`）、`Home.unity`（カメラと EventSystem を外す）、`ProjectSettings/EditorBuildSettings.asset`
- Delete: `Resources/Main.prefab`（と空になる `Resources/`）
- Test: `SceneSetupTests.cs`、`ArchitectureGuardTests.cs`（`SceneManager` を `SceneTransform` にも許す）、`GameContextTests.cs`、`Tests/PlayMode/SceneFlowPlayModeTests.cs`、`Tests/PlayMode/DisposeThrowsScene.cs`、Create `Tests/PlayMode/HostScene.cs`
- 一時ファイル：`Scripts/Editor/RootSceneScaffold.cs`（シーンを作ったら消す。コミットしない）

**Interfaces（Produces）:**
- `sealed class SceneTransform`：`SceneTransform(SceneFlow<SceneId, OverlayId> flow, GameContext context, Scene root)`、`void Update(float deltaTime, float unscaledDeltaTime)`（4.4 の 2〜4）、`void FixedTick(float)`、`void LateTick(float deltaTime, float unscaledDeltaTime)`、`bool FreezesTime`、`void Dispose()`、`const string RootSceneName = "Root"`
- `GameContext`：`OpenOverlay(OverlayId) : bool`、`CloseOverlay(int result = 0) : bool`
- `SceneBase`：`virtual bool KeepsBelowRunning => false`、`virtual void OnOverlayClosed(OverlayId id, int result) {}`
- `RootScene`（初期化用）：`[SerializeField] SceneId _firstScene = SceneId.Home`、`SceneId FirstScene`。`Init` はほかのどのシーンよりも先に呼ばれ、`Dispose` は最後に呼ばれる
- `ResultScene`：`[SerializeField] UiButtonRelay _homeButton, _retryButton`（🏠 → Home、↻ → Battle）
- `HostScene`（PlayMode テスト用）：`GameContext Context`、`int TickCount`、`OverlayId? LastClosed`、`int LastResult`

- [ ] **Step 1: テストを書く**
  - `SceneSetupTests`：
    - `BuildSettings_ListRootThenSceneIdsThenOverlayIds`（Root、Home、Battle、Result、Pause。Pause は Task 3 で足すので、この Task では Root＋`SceneId` までを確かめ、Task 3 で `OverlayId` を足す）
    - `Root_HasMainRootSceneCameraAndEventSystem`（ルートに `Main` 1つ、`RootScene` 1つ、`Camera` 1つ、`EventSystem` 1つ）
    - `Scene_HasOneRootSceneBaseOfType`：Root／Home／Battle／Result
    - `ContentScenes_HaveNoCameraListenerEventSystemOrMain`（`SceneId` の全シーン）
    - `ResultButtons_AreWiredAndHaveNoText`。Home の既存のボタンのテストはそのまま。`MainPrefab_HasMain` は消す。
  - `ArchitectureGuardTests`：`SceneManager` の許可を `Main` と `SceneTransform` にし、メッセージも直す。
  - `GameContextTests`：Task 1 の2本。
  - PlayMode（各テストの最初に `SceneManager.LoadScene("Root")`。毎回新しい Main になる）：
    - `Root_GoesToHome_ThenStartGoesToBattle`：アクティブが Home → ▶ → Battle。`sceneCount` は 2（Root と Battle）。
    - `HostRequestsResult_ThenHomeAndRetryButtonsWork`：テスト用のシーンを作って `HostScene` を置き、アクティブにする（外からの切り替え。Home は外される）→ `Context.RequestScene(Result)` → Result の 🏠 で Home。もう一度 Result にして ↻ で Battle。
    - `SceneWhoseDisposeThrows_StillMovesToNextScene`：今のテストを Root から始める形に直す。
    - `SceneOutsideAssetsWithoutSceneBase_OnlyWarns`：同上。
    - `RootInit_RunsBeforeAdoptedScene`：テスト用の `HostScene` を置いたシーンを開いた状態から Root を加算で読み込む → `HostScene` の `Init` の時点で `RootScene` がもう `Init` 済み（`RootScene.IsInitialized` を見る。テスト用に読み取り専用のプロパティを1つ足す）。
- [ ] **Step 2:** EditMode と PlayMode を走らせる　Expected: コンパイルエラー、またはシーンがないので失敗。
- [ ] **Step 3: 実装する**（設計書 §4.2〜4.6）
  - `Main`：`Awake` で `GameClock`、`SceneFlow`、`GameContext`、`SceneTransform`（`gameObject.scene` を Root として渡す）を作る。`Update` は 時計 → `_transform.Update(Time.deltaTime, Time.unscaledDeltaTime)` → `Time.timeScale = _transform.FreezesTime ? 0f : _clock.EffectiveTimeScale`。`FixedUpdate`／`LateUpdate`／`OnDestroy` は `SceneTransform` に渡すだけ。`[RuntimeInitializeOnLoadMethod(AfterSceneLoad)]` で Root がなければ `LoadScene(RootSceneName, Additive)`。`DontDestroyOnLoad` と `Resources.Load` はやめる。
  - `SceneTransform`：Root の `SceneBase` を最初に `Init`。アクティブなシーンが Root でなければそれを中身として始め、Root なら `RootScene.FirstScene` を依頼する。入れ替えは「重ねを上から片付けて外す → 中身を片付けて外す → 次を `Additive` で読み込む → 終わったら `SetActiveScene` して `Init`」。外からアクティブなシーンが変わったら §4.6 のとおり。`GameContext.OpenOverlay`／`CloseOverlay` はこの Task で足すが、`SceneTransform` は開く依頼をエラーのログにして `CancelOverlayOpen()` で取り消すだけにする（処理は Task 3 で足す。この Task のテストは重ねシーンを使わない）。
  - 一時エディタスクリプト（前回の `SceneScaffold` と同じ作り方）：
    - Root：`Main`（GameObject に `Main`）、`RootScene`、`MainCamera` プレハブ、`EventSystem`（InputSystemUIInputModule）。
    - Home：今のものから `MainCamera` と `EventSystem` を外す。
    - Battle：今の Run の中身（`Global Light 2D`、`BattleScene`）。
    - Result：`ResultScene`、Canvas（Screen Space Overlay、CanvasScaler 1080×1920、GraphicRaycaster）、`HomeButton` と `RetryButton`（組み込みの UISprite、仮の図形の子 Image）。
    - Build Settings を Root、Home、Battle、Result にする。
  - スクリプトを消し、ログにエラーがないか確かめる。
- [ ] **Step 4:** EditMode と PlayMode　Expected: 全部通る。
- [ ] **Step 5:** コミット（2つに分ける）`refactor: rename Boot to Root and Run to Battle` と `feat: load scenes additively over a persistent Root scene`

### Task 3: 重ねシーンと Pause

**Files:** Modify `SceneTransform.cs`、`BattleScene.cs`、`Battle.unity`、`EditorBuildSettings.asset`、`SceneSetupTests.cs`、`SceneFlowPlayModeTests.cs`／Create `Scripts/Game/Scenes/PauseScene.cs`、`Scenes/Pause.unity`／一時ファイル `Scripts/Editor/PauseSceneScaffold.cs`

**Interfaces:** Consumes Task 1・2 のもの。Produces `PauseScene`（`_resumeButton` → `CloseOverlay(0)`、`_homeButton` → `RequestScene(Home)`）、`BattleScene._pauseButton` → `OpenOverlay(Pause)`。

- [ ] **Step 1: テストを書く**
  - `SceneSetupTests`：Build Settings に Pause を足す、`Scene_HasOneRootSceneBaseOfType("Pause", PauseScene)`、`OverlayScenes_HaveNoCameraListenerEventSystemOrMain`、`BattlePauseButton_IsWiredAndHasNoText`、`PauseButtons_AreWiredAndHaveNoText`。
  - PlayMode：
    - `Battle_PauseOpens_StopsTime_ResumeCloses`：Home → Battle → ⏸ → `sceneCount` 3、`timeScale` 0 → ▶ → `sceneCount` 2、`timeScale` 1。
    - `Battle_PauseThenHome_LeavesOnlyRootAndHome`（Review Focus 1）。
    - `HostOpensPause_StopsHostTicks_AndGetsResult`：`HostScene` が Pause を開く → 2フレームで `TickCount` が増えない → `Context.CloseOverlay(3)` → `LastClosed` が Pause、`LastResult` が 3、`TickCount` がまた増える。
    - `ClosePause_KeepsClockPaused`：`Context.Clock.Paused = true` で開いて閉じる → `timeScale` 0 のまま（Review Focus 4）。
- [ ] **Step 2:** 走らせる　Expected: 失敗。
- [ ] **Step 3: 実装する**：`SceneTransform` に重ねシーンの一覧（`List<SceneBase>`、`List<Scene>`）、開く（`Additive`、アクティブは変えない、`SceneBase` が1つでなければエラーと取り消し）、閉じる（`Dispose` → 非表示 → 外す → 下の `OnOverlayClosed`、どれも try/catch）、動かす範囲（§4.4）、`FreezesTime`。シーンは一時エディタスクリプトで作る：Pause（`PauseScene`、Canvas Sort Order 100、全面の黒アルファ 0.6 の板、`ResumeButton`、`HomeButton`）、Battle に Canvas と右上の `PauseButton`。Build Settings の最後に Pause。
- [ ] **Step 4:** 全テスト　Expected: 全部通る。
- [ ] **Step 5:** コミット `feat: add overlay scenes and Pause`

### Task 4: 文書・スキル・仕様書

**Files:** `docs/superpowers/specs/2026-11-14-coding-rules-design.md`、`.claude/skills/pictoarcher-coding-rules/SKILL.md`、`docs/unity-pitfalls.md`、`CLAUDE.md`、`docs/development-rules.md`、`.claude/agents/task-reviewer.md`、`.claude/agents/transcriber.md`、`.github/pull_request_template.md`、`tools/spec/build_spec_xlsx.py` と `.xlsx`、`docs/superpowers/plans/2026-11-14-coding-rules.md` と計画1（古い名前の所に注記だけ）、この設計書と計画

- [ ] **Step 1:** Boot → Root、Run → Battle（シーン名・クラス名・`SceneId` の所だけ。遊びの「ラン」という言葉は変えない）、`Resources/Main.prefab` の記述を Root.unity に、`SceneManager` の許可、重ねシーンの書き方（`OpenOverlay`／`CloseOverlay`／`OnOverlayClosed`／`KeepsBelowRunning`、§4.7 の規則）を書く。`CLAUDE.md` の「C さんだけが編集する」シーンを Root、Home、Battle、Result、Pause にする。
- [ ] **Step 2:** `python3 tools/spec/build_spec_xlsx.py`（版 1.4）　Expected: エラーなし。
- [ ] **Step 3:** `grep -rn "Run\.unity\|RunScene\|Boot\.unity\|BootScene\|Main\.prefab"` で残りを確かめる（古い計画の注記以外に出ない）。全テスト。
- [ ] **Step 4:** コミット `docs: describe Root scene and overlays in rules, skill and spec sheet`

### Task 5: 全体のレビューと PR

- [ ] ブランチ全体を `task-reviewer` に見てもらい、Critical／Important を1回で直す。
- [ ] PR #5 の本文に「Root Scene 方式」を足す。
- [ ] 人の OK をもらってから push。

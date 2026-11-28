# C-07 Home・クリア画面・死亡画面 実装計画

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Home に ▶ と最高記録、Result にクリア画面（🏆・⏱・コイン）と死亡画面（💀・到達ウェーブ・武器と強化のアイコン）を作り、Battle から Result へプレイの結果を渡す口を用意する。

**Architecture:** プレイの結果は `RunResult`（Game、作ったら変わらない）にまとめ、`GameContext.FinishRun(result)` で Result へ切り替えると同時に `GameContext.LastRun` と最高記録 `BestRecord`（Core）に記録する。Home と Result は `GameContext` から読んで、`Game/UI` の View に表示させるだけ。絵はすべて見た目の表 `UiIconVisual` から取る。

**Tech Stack:** Unity 6000.5.10f1、URP 2D、uGUI、TextMeshPro（数字と記号だけの `Digits.asset`）、NUnit（EditMode／PlayMode）

**Spec:** `docs/superpowers/specs/2026-10-31-pictoarcher-design-v3.md` §5（文字ゼロ）・§6（進行フロー）・§10（HUD）・§12（見た目の表）。Issue #15。

---

## 人に確認すること（実装の前に決める）

推奨を先に書いた。違えばその項目だけ直す。決まるまで、関係するタスクは始めない。

| # | 決めること | 推奨 | ほかの案 | 関係するタスク |
|---|---|---|---|---|
| Q1 | **Battle から Result への配線**。`BattleScene.cs` と `Battle.unity` は C-05（#7）の範囲なので、C-07 では触らない。Issue の終わりの条件「Home → Battle → クリア／死亡 → Home」は、本物の Battle がまだクリアも死亡もしないので、そのままでは満たせない | C-07 は渡す口（`GameContext.FinishRun`）と受け取る側まで。PlayMode では Battle の代わりにテスト用の `HostScene` が `FinishRun` を呼ぶ。Battle → Result の1行は C-05 で足す。Issue #15 の終わりの条件を「Home → ▶ → Battle、（テストのシーンから）クリア／死亡 → Result → 🏠 Home／↻ Battle」に直す | C-07 で `BattleScene` に仮の操作（キーでクリア／死亡）を足す（今回の指示では禁止なので、選ぶなら人が決める） | 2、6 |
| Q2 | **クリアタイムの書き方**。数字フォント `Digits.asset` は `0123456789+-×/%` だけで、`:` がない。50ウェーブは 20〜30分 | `:` をフォントに足して `⏱ 23:45`（分:秒）。`NoTextGuardTests` の `Required` にも `:` を足す。設計書 §5 の「代わりの表記」に `⏱ 23:45` を足し、`tools/spec/build_spec_xlsx.py` と `.xlsx` も同じ PR で直す（§10 のルール） | 秒だけ `⏱ 1425`（フォントを触らないが読みにくい） | 1、3 |
| Q3 | **武器の型 `WeaponId`** がまだない（A-02 #31 は未着手）。死亡画面に「選んだ武器」を出すのに要る | C-07 で `Scripts/Game/Data/WeaponId.cs`（`enum WeaponId { Gun, Laser, Launcher }`）を足し、A さんに「A-02 と C-13 はこれを使う」と伝える | `RunResult` に武器を入れず、A-02／C-13 で足す（死亡画面の武器の枠は空のまま） | 2、4 |
| Q4 | **🏆 と死亡の 💀 の絵の枠**。`UiIconVisual` に 🏆 の枠がない。💀 は `Boss`（ボスの HP バー用）があるが、意味が違う | `UiIconVisual` に `_trophy`（🏆 クリア）と `_defeat`（💀 死亡）を足す。仮の絵は `Art/Placeholder/UI/Trophy.png` を新しく描き、`_defeat` は今の `Boss.png`（白い髑髏）を指しておく。本番の絵が別に届けば枠に入れ替えるだけ | `_defeat` を足さず `Boss` を使い回す | 3 |
| Q5 | **Core の置き場所**。最高記録 `BestRecord` に合う Core のフォルダがない（Core は `Common` `Combat` `Movement` `Waves` `Skills`） | `BestRecord` は `Core/Waves/`（到達ウェーブとクリアタイムの記録なので）、表示の整形 `DigitText` は `Core/Common/` | `Core/Run/` を作る（`CLAUDE.md`、スキル、コーディング規則の設計書のフォルダ一覧も直すことになる） | 1 |
| Q6 | **Unity のエディタ作業**（`Home.unity`・`Result.unity` に UI を置く、`.meta` を作る、テストを流す）にディスクの空き（今 約7GB）が足りるか、誰がするか | 空きを確かめてから、Claude がエディタを開いて行う（computer-use）。人はタスク5のあとに画面を見て確かめる | 人がタスク5の表どおりに手で置く | 3、5、6 |

決めて書いておくこと（違えば直す）：
- 記録がまだないとき（保存なし・まだ1回も遊んでいない）は、Home の記録の段を隠し、▶ だけを出す。
- クリア済みなら Home は `⏱ 最短タイム` だけを出し、`50/50` は出さない（§6 のとおり）。
- 最高記録は C-14（保存）までメモリだけに持つ。ゲームを閉じると消える。
- 死亡画面の強化のアイコンは、同じ強化を重ねて取っても1つだけ出し、取った順に並べる（重ねた数は出さない）。
- `LastRun` がないまま Result が開いたとき（今の PlayMode テストのように外から `RequestScene(SceneId.Result)` したとき）は、クリアの板も死亡の板も隠し、↻ と 🏠 だけを出す。
- ↻ は今は `RequestScene(SceneId.Battle)` のまま。「同じ武器でもう一度」は C-13 で武器を `GameContext` に持たせたときに直す。

## あとのタスクで行う統合

| 何を | どのタスクで | すること |
|---|---|---|
| Battle → Result | C-05（#7） | 50ウェーブ目のクリアとプレイヤーの死亡で `_context.FinishRun(new RunResult(...))` を呼ぶ。クリアタイムは Battle の Tick の `deltaTime` の合計（一時停止中は Tick が止まるので数えない）。コインは `IRunStatus.Coins` |
| 選んだ武器 | A-02（#31）、C-13（#16） | 武器の種類に `WeaponId` を使う。C-13 で選んだ武器を `GameContext` に持たせ、`RunResult` と ↻ に使う |
| 取った強化 | A-03（#29）、A-05（#30） | 取った強化を `IReadOnlyList<SkillId>` で渡す。`Data/Skills/` の `SkillDefinition` を Result の `DefeatView._skills` に入れる（C-09 の HUD でも使うので、そのとき1つの一覧にまとめるかを決める） |
| 最高記録の保存 | C-14（#17） | `RootScene.Init` で読み込んだ値から `new BestRecord(bestWave, hasCleared, bestClearSeconds)` を作って `Main` が `GameContext` に渡す。`FinishRun` のあとに保存する |
| 初回はチュートリアル | C-18（#20） | 記録がないときの行き先を変える（C-07 では Home のまま） |
| 本番の絵 | 絵が届いたとき（C-21 #24 ほか） | `UiIconVisual` の枠にアタッチするだけ |

## Global Constraints

- 画面に文字を出さない（§5）。ボタンは絵だけで、`Text`／`TMP_Text` の子を持たない。TMP の文字は数字と記号だけ（シーンに保存する初めの値も `0/50`、`0:00`、`0` のように数字だけ）。進行は `23/50`、時間は `⏱`＋数字。
- 色の意味：黄＝コイン・経験値。コインの数字とアイコンだけ黄色（`#F5C542`）にし、🏆 は黄色にしない（白の仮の絵）。ほかの文字は白。
- 絵はすべて `UiIconVisual` から取る。シーンの `Image` に絵を直接入れない（絵が届いたら表に入れるだけで替わるように）。
- Unity の標準ライフサイクルは `Main` だけ。View は `Init(...)`／`Show(...)` を持ち主のシーンが呼ぶ。ボタンは `UiButtonRelay`。`Find` 系、static の書き換えられる値、シングルトンは使わない。クラスは `sealed`、1ファイル1型。
- `Scripts/Core` は Unity を使わない。名前空間は `PictoArcher.Core`／`PictoArcher.Game` だけ。
- `BattleScene.cs`、`Battle.unity`、A・B さんのファイルとプレハブは触らない。`Pause.unity` も触らない（C-15）。
- Canvas は今のシーンのもの（Scale With Screen Size、基準 1080×1920）を使い、UI は画面の端か中央を基準に置く。
- 仮の絵はフリー素材を使わず、今の `Art/Placeholder/UI/` と同じ 128×128・白一色の単純な図形を Python（Pillow）で描く。描くスクリプトはリポジトリに入れない。
- コミットは触ったファイルだけをパスで指定する。

## Review Focus

1. `LastRun` なしで Result が開く（外からの `RequestScene(SceneId.Result)`）→ 例外を出さず、板を隠し、↻ と 🏠 は動く。→ タスク6 の PlayMode（今の `HostRequestsResult_ThenHomeAndRetryButtonsWork` に板が隠れていることを足す）。
2. 同じフレームに `FinishRun` を2回 → 2回目は `false`、記録も `LastRun` も1回目のまま。→ タスク2 の EditMode。
3. クリアタイムが 60分以上・端数・負の値・NaN → `61:05`、切り捨て、`0:00`。→ タスク1 の EditMode。
4. 同じ強化が重なる・`SkillDefinition` がない強化 → 1つだけ出し、絵のないものは飛ばして例外を出さない。→ タスク2（重なりを1つにする）とタスク4（絵のない強化を飛ばす）の EditMode。
5. クリアしたあとに、もっと手前で死ぬ → 最高到達ウェーブと最短タイムは良いほうが残る。→ タスク1 の EditMode。

---

### Task 1: Core の最高記録と数字の整形

**Files:**
- Create: `Assets/_Project/Scripts/Core/Waves/BestRecord.cs`（Q5）
- Create: `Assets/_Project/Scripts/Core/Common/DigitText.cs`（Q5）
- Test: `Assets/_Project/Tests/EditMode/BestRecordTests.cs`、`DigitTextTests.cs`
- Modify（Q2 で `:` に決まったら）：`Assets/_Project/Tests/EditMode/NoTextGuardTests.cs` の `Required` を `"0123456789+-×/%:"` に。フォントの作り直しはタスク3

**Interfaces:**
- Produces:
  - `sealed class BestRecord`：`BestRecord()`、`BestRecord(int bestWave, bool hasCleared, float bestClearSeconds)`（C-14 の読み込み用）、`int BestWave`（0＝記録なし）、`bool HasPlayed => BestWave > 0`、`bool HasCleared`、`float BestClearSeconds`（`HasCleared` のときだけ意味がある）、`void Submit(int reachedWave, bool cleared, float clearSeconds)`
  - `static class DigitText`：`string Progress(int current, int total)`、`string Duration(float seconds)`、`string Count(int value)`。どれも `CultureInfo.InvariantCulture`

- [ ] **Step 1: 失敗するテストを書く**

```csharp
// BestRecordTests
[Test] public void New_HasNotPlayed() { var r = new BestRecord(); Assert.IsFalse(r.HasPlayed); Assert.IsFalse(r.HasCleared); Assert.AreEqual(0, r.BestWave); }
[Test] public void Submit_KeepsHighestWave() { var r = new BestRecord(); r.Submit(23, false, 0f); r.Submit(12, false, 0f); Assert.AreEqual(23, r.BestWave); }
[Test] public void Submit_Cleared_KeepsShortestTime() { var r = new BestRecord(); r.Submit(50, true, 1500f); r.Submit(50, true, 1600f); r.Submit(50, true, 1400f); Assert.IsTrue(r.HasCleared); Assert.AreEqual(1400f, r.BestClearSeconds); }
[Test] public void Submit_DiedAfterClear_KeepsClearAndWave() { var r = new BestRecord(); r.Submit(50, true, 1500f); r.Submit(7, false, 0f); Assert.AreEqual(50, r.BestWave); Assert.IsTrue(r.HasCleared); Assert.AreEqual(1500f, r.BestClearSeconds); }
[Test] public void Submit_NotCleared_IgnoresTime() { var r = new BestRecord(); r.Submit(30, false, 5f); Assert.IsFalse(r.HasCleared); }
[Test] public void Submit_NegativeWave_Throws() => Assert.Throws<ArgumentOutOfRangeException>(() => new BestRecord().Submit(-1, false, 0f));
[Test] public void Constructor_RestoresValues() { var r = new BestRecord(50, true, 1234f); Assert.AreEqual(50, r.BestWave); Assert.IsTrue(r.HasCleared); Assert.AreEqual(1234f, r.BestClearSeconds); }

// DigitTextTests
[Test] public void Progress() => Assert.AreEqual("23/50", DigitText.Progress(23, 50));
[TestCase(0f, "0:00")] [TestCase(59.9f, "0:59")] [TestCase(1425.4f, "23:45")] [TestCase(3665f, "61:05")]
[TestCase(-3f, "0:00")] [TestCase(float.NaN, "0:00")]
public void Duration(float seconds, string expected) => Assert.AreEqual(expected, DigitText.Duration(seconds));
[Test] public void Count() => Assert.AreEqual("1234", DigitText.Count(1234));
[Test] public void Outputs_HaveNoLetters() => Assert.IsFalse((DigitText.Progress(1, 50) + DigitText.Duration(3665f) + DigitText.Count(-5)).Any(char.IsLetter));
```

Q2 が「秒だけ」なら `Duration` の期待値を `"0"`、`"59"`、`"1425"`、`"3665"`、`"0"`、`"0"` にする。

- [ ] **Step 2: テストが失敗するのを確かめる**（Unity のエディタを閉じて `tools/run-editmode-tests.sh`。型がないのでコンパイルエラー）
- [ ] **Step 3: `BestRecord` と `DigitText` を書く**。`Duration` は NaN と負を 0 にしてから `Mathf.FloorToInt` で秒に切り捨て、分は上限なし、秒は2桁。`Submit` は `reachedWave < 0` か `clearSeconds < 0` で `ArgumentOutOfRangeException`
- [ ] **Step 4: `tools/run-editmode-tests.sh` で全部通るのを確かめる**（`Logs/editmode-results.xml` の失敗が 0）
- [ ] **Step 5: コミット** `feat: best record and digit text formatting in Core`

### Task 2: プレイの結果を Result へ渡す口

**Files:**
- Create: `Assets/_Project/Scripts/Game/Data/WeaponId.cs`（Q3）
- Create: `Assets/_Project/Scripts/Game/Run/RunOutcome.cs`、`RunResult.cs`
- Modify: `Assets/_Project/Scripts/Game/Main/GameContext.cs`、`Main.cs`（`new GameContext(flow, _clock, new BestRecord())` の1か所だけ）
- Test: `Assets/_Project/Tests/EditMode/RunResultTests.cs`、`GameContextTests.cs`（作る所を3引数に直し、テストを足す）

**Interfaces:**
- Consumes: `BestRecord`（タスク1）、`WaveLayout.TotalWaves`（今ある。50）
- Produces:
  - `enum WeaponId { Gun, Laser, Launcher }`
  - `enum RunOutcome { Cleared, Died }`
  - `sealed class RunResult`：`RunResult(RunOutcome outcome, int reachedWave, float clearSeconds, int coins, WeaponId weapon, IReadOnlyList<SkillId> skills)`。プロパティ `Outcome`、`ReachedWave`、`ClearSeconds`、`Coins`、`Weapon`、`IReadOnlyList<SkillId> Skills`（重なりを除いて取った順、コピーして持つ）、`bool IsCleared => Outcome == RunOutcome.Cleared`
  - `GameContext(SceneFlow<SceneId, OverlayId> flow, GameClock clock, BestRecord records)`、`BestRecord Records { get; }`、`RunResult LastRun { get; }`（まだなければ null）、`bool FinishRun(RunResult result)`

- [ ] **Step 1: 失敗するテストを書く**

```csharp
// RunResultTests
[Test] public void Skills_AreDistinctInOrder()
{
    var r = new RunResult(RunOutcome.Died, 23, 0f, 10, WeaponId.Laser, new[] { SkillId.Fire, SkillId.AttackUp, SkillId.Fire });
    CollectionAssert.AreEqual(new[] { SkillId.Fire, SkillId.AttackUp }, r.Skills);
}
[Test] public void Skills_AreCopied() { var list = new List<SkillId> { SkillId.Ice }; var r = new RunResult(RunOutcome.Died, 1, 0f, 0, WeaponId.Gun, list); list.Add(SkillId.Fire); Assert.AreEqual(1, r.Skills.Count); }
[Test] public void NullSkills_AreEmpty() => Assert.AreEqual(0, new RunResult(RunOutcome.Cleared, 50, 1f, 0, WeaponId.Gun, null).Skills.Count);
[Test] public void ReachedWaveOutOfRange_Throws() => Assert.Throws<ArgumentOutOfRangeException>(() => new RunResult(RunOutcome.Died, 51, 0f, 0, WeaponId.Gun, null));

// GameContextTests に足す
[Test] public void FinishRun_RequestsResult_AndRecords()
{
    // flow を作り、context.FinishRun(cleared 50, 1500f) → true、flow が SceneId.Result を受け付けた、LastRun が同じもの、Records.HasCleared、BestClearSeconds == 1500f
}
[Test] public void FinishRun_SecondInSameFrame_IsRejectedAndNotRecorded()
{
    // 1回目 Died 23 → true、2回目 Died 40 → false、LastRun.ReachedWave == 23、Records.BestWave == 23
}
```

（2つ目は、今の `GameContextTests` が `flow.Request` の2回目を確かめている形に合わせて書く。）

- [ ] **Step 2: テストが失敗するのを確かめる**（`tools/run-editmode-tests.sh`）
- [ ] **Step 3: 型を書き、`GameContext.FinishRun` を足す**。順番は「`_flow.Request(SceneId.Result)` が false ならすぐ false を返す → `LastRun = result` → `Records.Submit(result.ReachedWave, result.IsCleared, result.ClearSeconds)` → true」。`RunResult` は `reachedWave` が `0..WaveLayout.TotalWaves` の外か `clearSeconds < 0` で `ArgumentOutOfRangeException`。`Main.cs` は `new BestRecord()` を渡すだけ
- [ ] **Step 4: `tools/run-editmode-tests.sh` で全部（`ArchitectureGuardTests` を含む）通るのを確かめる**
- [ ] **Step 5: コミット** `feat: run result handed from battle to result through GameContext`

### Task 3: 🏆・💀 の枠と仮の絵（と `:` のフォント）

Q4（と Q2）が決まってから。Unity のエディタが要る（Q6）。

**Files:**
- Create: `Assets/_Project/Art/Placeholder/UI/Trophy.png`（＋ Unity が作る `.meta`。取り込み設定は今の `Art/Placeholder/UI/*.png` と同じ：Sprite、Bilinear、圧縮なし）
- Modify: `Assets/_Project/Scripts/Game/Data/UiIconVisual.cs`（`[Header("結果")]` に `[SerializeField, Tooltip("🏆 クリア")] Sprite _trophy;`、`[SerializeField, Tooltip("💀 死亡")] Sprite _defeat;` と `Trophy`、`Defeat` のプロパティ。**末尾に足す**）
- Modify: `Assets/_Project/Data/Visuals/UiIconVisual.asset`（`_trophy` → `Trophy.png`、`_defeat` → `Boss.png`）
- Modify（Q2 で `:`）：`Assets/_Project/Fonts/Digits.asset`（Font Asset Creator で今と同じ設定・`Fredoka-Variable.ttf`・文字 `0123456789+-×/%:` で作り直す）、設計書 §5 の「代わりの表記」に `⏱ 23:45` を足す、`tools/spec/build_spec_xlsx.py` の同じ表、`docs/PictoArcher_仕様書.xlsx` を作り直す
- Test: 今ある `VisualTableTests.Tables_HaveNoEmptySlots`、`NoTextGuardTests` で確かめる

- [ ] **Step 1: `Trophy.png` を描く**（128×128、透明の背景に白一色の杯の形。文字を入れない）。描いたら Read で絵を見て、文字がないことを確かめる
- [ ] **Step 2: `UiIconVisual.cs` に枠を足し、エディタで `UiIconVisual.asset` の2つの枠に入れる**
- [ ] **Step 3:（Q2 で `:`）`Digits.asset` を作り直す**
- [ ] **Step 4: `tools/run-editmode-tests.sh` で `VisualTableTests` と `NoTextGuardTests` を含めて全部通るのを確かめる**。`ListSlotsStillUsingPlaceholders` の一覧に `_trophy`、`_defeat` が出ればよい
- [ ] **Step 5: コミット** `feat: trophy and defeat slots with placeholder art`（Q2 なら `feat: add colon to digit font for clear time` を分けてコミット）

### Task 4: Home と Result の View

**Files:**
- Create: `Assets/_Project/Scripts/Game/UI/HomeView.cs`、`ClearView.cs`、`DefeatView.cs`
- Modify: `Assets/_Project/Scripts/Game/Scenes/HomeScene.cs`、`ResultScene.cs`
- Test: `Assets/_Project/Tests/EditMode/DefeatViewTests.cs`（絵の探し方だけ）

**Interfaces:**
- Consumes: `UiIconVisual`（`Play`、`Retry`、`Home`、`Timer`、`Coin`、`Trophy`、`Defeat`、`WeaponGun`／`WeaponLaser`／`WeaponLauncher`）、`BestRecord`、`RunResult`、`DigitText`、`SkillDefinition`（`Id`、`Icon`）
- Produces（タスク5でシーンにつなぐフィールド名）：
  - `HomeView : MonoBehaviour`：`[SerializeField] Image _startIcon; GameObject _recordRow; TMP_Text _bestWaveText; GameObject _bestTimeGroup; Image _timerIcon; TMP_Text _bestTimeText;`。`void Show(BestRecord record, UiIconVisual icons)`：▶ の絵を入れ、`HasPlayed` でなければ `_recordRow` を隠す。`HasCleared` なら `_bestTimeGroup` だけ（`⏱`＋`Duration`）、でなければ `_bestWaveText` だけ（`Progress(BestWave, TotalWaves)`）
  - `ClearView : MonoBehaviour`：`Image _trophy; Image _timerIcon; TMP_Text _timeText; Image _coinIcon; TMP_Text _coinText;`。`void Show(RunResult result, UiIconVisual icons)`、`void Hide()`
  - `DefeatView : MonoBehaviour`：`Image _skull; TMP_Text _waveText; Image _weaponIcon; RectTransform _skillIconRoot; Image _skillIconPrefab; SkillDefinition[] _skills;`。`void Init()`、`void Show(RunResult result, UiIconVisual icons)`、`void Hide()`、`void Dispose()`（作った強化アイコンを Destroy）。`internal static Sprite FindSkillIcon(SkillDefinition[] skills, SkillId id)`（ないか絵が null なら null）
  - `ResultScene`：`[SerializeField] UiIconVisual _icons; ClearView _clearView; DefeatView _defeatView; Image _retryIcon; Image _homeIcon;`（ボタンのフィールド名 `_homeButton`、`_retryButton` は変えない）。`Init` で `LastRun` が null なら両方 `Hide`、`IsCleared` なら `_clearView.Show` と `_defeatView.Hide`、死亡ならその逆。↻・🏠 の絵も表から入れる
  - `HomeScene`：`[SerializeField] UiIconVisual _icons; HomeView _view;`。`Init` で `_view.Show(context.Records, _icons)`。`_startButton` は変えない

- [ ] **Step 1: 失敗するテストを書く**（`DefeatViewTests`：`ScriptableObject.CreateInstance<SkillDefinition>()` に `SerializedObject` で `_id`／`_icon` を入れて）`FindSkillIcon` が「ある → その絵」「ない id → null」「絵が null → null」「配列が null → null」を返す
- [ ] **Step 2: テストが失敗するのを確かめる**（`tools/run-editmode-tests.sh`）
- [ ] **Step 3: View と2つのシーンを書く**。強化アイコンは `Show` のたびに前のを消してから `Instantiate(_skillIconPrefab, _skillIconRoot)` で `Skills` の順に作り、`FindSkillIcon` が null のものは飛ばす。武器の絵は `switch (result.Weapon)` で表から選ぶ。View は `Update` などを書かず、文字列は `DigitText` だけで作る。`Show` は画面を開いたときに1回だけ呼ぶ（毎フレーム文字列を作らない）
- [ ] **Step 4: `tools/run-editmode-tests.sh` で全部通るのを確かめる**（`ArchitectureGuardTests` の「Unity のライフサイクルは Main だけ」「sealed」「直下に置かない」を含む）
- [ ] **Step 5: コミット** `feat: home, clear and defeat views`

### Task 5: Home.unity と Result.unity に画面を置く

Unity のエディタで行う（Q6）。C さんだけが編集するシーン。

**Files:**
- Modify: `Assets/_Project/Scenes/Home.unity`、`Assets/_Project/Scenes/Result.unity`
- Create: `Assets/_Project/Prefabs/UI/SkillIcon.prefab`（`Image` だけ、120×120、Raycast Target オフ）
- Test: `Assets/_Project/Tests/EditMode/SceneSetupTests.cs` に足す

配置（Canvas の基準 1080×1920。位置は基準点からのずれ）：

| シーン | もの | 基準点 | 位置 | 大きさ |
|---|---|---|---|---|
| Home | ▶（今の StartButton、`_startIcon` は子の Image） | 中央 | (0, -200) | 360×360 |
| Home | 記録の段 `_recordRow`（`_bestWaveText`、`_bestTimeGroup`＝⏱ 96×96＋文字） | 上の中央 | (0, -260) | 幅 800・高さ 120、文字 96pt |
| Result | `ClearView`：🏆 / ⏱＋時間 / コイン＋数（黄） | 中央 | 🏆 (0, 400) 360×360、⏱ の段 (0, 80)、コインの段 (0, -80)。文字 96pt | — |
| Result | `DefeatView`：💀 / `23/50` / 武器 / 強化の並び | 中央 | 💀 (0, 420) 300×300、ウェーブ (0, 160) 120pt、武器 (0, -20) 160×160、強化 (0, -260) | 強化は GridLayoutGroup：セル 120×120、間 16、6列で折り返す |
| Result | ↻（今の RetryButton）と 🏠（今の HomeButton） | 下の中央 | (-220, 260) と (220, 260) | 280×280 |

- ボタンの `Image` の絵は空のままにして、絵は子の `Image`（`_startIcon`、`_retryIcon`、`_homeIcon`）に表から入れる。ボタンに文字の子を作らない。
- TMP の文字はすべて `Digits.asset`、白（コインだけ `#F5C542`）、初めの値は `0/50`、`0:00`、`0`。
- 💀 と 🏆 の色は白のまま（赤や黄にしない）。

- [ ] **Step 1: 失敗するテストを書く**（`SceneSetupTests`）

```csharp
[TestCase("Home", typeof(HomeScene), "_view")]
[TestCase("Home", typeof(HomeScene), "_icons")]
[TestCase("Result", typeof(ResultScene), "_clearView")]
[TestCase("Result", typeof(ResultScene), "_defeatView")]
[TestCase("Result", typeof(ResultScene), "_icons")]
public void SceneField_IsWired(string sceneName, Type sceneType, string field)
// → WithScene で SerializedObject(owner).FindProperty(field).objectReferenceValue が null でない

[TestCase("Home")] [TestCase("Result")]
public void SceneTexts_UseDigitFontOnly(string sceneName)
// → シーンの全 TMP_Text の font が Assets/_Project/Fonts/Digits.asset
```

今の `Button_IsWiredAndHasNoText` はそのまま通ること。

- [ ] **Step 2: テストが失敗するのを確かめる**（`tools/run-editmode-tests.sh`）
- [ ] **Step 3: エディタで上の表どおりに置き、インスペクターでつなぐ**（onClick には何も入れない）
- [ ] **Step 4: `tools/run-editmode-tests.sh` で全部（`NoTextGuardTests.SerializedTexts_ContainNoLetters` を含む）通るのを確かめる**
- [ ] **Step 5: 540×960 の Game ビューで Home と Result（Task 6 の PlayMode と同じ手順で両方の板）を見て、文字がない・端からはみ出さないことを確かめ、スクリーンショットを人に見せる**
- [ ] **Step 6: コミット** `feat: lay out home and result screens`

### Task 6: PlayMode で遷移と表示を確かめる

**Files:**
- Modify: `Assets/_Project/Tests/PlayMode/SceneFlowPlayModeTests.cs`

**Interfaces:**
- Consumes: `GameContext.FinishRun`、`RunResult`（タスク2）、`ClearView`／`DefeatView`／`HomeView`（タスク4。表示は `gameObject.activeSelf` と TMP の `text` で確かめる。フィールドは今の `Click<T>` と同じくリフレクションで取る）

- [ ] **Step 1: テストを書く**

```csharp
[UnityTest] public IEnumerator Clear_ShowsClearView_ThenHomeShowsBestTime()
// LoadRoot → Click<HomeScene>("_startButton") → Battle を待つ（▶ → Battle を確かめる）
// → OpenHostScene("HostClear") → host.Context.FinishRun(new RunResult(Cleared, 50, 1425.4f, 321, WeaponId.Gun, null)) が true
// → Result を待つ → ClearView が表示・DefeatView が非表示、_timeText.text == "23:45"、_coinText.text == "321"
// → Click<ResultScene>("_homeButton") → Home を待つ → _bestTimeGroup が表示・_bestWaveText が非表示、_bestTimeText.text == "23:45"

[UnityTest] public IEnumerator Died_ShowsDefeatView_ThenRetryGoesToBattle()
// LoadRoot → OpenHostScene("HostDied") → FinishRun(new RunResult(Died, 23, 0f, 5, WeaponId.Laser, new[] { SkillId.Fire }))
// → Result を待つ → DefeatView が表示・ClearView が非表示、_waveText.text == "23/50"、_weaponIcon.sprite が null でない
// → Click<ResultScene>("_retryButton") → Battle を待つ。そのあと Root と Battle の2つだけ

[UnityTest] public IEnumerator Home_FirstTime_HidesRecordRow()
// LoadRoot → HomeView の _recordRow が非表示
```

今の `HostRequestsResult_ThenHomeAndRetryButtonsWork` には「`LastRun` なしで開いた Result は ClearView も DefeatView も非表示」を足す（Review Focus 1）。

`Main` は毎回新しく作られ、`BestRecord` も新しくなるので、テストの順番に左右されない（`LoadRoot` のコメントのとおり）。

- [ ] **Step 2: `tools/run-playmode-tests.sh` で全部通るのを確かめる**（`Logs/playmode-results.xml`。長いときは `log-triage` に失敗だけを抜き出させる）
- [ ] **Step 3: `tools/run-editmode-tests.sh` も通るのを確かめる**（PR の前に全テスト）
- [ ] **Step 4: コミット** `test: play mode flow through clear and defeat screens`

### Task 7: 仕上げ

- [ ] **Step 1: Issue #15 の「触る予定のファイル」と終わりの条件を、Q1 の決定に合わせて直す案を人に見せる**（コメントや書き換えは人の OK のあと）
- [ ] **Step 2: `task-reviewer` に、このブランチの差分を仕様（この計画と設計書 §5・§6）に合っているか見てもらう**
- [ ] **Step 3: 人の OK をもらってから push し、`develop` 向けの下書きの PR を出す**（本文に `Closes #15`、触ったファイル、Q1〜Q6 の決定、「あとのタスクで行う統合」の表）

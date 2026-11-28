# C-12 障害物のパターン6つとウェーブごとの切り替え 実装計画

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 障害物の並びを6パターン（`ObstacleLayout`）で用意し、`WaveDefinition` から指定して、休みの間に「消える → 薄い影 約0.5秒 → 現れる（プレイヤーと重なる物だけ待つ）」で切り替える System を作る。

**Architecture:** 重なりの判定・影から出現までの時間の進み・袋小路の判定は Core（`Scripts/Core/Waves/`）の純粋な C#。Game 側は `ObstacleLayout`（並びのデータ）と `ObstacleSwitchSettings`（調整値）、プレハブに付く `ObstacleView`、それを動かす `ObstacleSystem`（`Scripts/Game/Field/`）。Battle への組み込み（`BattleScene.cs`・`Battle.unity`）は C-05 で行い、このタスクでは触らない（下の「人に確認すること」1）。

**Tech Stack:** Unity 6000.5.10f1、URP 2D、NUnit（EditMode／PlayMode）

**Spec:** `docs/superpowers/specs/2026-10-31-pictoarcher-design-v3.md` §6「1ウェーブの流れ」「障害物のパターン」、§12、§16（パターンの中身は作る人に任せる）。Issue #14。

## 人に確認すること（実装の前に答えをもらう）

計画はそれぞれ「おすすめ」で書いてある。違う答えなら、そのタスクを直してから始める。

1. **Battle への組み込みは C-05 に回す（おすすめ）。** `BattleScene.cs` と `Battle.unity` は今回触らない。まだプレイヤー（A-01）もウェーブの System（C-05）もないので、`ObstacleSystem` に渡すプレイヤーがいない。終わりの条件「ウェーブが変わると障害物の配置が変わる」は、Task 5 の PlayMode テスト `WaveCleared_SwitchesToNextWavesLayout`（本物の `WaveSequence`・`WaveDefinition`・`ObstacleSystem` をつなぐ）で確かめる。目で見る確認は C-05 で行う。C-05 で足す行は末尾の「C-05 への引き継ぎ」に書いた。→ これでよいか。
2. **`Field_Test.prefab` の固定の障害物4つを外し、パターン1（`Layout1_Corners`）に移す（おすすめ）。** 外さないと、Battle で固定の障害物と切り替わる障害物が二重になる。空の `Obstacles` の子は残して、切り替わる障害物の親にする。`FieldTestPrefabTests` の障害物のテストは、パターンのアセットのテスト（Task 4）に移す。いま `Field_Test` を参照するシーン・プレハブはない（GUID で確かめた）が、A さん・B さんが手元の試しで障害物つきの `Field_Test` を使っていないか、念のため聞きたい。
3. **Core のファイルは `Scripts/Core/Waves/` に置く（おすすめ）。** Core のフォルダの一覧（`Common` `Combat` `Movement` `Waves` `Skills`）に Field がないため、ウェーブごとの切り替えとして Waves に入れる。`Scripts/Core/Field/` を新しく作るほうがよければ、そうする（`CLAUDE.md` のフォルダ構成も直す）。
4. **「袋小路を作らない」の判定は「プレイヤーの体（1.5×1.5 ユニット）が通れる場所が、すべて1つにつながっている」にする（おすすめ）。** 閉じ込められる場所はなくなるが、U 字のくぼみ（入口が1つの行き止まり）は許す。くぼみも禁止するなら、パターンの作り方の決まり（「障害物どうし・壁との間は 2 ユニット以上あけるか、くっつける」など）を足す。
5. **プレイヤーと重なっているかの判定に使う体の半径は、`ObstacleSwitchSettings` の調整値 0.6 ユニットにする（おすすめ）。** プレイヤーの当たりの大きさは A-01 で決まる。決まったらこの値を合わせる（A-01 のプレハブから読む形にはしない。A さんのファイルに依存させないため）。
6. **`WaveDefinition` の障害物が空なら、今の並びのまま（エラーにしない）（おすすめ）。** C-05 のテスト用のウェーブで入れ忘れても動く。50ウェーブすべてに入っているかは C-11 のデータテストで確かめる（末尾の「C-11 への引き継ぎ」）。

決めたこと（§16 で作る人に任せられている範囲。違和感があれば言ってほしい）：

- 6パターンは「通常のウェーブ向け4つ（障害物4〜6個）＋ボスと中ボス向け2つ（障害物2個）」。どれがボス向けかは `ObstacleLayout` の `ForBossWaves` に書く。
- 薄い影は、同じ絵を黒の半透明（アルファ 0.35）で出す。影の間は当たり判定を切る。
- 今の障害物は休みに入った瞬間に消す。次のウェーブと同じパターンなら、消さずにそのまま残す（ちらつかせない）。
- プレイヤーと重なって待っている障害物は、影を出したままにする（どこに出るかが見える）。休みが終わって次のウェーブが始まっても、離れた時点で現れる。
- 戦いの始め（1つ目のウェーブ）は影を出さずにすぐ出す。重なる物だけは同じく待つ。
- 障害物の絵は `Data/Visuals/Obstacles/` の見た目の表（`SpriteVisual`、いまは `Art/Placeholder/Obstacles/` の仮の四角）を指す。コマ送りは使わず、1コマ目だけを出す。ダウンロードはしない。

## このタスクの範囲（C-05・C-11 と重ねない）

| 物 | C-12（このタスク） | C-05（#7） | C-11（#13） |
|---|---|---|---|
| `WaveDefinition.cs` | `_obstacleLayout` の欄とプロパティを足すだけ | — | 触らない |
| `Data/Waves/*.asset` | 作らない | テスト用のウェーブ2つ | 50ウェーブを作り、パターンを割り振る |
| `Data/Obstacles/` | 6パターンと設定のアセット | — | — |
| `ObstacleSystem` | 作る（Field） | `BattleScene` で作り、ウェーブの流れから呼ぶ | — |
| `BattleScene.cs`・`Battle.unity` | 触らない | 組み込む | — |
| ボス・中ボスのウェーブにボス向けのパターン | `ForBossWaves` の印を用意 | — | データテストで確かめる |

## Global Constraints

- Unity の標準ライフサイクル（`Awake`、`Start`、`Update`、`OnEnable`、`OnDestroy` など）は `Main` だけ。`ObstacleView` は `Init(...)` などのメソッドを持ち主（`ObstacleSystem`）が呼ぶ。
- 禁止：`Find` 系、`Camera.main`、コルーチン、`Invoke`、`async`、シングルトン、static の書き換えられる値、`Time.`（`deltaTime` は引数）。
- クラスは `sealed`。1ファイル1型、ファイル名＝型名。名前空間は `PictoArcher.Core`／`PictoArcher.Game` だけ。
- `Scripts/Core` は Unity の部品を使わない（`Vector2`、`Rect`、`Mathf` だけ）。Core はログを出さない。
- ScriptableObject は `[SerializeField] _camelCase` ＋ 読み取り専用のプロパティ。実行中に書き換えない。
- 障害物のレイヤーは `Wall`（9）。コードでは `Layers.Wall` を使い、数字を書かない。
- 数値（影の秒数、影の色、体の半径）は `ObstacleSwitchSettings` の調整値。パターンの位置は `ObstacleLayout` のアセット。コードに直接書かない。
- 画面に文字を出さない。影の色は黒（§5 の5色を使わない）。
- 触らない：`BattleScene.cs`、`Battle.unity`、ほかのシーン、A・B のコードとプレハブ（`Prefabs/Enemies/` など）、Home／Result のファイル（C-07 が別に進めている）。
- コミットは触ったファイルだけをパスで指定する（`.meta` も一緒に）。

## Review Focus

1. **影が終わる瞬間にプレイヤーが障害物の位置にいる**：その障害物だけ影のまま、ほかは現れる。離れた次の Tick で現れる。→ Task 1 `SlotUnderPlayer_IsBlockedUntilPlayerLeaves`、Task 5 `SwitchTo_ObstacleUnderPlayer_StaysShadowUntilPlayerLeaves`。
2. **次のウェーブも同じパターン／パターンが空**：消えたり影が出たりしない。→ Task 5 `SwitchTo_SameLayout_KeepsObstaclesSolid`、`SwitchTo_Null_KeepsCurrentLayout`。
3. **切り替えの途中でもう一度切り替える**（休みの間に次のウェーブがすぐ終わる、デバッグで飛ばすなど）：古い影は残らず、新しい並びだけになる。→ Task 5 `SwitchTo_DuringShadow_ReplacesPendingObstacles`。
4. **パターンを手で直したときに、壁の外・ほかの障害物と重なる・閉じ込める並び・ボス向けなのに多い**：データテストで落ちる。→ Task 4 `ObstacleLayoutAssetTests`。
5. **Tick と物理の間にプレイヤーが動いて、現れた障害物とわずかに重なる**：判定は Tick の時点の位置なので、物理の押し出しで外へ出る（閉じ込めはしない）。テストでは作れないので、C-05 の手動確認で見る（下の「C-05 への引き継ぎ」）。

---

### Task 1: Core の重なりの判定と、影から出現までの進み

**Files:**
- Create: `Assets/_Project/Scripts/Core/Waves/ObstacleOverlap.cs`、`ObstacleSlotState.cs`、`ObstacleSwap.cs`
- Test: `Assets/_Project/Tests/EditMode/ObstacleOverlapTests.cs`、`ObstacleSwapTests.cs`

**Interfaces:**
- Produces:
  - `public static class ObstacleOverlap { public static bool Overlaps(Rect obstacle, Vector2 center, float radius); }` — 円と長方形。長方形の中で円の中心に一番近い点までの距離が `radius` より**小さい**とき true（ちょうど `radius` は重ならない）。`radius < 0` は `ArgumentOutOfRangeException`。
  - `public enum ObstacleSlotState { Shadow, Blocked, Shown }` — 影の時間中／影の時間は終わったがプレイヤーと重なって待っている／現れた。
  - `public sealed class ObstacleSwap`
    - `ObstacleSwap(IReadOnlyList<Rect> obstacles, float shadowSeconds)` — 一覧はコピーして持つ。`shadowSeconds < 0` は `ArgumentOutOfRangeException`。最初はすべて `Shadow`。
    - `int Count`、`ObstacleSlotState StateAt(int index)`、`bool IsFinished`（すべて `Shown`。0個なら最初から true）、`float ShadowProgress`（0〜1。`shadowSeconds == 0` なら 1）
    - `void Tick(float deltaTime, Vector2 playerPosition, float playerRadius)` — 経過を足し、`shadowSeconds` に達していれば、まだ `Shown` でない枠をそれぞれ「プレイヤーと重なる → `Blocked`、重ならない → `Shown`」にする。`Shown` は戻らない。`deltaTime < 0` は `ArgumentOutOfRangeException`。

- [ ] **Step 1: 失敗するテストを書く**

`ObstacleOverlapTests`（`Rect(0, 0, 1, 1)` に対して）：
```csharp
[Test] public void CenterInside_Overlaps() => Assert.IsTrue(ObstacleOverlap.Overlaps(Box, new Vector2(0.5f, 0.5f), 0.1f));
[Test] public void FarAway_DoesNotOverlap() => Assert.IsFalse(ObstacleOverlap.Overlaps(Box, new Vector2(5f, 5f), 0.6f));
[Test] public void NearCornerWithinRadius_Overlaps() => Assert.IsTrue(ObstacleOverlap.Overlaps(Box, new Vector2(1.3f, 1.3f), 0.5f));   // 約 0.42
[Test] public void DiagonalOutsideRadius_DoesNotOverlap() => Assert.IsFalse(ObstacleOverlap.Overlaps(Box, new Vector2(1.4f, 1.4f), 0.5f)); // 約 0.57
[Test] public void ExactlyRadiusFromEdge_DoesNotOverlap() => Assert.IsFalse(ObstacleOverlap.Overlaps(Box, new Vector2(1.5f, 0.5f), 0.5f));
[Test] public void NegativeRadius_Throws() => Assert.Throws<ArgumentOutOfRangeException>(() => ObstacleOverlap.Overlaps(Box, Vector2.zero, -0.1f));
```

`ObstacleSwapTests`（障害物 A=`Rect(0,0,2,2)`、B=`Rect(5,0,2,2)`、影 0.5 秒、半径 0.6、プレイヤーが遠いときは `(20, 20)`）：
```csharp
[Test] public void NewSwap_AllSlotsAreShadow()                    // StateAt(0..1) == Shadow、IsFinished false
[Test] public void BeforeShadowSeconds_StaysShadow()              // Tick(0.49f, far) → どちらも Shadow、ShadowProgress == 0.98f（誤差 1e-4）
[Test] public void AtShadowSeconds_ShowsSlotsAwayFromPlayer()     // Tick(0.25f)×2 → どちらも Shown、IsFinished true
[Test] public void SlotUnderPlayer_IsBlockedUntilPlayerLeaves()   // Tick(0.5f, (1,1)) → A Blocked・B Shown・IsFinished false
                                                                   // Tick(0f, (1,1)) → A Blocked のまま／Tick(0f, far) → A Shown、IsFinished true
[Test] public void ShownSlot_StaysShownWhenPlayerComesBack()      // Tick(0.5f, far) → Tick(0f, (1,1)) でも A Shown
[Test] public void ZeroShadowSeconds_ShowsOnFirstTick()           // new ObstacleSwap(list, 0f)：ShadowProgress == 1、Tick(0f, far) → Shown
[Test] public void EmptyList_IsFinishedAtOnce()
[Test] public void NegativeShadowSeconds_Throws()
[Test] public void NegativeDeltaTime_Throws()
```

- [ ] **Step 2: 失敗を確かめる** — Unity のエディタを閉じて `tools/run-editmode-tests.sh`。期待：コンパイルエラー（`ObstacleOverlap` などがない）。
- [ ] **Step 3: 上の Interfaces どおりに3つのファイルを書く**（`ObstacleSwap` は状態を `ObstacleSlotState[]` で持つ。`<summary>` は日本語で、§6 の「閉じ込めない」を書く）
- [ ] **Step 4: 通ることを確かめる** — `tools/run-editmode-tests.sh`。期待：失敗 0（`ArchitectureGuardTests` を含む）。
- [ ] **Step 5: コミット**

```bash
git add Assets/_Project/Scripts/Core/Waves/ObstacleOverlap.cs* Assets/_Project/Scripts/Core/Waves/ObstacleSlotState.cs* Assets/_Project/Scripts/Core/Waves/ObstacleSwap.cs* Assets/_Project/Tests/EditMode/ObstacleOverlapTests.cs* Assets/_Project/Tests/EditMode/ObstacleSwapTests.cs*
git commit -m "feat: add obstacle overlap check and shadow-to-solid swap timing"
```

### Task 2: Core の袋小路の判定

**Files:**
- Create: `Assets/_Project/Scripts/Core/Waves/ObstacleReachability.cs`
- Test: `Assets/_Project/Tests/EditMode/ObstacleReachabilityTests.cs`

**Interfaces:**
- Produces: `public static class ObstacleReachability { public static bool IsAllReachable(Rect area, IReadOnlyList<Rect> obstacles, float bodySize, float cellSize); }`
  - `area` を `cellSize` のマスに分け、マスの中心に `bodySize`×`bodySize` の正方形を置く。正方形が `area` の中に収まり（辺が重なるのはよい）、どの障害物とも `Rect.Overlaps` しないマスを「通れる」とする。
  - 通れるマスが1つ以上あり、上下左右のつながりで全部1つにつながっていれば true。
  - `bodySize <= 0` か `cellSize <= 0` は `ArgumentOutOfRangeException`。
  - データテストだけで使う（実行中には呼ばない）ので、配列を毎回作ってよい。

- [ ] **Step 1: 失敗するテストを書く**（`area = Rect(0, 0, 10, 10)`、`bodySize = 1f`、`cellSize = 0.25f`）

```csharp
[Test] public void EmptyArea_IsReachable()                // obstacles なし → true
[Test] public void WallAcrossArea_IsNotReachable()        // Rect(0, 4, 10, 2) → false（上と下に分かれる）
[Test] public void ClosedRoomWithNarrowDoor_IsNotReachable()
    // 部屋 (6〜10, 6〜10) の壁：Rect(6,6,4,0.5) の下壁を Rect(6,6,1.5,0.5) と Rect(8,6,2,0.5) に分け、すき間 0.5。左壁 Rect(6,6,0.5,4) → false
[Test] public void RoomWithWideDoor_IsReachable()         // 同じ部屋で下壁のすき間を 2（Rect(6,6,0.5,0.5) と Rect(8.5,6,1.5,0.5)）→ true
[Test] public void ScatteredBoxes_AreReachable()          // Rect(2,2,2,2)、Rect(6,2,2,2)、Rect(2,6,2,2) → true
[Test] public void AreaFullyBlocked_IsNotReachable()      // Rect(0,0,10,10) → false
[Test] public void NonPositiveSizes_Throw()               // bodySize 0、cellSize 0 のそれぞれで Throws
```

- [ ] **Step 2: 失敗を確かめる** — `tools/run-editmode-tests.sh`。期待：コンパイルエラー。
- [ ] **Step 3: `IsAllReachable` を書く**（`bool[,]` の通れるマスを作り、最初に見つけた通れるマスから `Queue<int>` で塗りつぶし、塗った数と通れる数を比べる）
- [ ] **Step 4: 通ることを確かめる** — `tools/run-editmode-tests.sh`。期待：失敗 0。
- [ ] **Step 5: コミット**

```bash
git add Assets/_Project/Scripts/Core/Waves/ObstacleReachability.cs* Assets/_Project/Tests/EditMode/ObstacleReachabilityTests.cs*
git commit -m "feat: add reachability check so obstacle layouts never trap the player"
```

### Task 3: Game のデータ（ObstacleLayout、調整値、WaveDefinition の欄）

**Files:**
- Create: `Assets/_Project/Scripts/Game/Data/ObstaclePlacement.cs`、`ObstacleLayout.cs`、`ObstacleSwitchSettings.cs`
- Modify: `Assets/_Project/Scripts/Game/Data/WaveDefinition.cs`（欄を1つとプロパティを1つ足すだけ）
- Test: `Assets/_Project/Tests/EditMode/DataFormatTests.cs`（テストを足す）

**Interfaces:**
- Consumes: `SpriteVisual`（`Clip.Frames`、`Scale`）
- Produces:
  - `[Serializable] public struct ObstaclePlacement`：`[SerializeField] SpriteVisual _visual`、`[SerializeField] Vector2 _position`、コンストラクター `ObstaclePlacement(SpriteVisual visual, Vector2 position)`、`SpriteVisual Visual`、`Vector2 Position`、`Sprite Sprite`（`Visual.Clip.Frames[0]`。表かコマが空なら null）、`Rect Bounds`（フィールド上の範囲：`Sprite.bounds` を `Visual.Scale` 倍して `Position` に置く。中心以外の pivot でも合うよう `bounds.min`・`bounds.size` を使う。`Sprite` が null なら `Rect(Position, Vector2.zero)`）
  - `[CreateAssetMenu(menuName = "PictoArcher/ObstacleLayout")] public sealed class ObstacleLayout : ScriptableObject`：`[SerializeField] List<ObstaclePlacement> _placements`、`[SerializeField, Tooltip("ボスと中ボスのウェーブ向け（障害物が少ない）")] bool _forBossWaves`、`IReadOnlyList<ObstaclePlacement> Placements`、`bool ForBossWaves`
  - `[CreateAssetMenu(menuName = "PictoArcher/ObstacleSwitchSettings")] public sealed class ObstacleSwitchSettings : ScriptableObject`：`_shadowSeconds = 0.5f`（影を出す秒数）、`_shadowColor = new Color(0f, 0f, 0f, 0.35f)`（影の色。黒の半透明）、`_playerRadius = 0.6f`（プレイヤーと重なるかの判定に使う体の半径）。プロパティ `ShadowSeconds`、`ShadowColor`、`PlayerRadius`。
  - `WaveDefinition`：`[SerializeField, Tooltip("このウェーブの障害物の並び。空なら前のウェーブのまま")] ObstacleLayout _obstacleLayout;` と `public ObstacleLayout ObstacleLayout => _obstacleLayout;`

- [ ] **Step 1: 失敗するテストを `DataFormatTests` に足す**

```csharp
[Test] public void WaveDefinition_HasNoObstacleLayoutByDefault()        // CreateInstance<WaveDefinition>().ObstacleLayout が null
[Test] public void ObstacleSwitchSettings_DefaultsMatchSpec()          // ShadowSeconds 0.5f、ShadowColor == new Color(0,0,0,0.35f)、PlayerRadius 0.6f
[Test] public void ObstaclePlacement_BoundsFollowSpriteAndScale()
    // Sprite.Create(new Texture2D(32, 16), new Rect(0,0,32,16), new Vector2(0.5f,0.5f), 16f) を1コマの SpriteVisual に入れ（SerializedObject で _clip._frames と _scale = 2 を書く）、
    // new ObstaclePlacement(visual, new Vector2(3, 4)).Bounds == Rect(1, 3, 4, 2)
[Test] public void ObstaclePlacement_WithoutSprite_HasEmptyBounds()    // Visual が null → Bounds.size == Vector2.zero
```

- [ ] **Step 2: 失敗を確かめる** — `tools/run-editmode-tests.sh`。期待：コンパイルエラー。
- [ ] **Step 3: 上の Interfaces どおりに3つのファイルを書き、`WaveDefinition` に欄を足す**（`WaveDefinition` の既存の欄・`FormerlySerializedAs` は変えない）
- [ ] **Step 4: 通ることを確かめる** — `tools/run-editmode-tests.sh`。期待：失敗 0。
- [ ] **Step 5: コミット**

```bash
git add Assets/_Project/Scripts/Game/Data/ObstaclePlacement.cs* Assets/_Project/Scripts/Game/Data/ObstacleLayout.cs* Assets/_Project/Scripts/Game/Data/ObstacleSwitchSettings.cs* Assets/_Project/Scripts/Game/Data/WaveDefinition.cs Assets/_Project/Tests/EditMode/DataFormatTests.cs
git commit -m "feat: add ObstacleLayout data and obstacle field on WaveDefinition"
```

### Task 4: 6パターンのアセットと、Field_Test の固定の障害物を外す

**Files:**
- Create: `Assets/_Project/Data/Obstacles/Layout1_Corners.asset` 〜 `Layout6_BossBoxes.asset`、`ObstacleSwitchSettings.asset`（Unity の Create メニュー `PictoArcher/…` で作り、インスペクターで下の表の値を入れる）
- Modify: `Assets/_Project/Prefabs/Field/Field_Test.prefab`（`Obstacles` の子の Container・Pillar×2・MachineBox を消す。空の `Obstacles` は残す）
- Modify: `Assets/_Project/Tests/EditMode/FieldTestPrefabTests.cs`（`Obstacles_AreInsideTheInnerAreaAndAwayFromTheCenter` を消し、`Obstacles_RootIsEmpty`（`Obstacles` という子があり、子が0個）に替える。`InnerArea` を `internal static readonly` にして次のテストから使う）
- Delete: `Assets/_Project/Data/Obstacles/.gitkeep`（あれば）
- Test: `Assets/_Project/Tests/EditMode/ObstacleLayoutAssetTests.cs`

**Interfaces:**
- Consumes: Task 2 `ObstacleReachability.IsAllReachable`、Task 3 `ObstacleLayout`・`ObstaclePlacement.Bounds`、`FieldTestPrefabTests.InnerArea`
- Produces: `Data/Obstacles/` の6パターン（C-11 が `WaveDefinition` に割り振る）と `ObstacleSwitchSettings.asset`（C-05 が `BattleScene` のインスペクターでつなぐ）

6パターン（位置は障害物の中心、ユニット。戦える広さは x −7.5〜7.5、y −14〜11。C＝`Data/Visuals/Obstacles/Container`（2×2）、M＝`MachineBox`（2×2）、P＝`Pillar`（1×2））：

| アセット | ForBossWaves | 並び |
|---|---|---|
| `Layout1_Corners` | false | C(−4.5, −8)、M(4.5, −8)、P(−4, 5)、P(4, 5)（今の `Field_Test` と同じ） |
| `Layout2_Columns` | false | P(−4.5, −8)、P(4.5, −8)、P(−4.5, −1.5)、P(4.5, −1.5)、P(−4.5, 5)、P(4.5, 5) |
| `Layout3_Staggered` | false | C(−4.5, −10)、M(3.5, −6)、C(−3.5, 1)、M(4.5, 6) |
| `Layout4_Row` | false | C(−4.5, 2)、M(0, 2)、C(4.5, 2)、P(−2.5, −9)、P(2.5, −9) |
| `Layout5_BossPillars` | true | P(−5, 6)、P(5, 6) |
| `Layout6_BossBoxes` | true | C(−4.5, −10)、M(4.5, −10) |

- [ ] **Step 1: 失敗するテスト `ObstacleLayoutAssetTests` を書く**（`AssetDatabase.FindAssets("t:ObstacleLayout", new[] { "Assets/_Project/Data/Obstacles" })` で読む。違反はまとめて1つのメッセージで出す）

```csharp
const float BodySize = 1.5f;      // プレイヤーの体より少し大きめ（人に確認すること 4）
const float CellSize = 0.25f;
const int MaxBossObstacles = 2;

[Test] public void SixLayoutsExist()                         // 6個
[Test] public void BothBossAndNormalLayoutsExist()           // ForBossWaves が true のもの・false のものが1つ以上ずつ
[Test] public void BossLayouts_HaveFewObstacles()            // ForBossWaves → Placements.Count <= MaxBossObstacles、かつ通常のどれよりも少ない
[Test] public void Placements_HaveSprites()                  // すべての Placement で Sprite != null
[Test] public void Placements_AreInsideTheInnerArea()        // Bounds の min と max が FieldTestPrefabTests.InnerArea の中
[Test] public void Placements_DoNotOverlapEachOther()        // 同じパターンの中で Bounds.Overlaps しない
[Test] public void Layouts_NeverTrapThePlayer()              // ObstacleReachability.IsAllReachable(InnerArea, 各 Bounds, BodySize, CellSize)
[Test] public void SwitchSettings_ShadowFitsInTheRest()      // Data/Obstacles/ObstacleSwitchSettings.asset があり、0 < ShadowSeconds < WaveSequence.DefaultRestSeconds
```

- [ ] **Step 2: 失敗を確かめる** — `tools/run-editmode-tests.sh`。期待：`SixLayoutsExist` などが失敗。
- [ ] **Step 3: 表どおりに6つのアセットと設定のアセットを作り、`Field_Test` の固定の障害物を外し、`FieldTestPrefabTests` を直す**（`Field_Test` を開くのは Unity のプレハブ編集。消すのは4つの子の GameObject だけで、床・壁の Tilemap は触らない）
- [ ] **Step 4: 通ることを確かめる** — `tools/run-editmode-tests.sh`。期待：失敗 0（`VisualTableTests`、`NoTextGuardTests` を含む）。
- [ ] **Step 5: コミット**

```bash
git add Assets/_Project/Data/Obstacles Assets/_Project/Prefabs/Field/Field_Test.prefab Assets/_Project/Tests/EditMode/FieldTestPrefabTests.cs Assets/_Project/Tests/EditMode/ObstacleLayoutAssetTests.cs*
git commit -m "feat: add six obstacle layouts and move Field_Test obstacles into layout 1"
```

### Task 5: ObstacleView・プレハブ・ObstacleSystem と、ウェーブで切り替わる PlayMode テスト

**Files:**
- Create: `Assets/_Project/Scripts/Game/Field/ObstacleView.cs`、`ObstacleSystem.cs`
- Create: `Assets/_Project/Prefabs/Field/Obstacle.prefab`（レイヤー `Wall`。`SpriteRenderer`（マテリアル・ソート順は `Field_Test` にあった障害物と同じ）、`BoxCollider2D`、`ObstacleView`（`_sprite`・`_collider` をつなぐ））
- Test: `Assets/_Project/Tests/PlayMode/ObstacleSystemPlayModeTests.cs`

**Interfaces:**
- Consumes: Task 1 `ObstacleSwap`・`ObstacleSlotState`、Task 3 `ObstacleLayout`・`ObstaclePlacement`・`ObstacleSwitchSettings`・`WaveDefinition.ObstacleLayout`、既存の `ITarget`、`ITickable`、`WaveSequence`
- Produces（C-05 が使う）:
  - `public sealed class ObstacleView : MonoBehaviour`：`[SerializeField] SpriteRenderer _sprite`、`[SerializeField] BoxCollider2D _collider`、`void Init(ObstaclePlacement placement)`（位置、`Sprite`、`Visual.Scale` の大きさ、当たりの大きさと offset を `Sprite.bounds` から合わせる）、`void ShowShadow(Color color)`（当たりを切り、色を `color` に）、`void ShowSolid()`（当たりを入れ、色を白に）
  - `public sealed class ObstacleSystem : ITickable, IDisposable`
    - `ObstacleSystem(ObstacleView prefab, Transform parent, ObstacleSwitchSettings settings, ITarget player)`
    - `ObstacleLayout Current { get; }`、`bool IsSwitching { get; }`（現れていない障害物がある間 true）
    - `void ShowImmediately(ObstacleLayout layout)` — 今の障害物を消し、影なしで出す（`new ObstacleSwap(rects, 0f)` を作ってすぐ `Tick(0f, …)`）。重なる物は影のまま待つ。`null` なら何もしない。
    - `void SwitchTo(ObstacleLayout layout)` — `null` か `Current` と同じなら何もしない。違えば今の障害物をすぐ消し（`Object.Destroy`）、新しい並びを影で出し、`settings.ShadowSeconds` の `ObstacleSwap` を始める。切り替えの途中でも同じ（古い影も消す）。
    - `void Tick(float deltaTime)` — `ObstacleSwap.Tick(deltaTime, player.Position, settings.PlayerRadius)`。状態が変わった枠だけ `ShowShadow`／`ShowSolid` を呼ぶ（前の状態を配列で覚える。毎フレーム new しない）。
    - `void Dispose()` — 作った障害物をすべて消す。

- [ ] **Step 1: 失敗する PlayMode テストを書く**

準備：`FakePlayer : ITarget`（`Position` を書き換えられる。最初は `(100, 100)`）、親の空の GameObject、`Prefabs/Field/Obstacle.prefab` の `ObstacleView`、`Data/Obstacles/ObstacleSwitchSettings.asset`、`Layout1_Corners` と `Layout2_Columns`（`AssetDatabase` で読む）。`TearDown` で `Dispose` と親の破棄。数える・調べるのは `parent.GetComponentsInChildren<ObstacleView>()` とその `BoxCollider2D`・`SpriteRenderer`。`Destroy` は次のフレームで消えるので、消えたかは `yield return null` のあとに見る。

```csharp
[UnityTest] ShowImmediately_PlacesSolidObstaclesOnWallLayer
    // Layout1：4個、すべて gameObject.layer == Layers.Wall、collider.enabled、collider.bounds.center が各 Placement.Bounds.center（誤差 1e-3）、IsSwitching false
[UnityTest] ShowImmediately_ObstacleUnderPlayer_WaitsUntilPlayerLeaves
    // player を Layout1 の1つ目の中心に置く → その1個だけ collider off、色 == ShadowColor。player を遠くへ → Tick(0f) で on
[UnityTest] SwitchTo_RemovesOldAndShowsShadowsWithoutColliders
    // Layout1 を出す → SwitchTo(Layout2) → Tick(0.1f) → yield null：6個（Layout2 の数）だけ、すべて collider off、色 == ShadowColor、IsSwitching true
[UnityTest] SwitchTo_AfterShadowSeconds_TurnsSolid
    // SwitchTo(Layout2) → Tick(0.3f)×2：すべて collider on、色 == white、IsSwitching false、Current == Layout2
[UnityTest] SwitchTo_ObstacleUnderPlayer_StaysShadowUntilPlayerLeaves
    // player を Layout2 の1つ目の中心に置く → SwitchTo(Layout2) → Tick(0.6f)：その1個だけ off、ほかは on、IsSwitching true
    // player を遠くへ → Tick(0.016f)：全部 on、IsSwitching false
[UnityTest] SwitchTo_SameLayout_KeepsObstaclesSolid           // ShowImmediately(Layout1) → SwitchTo(Layout1) → Tick(0.1f)：4個とも on、同じインスタンス
[UnityTest] SwitchTo_Null_KeepsCurrentLayout                   // ShowImmediately(Layout1) → SwitchTo(null)：Current == Layout1、4個とも on
[UnityTest] SwitchTo_DuringShadow_ReplacesPendingObstacles     // SwitchTo(Layout2) → Tick(0.1f) → SwitchTo(Layout1) → yield null：4個だけ、すべて影
[UnityTest] WaveCleared_SwitchesToNextWavesLayout              // 終わりの条件
    // ScriptableObject.CreateInstance<WaveDefinition>() を2つ作り、SerializedObject で _obstacleLayout に Layout1・Layout2 を入れる（ウェーブ1・2）
    // var waves = new WaveSequence(1, WaveSequence.DefaultRestSeconds); waves.WaveStarted += n => { if (n == 1) system.ShowImmediately(defs[0].ObstacleLayout); };
    // waves.Begin() → Layout1 の位置。waves.NotifyCleared(); if (waves.Phase == WavePhase.Resting) system.SwitchTo(defs[waves.CurrentWave - 1].ObstacleLayout);
    // 1.5 秒ぶん waves.Tick と system.Tick を 0.1 ずつ進める → waves.CurrentWave == 2、Phase == Fighting、障害物の位置が Layout2 の位置で、すべて on
[UnityTest] Dispose_DestroysAllObstacles                       // Dispose → yield null → 子が0個
```

- [ ] **Step 2: 失敗を確かめる** — `tools/run-playmode-tests.sh`。期待：コンパイルエラー（`ObstacleSystem` がない）。
- [ ] **Step 3: `ObstacleView`・`ObstacleSystem` を書き、`Obstacle.prefab` を作る**（`ObstacleSystem` は `List<ObstacleView>`、`List<Rect>`、`ObstacleSlotState[]` を持ち回して使う。`Instantiate(prefab, parent)` のあと `Init` → `ShowShadow` か `ShowSolid`）
- [ ] **Step 4: 通ることを確かめる** — `tools/run-editmode-tests.sh` と `tools/run-playmode-tests.sh`。期待：どちらも失敗 0。長い結果は `log-triage` に読ませる。
- [ ] **Step 5: コミット**

```bash
git add Assets/_Project/Scripts/Game/Field/ObstacleView.cs* Assets/_Project/Scripts/Game/Field/ObstacleSystem.cs* Assets/_Project/Prefabs/Field/Obstacle.prefab* Assets/_Project/Tests/PlayMode/ObstacleSystemPlayModeTests.cs*
git commit -m "feat: add ObstacleSystem that swaps layouts with a shadow between waves"
```

### Task 6: 仕上げ

- [ ] **Step 1: 全テスト** — `tools/run-editmode-tests.sh` と `tools/run-playmode-tests.sh`。期待：どちらも失敗 0（`ArchitectureGuardTests`、`NoTextGuardTests` を含む）。
- [ ] **Step 2: `task-reviewer` にブランチ全体を見てもらう**（仕様 §6 の6項目と、この計画の「決めたこと」に合っているか）
- [ ] **Step 3: 人に見せる** — push と下書きの PR（`develop` 向け、本文に `Closes #14` と触ったファイル、`.github/pull_request_template.md` の項目）は人の OK をもらってから。マージ後に `gh issue close 14`。

---

## C-05 への引き継ぎ（BattleScene に足す行）

C-05 で `BattleScene` に足す（このタスクでは書かない）：

- インスペクター：`[SerializeField] ObstacleView _obstaclePrefab;`、`[SerializeField] ObstacleSwitchSettings _obstacleSettings;`、`[SerializeField] Transform _obstacleRoot;`（フィールドの `Obstacles` の子）
- `Init`：プレイヤーの System のあとで `_obstacles = new ObstacleSystem(_obstaclePrefab, _obstacleRoot, _obstacleSettings, player);`。最初のウェーブの開始で `_obstacles.ShowImmediately(firstWave.ObstacleLayout);`
- 全滅で `_waves.NotifyCleared();` を呼んだすぐあと：`if (_waves.Phase == WavePhase.Resting) _obstacles.SwitchTo(waveDefinitions[_waves.CurrentWave].ObstacleLayout);`（添え字は C-05 のウェーブの並べ方に合わせる）
- `Tick`：プレイヤーの移動のあとに `_obstacles.Tick(deltaTime);`
- `Dispose`：逆順で `_obstacles.Dispose();`
- 手動確認：休みに入ると障害物が消え、薄い影が約0.5秒出てから現れる。影の上に立っていると、その障害物だけ出ず、離れると出る。現れた瞬間にプレイヤーが押し出されて引っかからない（Review Focus 5）。

## C-11 への引き継ぎ

- 50ウェーブの `WaveDefinition` の `_obstacleLayout` に `Data/Obstacles/` のパターンを割り振る。中ボス（5・15・25・35・45）・ボス（10・20・30・40・50）のウェーブは `ForBossWaves` が true のパターン（`Layout5_BossPillars`、`Layout6_BossBoxes`）にする。
- データテストを足す：50ウェーブすべてに `ObstacleLayout` が入っている／`WaveLayout.GetKind` が `MiniBoss`・`Boss`・`FinalBoss` のウェーブは `ForBossWaves` が true／`SpawnEntry.Position` がそのウェーブの障害物の `Bounds` の中にない。

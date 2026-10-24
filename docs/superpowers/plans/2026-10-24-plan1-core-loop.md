# 計画1 コアループ 実装計画

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 1部屋を最初から最後まで遊べるようにする（入室 → 赤い `!` 円 → スライム出現 → 移動しながら自動射撃 → 全滅 → 扉の ➜ → 次の部屋）。

**Architecture:** ゲームのルール（移動ベクトルの計算、照準、連射、HP、部屋とランの進行）は MonoBehaviour を持たない純粋な C# クラスとして `PictoArcher.Core` に置き、EditMode テストで固める。Unity 上の部品（プレハブ、物理、表示）は `PictoArcher.Game` に置き、Core のクラスを呼ぶだけにする。数値はすべて ScriptableObject に置く。

**Tech Stack:** Unity 6000.5.10f1、URP 2D、Input System 1.20、TextMeshPro（`Unity.TextMeshPro`）、Unity Test Framework 1.7（NUnit）

**Spec:** `docs/superpowers/specs/2026-10-24-pictoarcher-design.md`（全体の分け方は `docs/superpowers/plans/2026-10-24-roadmap.md`）

## Global Constraints

- Unity は全員 6000.5.10f1 に固定する（設計書 §3）。
- 画面に文字を出さない。アルファベット・ひらがな・カタカナ・漢字、`LV` `HP` `BOSS` `x2` `s` などの略語や単位もすべて禁止（§5）。
- 使ってよい表記：レベル `★5`／倍率 `×2`／時間 `⏱ 30`／割合 `+20%`／進行 `7/10`（§5）。
- 色の意味：赤＝危険・攻撃／緑＝回復／黄＝コイン・経験値／青＝防御・氷／紫＝レア（§5）。
- 絵文字をフォントで出さない。ピクトグラムはすべてスプライト画像で用意する（§5）。
- TextMeshPro のフォントアセットには数字と記号だけを登録する（§5）。
- 開発ビルドやデバッグ表示でも画面に文字を出さない（ログはコンソールのみ）（§10）。
- 画面は 16:9 横長、1920×1080 基準、見下ろし視点、1部屋＝1画面でスクロールしない（§3, §10）。
- 入力は Input System。キーボードとマウスのドラッグを同じ「移動ベクトル」に変換する（§12）。
- 射撃は常に自動で、移動中も撃てる。一番近い敵を狙う。敵がいないときは撃たない（§4）。
- 移動中の連射速度は調整値で、初期値は止まっているときと同じ 100%（§4）。
- 被弾したら短い無敵時間（点滅）がある（§4）。
- 数値（HP、攻撃力、速度、出現数）はすべて ScriptableObject の調整値にする（§8）。
- シーンは同時に編集しない。担当ごとにプレハブを分け、シーンを編集するのは1人に決める（§12）。
- 純粋なロジックには EditMode テストを書く（§12）。
- 名前空間：ルールは `PictoArcher.Core`、Unity 上の部品は `PictoArcher.Game`。【計画での判断】
- 1タスク＝1ブランチ（`feature/t<番号>-<短い名前>`）。main へのマージ前に、担当以外の1人がプルリクエストを見る。【計画での判断】

## Review Focus

1. **TextMeshPro を置いたときの既定文字「New Text」がシーンやプレハブに残る** → 画面に英字が出る。保存されたテキストに文字が1つでも入っていたらテストで落とす（タスク3）。
2. **キーボードとマウスのドラッグを同時に使う** → 速さが最大を超えない。キーボードを優先する（タスク4）。
3. **ウィンドウを掴んでいる間などにフレームが大きく止まる** → 再開したときに矢がまとめて何本も出ない（タスク5）。
4. **同じフレームで2本の矢が同じ敵を倒す** → 倒した数は1回だけ数え、死亡の通知も1回だけにする（タスク6、タスク7）。
5. **扉に2回触れる、または開く前に触れる** → 次の部屋へは1回だけ進む。開く前は進まない（タスク7）。

16:9 以外のウィンドウ（4:3、21:9）でも部屋全体が見えることは、タスク10の手動確認で見る。

## ファイル構成

```
Assets/_Project/
  Scripts/Core/            PictoArcher.Core（純粋な C#、テスト対象）
    PictoArcher.Core.asmdef
    MoveVectorResolver.cs  移動ベクトルの計算
    TargetSelector.cs      一番近い敵を選ぶ
    FireTimer.cs           連射の間隔
    Health.cs              HP
    HitInvulnerability.cs  無敵時間
    RoomProgress.cs        部屋の敵の数と扉
    RunLayout.cs           1章10部屋の並び（RoomType を含む）
    RunProgress.cs         今どの部屋か、進行ドット
  Scripts/Game/            PictoArcher.Game（MonoBehaviour と ScriptableObject）
    PictoArcher.Game.asmdef
    Data/                  PlayerStats, EnemyDefinition, RoomDefinition, SkillId, SkillDefinition
    IHittable.cs           矢や体当たりが当たる相手
    EnemyRegistry.cs       今いる敵の一覧
    Player/                PlayerController, PlayerShooter, PlayerHealth, Arrow
    Enemies/               EnemyActor, ChaseMovement, SpawnTelegraph
    Run/                   RoomController, Door, RunController
    UI/                    HudView
  Tests/EditMode/          PictoArcher.Tests.EditMode
  Data/                    ScriptableObject のアセット
  Prefabs/                 Player, Arrow, Slime, Door, Room_Test
  Scenes/Run.unity         Cさんだけが編集する
  Scenes/Sandbox/          Sandbox_A.unity, Sandbox_B.unity（各自の試し用、他の人は触らない）
  Fonts/                   数字と記号だけのフォント
  Art/                     フリー素材（CC0）
tools/run-editmode-tests.sh
docs/assets-list.md
```

## 担当と順番

| タスク | 担当 | 週 | 先に終わっている必要があるもの |
|---|---|---|---|
| 1 土台 | C | 1（初日） | なし |
| 2 データ形式と共通部品 | A（形式は全員で合意） | 1 | 1 |
| 3 文字ゼロの仕組み | C | 1 | 1 |
| 4 移動ベクトル | A | 1 | 1 |
| 5 照準と連射 | A | 1 | 1 |
| 6 HPと無敵時間 | B | 1 | 1 |
| 7 部屋とランの進行 | C | 1 | 1 |
| 8 プレイヤー | A | 1–2 | 2, 4, 5, 6 |
| 9 スライムと出現予兆 | B | 1–2 | 2, 6 |
| 10 部屋の流れとHUD | C | 2 | 3, 7, 8, 9 |

テストの実行（Mac）：Unity エディタを閉じてから `tools/run-editmode-tests.sh`。Windows の人やエディタを開いたままのときは Window → General → Test Runner → EditMode → Run All。

---

### Task 1: 土台（担当 C）

**Files:**
- Create: `Assets/_Project/Scripts/Core/PictoArcher.Core.asmdef`
- Create: `Assets/_Project/Scripts/Game/PictoArcher.Game.asmdef`
- Create: `Assets/_Project/Tests/EditMode/PictoArcher.Tests.EditMode.asmdef`
- Create: `Assets/_Project/Tests/EditMode/ProjectSmokeTests.cs`
- Create: `tools/run-editmode-tests.sh`
- Create: `docs/assets-list.md`
- Create: `README.md`
- Modify: `ProjectSettings/ProjectSettings.asset`、`ProjectSettings/TagManager.asset`、`ProjectSettings/Physics2DSettings.asset`（エディタ上で設定する）
- Move: `Assets/Scenes/SampleScene.unity` → `Assets/_Project/Scenes/Run.unity`

**Interfaces:**
- Produces: アセンブリ `PictoArcher.Core`（参照なし）、`PictoArcher.Game`（参照：`PictoArcher.Core`, `Unity.InputSystem`, `Unity.TextMeshPro`）、`PictoArcher.Tests.EditMode`（参照：`PictoArcher.Core`, `PictoArcher.Game`, `Unity.TextMeshPro`, `UnityEngine.TestRunner`, `UnityEditor.TestRunner`、`nunit.framework.dll`、Editor のみ、`UNITY_INCLUDE_TESTS`）
- Produces: レイヤー `Player`=6, `Enemy`=7, `PlayerArrow`=8, `Wall`=9。タグ `Player`。
- Produces: `tools/run-editmode-tests.sh`（失敗すると終了コードが 0 以外）

- [ ] **Step 1: 3つの asmdef を作る**（上の Interfaces の参照のとおり。テスト用は `"includePlatforms": ["Editor"]`, `"overrideReferences": true`, `"precompiledReferences": ["nunit.framework.dll"]`, `"defineConstraints": ["UNITY_INCLUDE_TESTS"]`）

- [ ] **Step 2: 失敗するテストを書く**

```csharp
// Assets/_Project/Tests/EditMode/ProjectSmokeTests.cs
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace PictoArcher.Tests.EditMode
{
    public class ProjectSmokeTests
    {
        [Test]
        public void UnityVersion_IsPinned() =>
            Assert.AreEqual("6000.5.10f1", Application.unityVersion);

        [Test]
        public void DefaultResolution_Is1920x1080() 
        {
            Assert.AreEqual(1920, PlayerSettings.defaultScreenWidth);
            Assert.AreEqual(1080, PlayerSettings.defaultScreenHeight);
        }

        [Test]
        public void Layers_AreDefined()
        {
            Assert.AreEqual(6, LayerMask.NameToLayer("Player"));
            Assert.AreEqual(7, LayerMask.NameToLayer("Enemy"));
            Assert.AreEqual(8, LayerMask.NameToLayer("PlayerArrow"));
            Assert.AreEqual(9, LayerMask.NameToLayer("Wall"));
        }

        [Test]
        public void PlayerArrow_CollidesOnlyWithEnemyAndWall()
        {
            int arrow = LayerMask.NameToLayer("PlayerArrow");
            Assert.IsFalse(Physics2D.GetIgnoreLayerCollision(arrow, LayerMask.NameToLayer("Enemy")));
            Assert.IsFalse(Physics2D.GetIgnoreLayerCollision(arrow, LayerMask.NameToLayer("Wall")));
            Assert.IsTrue(Physics2D.GetIgnoreLayerCollision(arrow, LayerMask.NameToLayer("Player")));
            Assert.IsTrue(Physics2D.GetIgnoreLayerCollision(arrow, arrow));
        }
    }
}
```

- [ ] **Step 3: `tools/run-editmode-tests.sh` を作る**

```bash
#!/usr/bin/env bash
# エディタを閉じてから実行する。結果は Logs/editmode-results.xml
set -euo pipefail
UNITY="/Applications/Unity/Hub/Editor/6000.5.10f1/Unity.app/Contents/MacOS/Unity"
PROJECT="$(cd "$(dirname "$0")/.." && pwd)"
"$UNITY" -batchmode -nographics -projectPath "$PROJECT" \
  -runTests -testPlatform EditMode \
  -testResults "$PROJECT/Logs/editmode-results.xml" \
  -logFile "$PROJECT/Logs/editmode.log"
```

`chmod +x tools/run-editmode-tests.sh`

- [ ] **Step 4: 実行して失敗を確認する**

Run: `tools/run-editmode-tests.sh`
Expected: 終了コードが 0 以外。`Layers_AreDefined` と `PlayerArrow_CollidesOnlyWithEnemyAndWall` が失敗（解像度とバージョンはテンプレートで設定済みなので通る）

- [ ] **Step 5: エディタで設定する**
  - Tags and Layers：上の Interfaces のレイヤーとタグ
  - Physics 2D → Layer Collision Matrix：`PlayerArrow` は `Enemy` と `Wall` だけにチェック
  - Player → Splash Image → Show Splash Screen をオフ。**オフにできない場合はチームに報告**（設計書 §5 で、チームが判断することになっている）
  - Player → Resolution：Fullscreen Window、Resizable Window オン
  - Editor → Version Control：Visible Meta Files、Asset Serialization：Force Text（既定値になっているか確認だけ）
  - `SampleScene` を `Assets/_Project/Scenes/Run.unity` に移して名前を変え、Build Profiles のシーン一覧に登録する

- [ ] **Step 6: もう一度実行して全部通ることを確認する**

Run: `tools/run-editmode-tests.sh`
Expected: 終了コード 0、`Logs/editmode-results.xml` に 4件成功

- [ ] **Step 7: `docs/assets-list.md` と `README.md` を作る**
  - `assets-list.md`：表（ファイル／入手先URL／ライセンス／使用箇所）。CC0 以外を入れるときは、ゲームの外にクレジットを書く場所を備考に書く
  - `README.md`：Unity のバージョン、テストの実行方法、UnityYAMLMerge の設定方法（各自の `~/.gitconfig` に `[merge "unityyamlmerge"]` と `driver = '<Unity のパス>/Tools/UnityYAMLMerge' merge -p %O %B %A %A` を書く。Windows と Mac でパスが違うので両方書く）、ブランチの運用（Global Constraints の最後の行）

- [ ] **Step 8: Commit**

```bash
git add Assets ProjectSettings tools docs/assets-list.md README.md
git commit -m "chore: set up assemblies, layers, test runner"
```

---

### Task 2: データ形式と共通部品（担当 A、形式は全員で合意）

1週目の最初に3人で15分集まり、下のフィールドで合意してから実装する（設計書 §13「データ形式を全員で決める」）。

**Files:**
- Create: `Assets/_Project/Scripts/Game/Data/PlayerStats.cs`
- Create: `Assets/_Project/Scripts/Game/Data/EnemyDefinition.cs`
- Create: `Assets/_Project/Scripts/Game/Data/RoomDefinition.cs`
- Create: `Assets/_Project/Scripts/Game/Data/SkillId.cs`
- Create: `Assets/_Project/Scripts/Game/Data/SkillDefinition.cs`
- Create: `Assets/_Project/Scripts/Game/IHittable.cs`
- Create: `Assets/_Project/Scripts/Game/EnemyRegistry.cs`
- Create: `Assets/_Project/Data/PlayerStats.asset`
- Test: `Assets/_Project/Tests/EditMode/DataFormatTests.cs`

**Interfaces:**
- Produces（すべて `PictoArcher.Game`）:
  - `PlayerStats : ScriptableObject` — `int maxHp = 100`, `float moveSpeed = 5f`, `float fireInterval = 0.5f`, `float moveFireRateMultiplier = 1f`, `int arrowDamage = 10`, `float arrowSpeed = 12f`, `float arrowLifetime = 3f`, `float invulnerabilitySeconds = 0.6f`, `float dragRadiusPixels = 80f`。`moveFireRateMultiplier` 以外の初期値は計画での判断（調整値）。
  - `EnemyDefinition : ScriptableObject` — `GameObject prefab`, `int maxHp`, `float moveSpeed`, `int contactDamage`, `int xpReward`, `int coinReward`
  - `RoomDefinition : ScriptableObject` — `GameObject layoutPrefab`, `List<SpawnEntry> spawns`, `float telegraphSeconds = 0.8f`。`[Serializable] public struct SpawnEntry { public EnemyDefinition enemy; public Vector2 position; }`
  - `enum SkillId { FrontArrow, DiagonalArrow, RearArrow, Pierce, Ricochet, Fire, Ice, AttackSpeed, Critical, AttackUp, MaxHpUp, Orbit }`（設計書 §9 の表の順）
  - `SkillDefinition : ScriptableObject` — `SkillId id`, `Sprite icon`, `int maxStacks`（アセットは計画2で作る）
  - `interface IHittable { void TakeHit(int damage); }`
  - `static class EnemyRegistry` — `IReadOnlyList<Transform> Active`, `void Register(Transform t)`, `void Unregister(Transform t)`, `void Clear()`。同じものを2回登録しない、登録していないものの解除は無視する。

- [ ] **Step 1: 失敗するテストを書く**

```csharp
public class DataFormatTests
{
    [Test]
    public void PlayerStats_MoveFireRateDefaultsToFull()
    {
        var stats = ScriptableObject.CreateInstance<PlayerStats>();
        Assert.AreEqual(1f, stats.moveFireRateMultiplier);
    }

    [Test]
    public void SkillId_HasTwelveSkills() =>
        Assert.AreEqual(12, System.Enum.GetValues(typeof(SkillId)).Length);

    [Test]
    public void EnemyRegistry_IgnoresDuplicateRegisterAndUnknownUnregister()
    {
        EnemyRegistry.Clear();
        var a = new GameObject("a").transform;
        var b = new GameObject("b").transform;
        EnemyRegistry.Register(a);
        EnemyRegistry.Register(a);
        EnemyRegistry.Unregister(b);
        Assert.AreEqual(1, EnemyRegistry.Active.Count);
        EnemyRegistry.Clear();
        Object.DestroyImmediate(a.gameObject);
        Object.DestroyImmediate(b.gameObject);
    }
}
```

- [ ] **Step 2: 実行して失敗を確認する** — Run: `tools/run-editmode-tests.sh` / Expected: コンパイルエラー（型がない）

- [ ] **Step 3: 上の Interfaces どおりにクラスを実装する**。ScriptableObject には `[CreateAssetMenu(menuName = "PictoArcher/<型名>")]` を付ける。

- [ ] **Step 4: 実行して通ることを確認する** — Expected: すべて成功

- [ ] **Step 5: `Assets/_Project/Data/PlayerStats.asset` を作る**（Create → PictoArcher → PlayerStats、初期値のまま）

- [ ] **Step 6: Commit** — `git commit -m "feat: add data formats and shared interfaces"`

---

### Task 3: 文字ゼロの仕組み（担当 C）

**Files:**
- Create: `Assets/_Project/Fonts/Digits SDF.asset`（元のフォントファイルも `Assets/_Project/Fonts/` に置く）
- Modify: `Assets/TextMesh Pro/Resources/TMP Settings.asset`
- Test: `Assets/_Project/Tests/EditMode/NoTextGuardTests.cs`

**Interfaces:**
- Produces: フォントアセット `Assets/_Project/Fonts/Digits SDF.asset`。登録する文字は `0123456789+-×/%` だけ。TMP Settings の既定フォントにする。
- Produces: ガードテスト。以後、全員のタスクがこのテストに通る必要がある。

- [ ] **Step 1: 失敗するテストを書く**

```csharp
public class NoTextGuardTests
{
    const string FontPath = "Assets/_Project/Fonts/Digits SDF.asset";

    [Test]
    public void DigitFont_HasOnlyDigitsAndSymbols()
    {
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        Assert.IsNotNull(font);
        Assert.AreEqual(AtlasPopulationMode.Static, font.atlasPopulationMode);
        Assert.IsEmpty(font.fallbackFontAssetTable ?? new List<TMP_FontAsset>());
        foreach (var c in font.characterTable)
            Assert.IsFalse(char.IsLetter((char)c.unicode), $"letter glyph U+{c.unicode:X4}");
    }

    [Test]
    public void TmpDefaultFont_IsDigitFont()
    {
        Assert.AreEqual(FontPath, AssetDatabase.GetAssetPath(TMP_Settings.defaultFontAsset));
        Assert.IsEmpty(TMP_Settings.fallbackFontAssets ?? new List<TMP_FontAsset>());
    }

    // Review Focus 1：「New Text」などが保存されたまま残るのを防ぐ
    [Test]
    public void SerializedTexts_ContainNoLetters()
    {
        var files = Directory.GetFiles("Assets/_Project", "*.*", SearchOption.AllDirectories)
            .Where(p => p.EndsWith(".unity") || p.EndsWith(".prefab") || p.EndsWith(".asset"));
        foreach (var path in files)
        foreach (var line in File.ReadLines(path))
        {
            var t = line.TrimStart();
            if (!t.StartsWith("m_text:") && !t.StartsWith("m_Text:")) continue;
            var value = t.Substring(t.IndexOf(':') + 1);
            Assert.IsFalse(value.Any(char.IsLetter), $"{path}: {t}");
        }
    }

    [Test]
    public void Guard_DetectsLetters() =>
        Assert.IsTrue(" New Text".Any(char.IsLetter) && !" 7/10 ×2 +20%".Any(char.IsLetter));
}
```

- [ ] **Step 2: 実行して失敗を確認する** — Expected: `DigitFont_HasOnlyDigitsAndSymbols` と `TmpDefaultFont_IsDigitFont` が失敗

- [ ] **Step 3: フォントを用意する**
  - CC0 のフォント（候補：Kenney Fonts）を選んで `Assets/_Project/Fonts/` に置き、`docs/assets-list.md` に記録する
  - Window → TextMeshPro → Import TMP Essential Resources
  - Window → TextMeshPro → Font Asset Creator：Character Set = Custom Characters、`0123456789+-×/%`、Render Mode = SDFAA → `Digits SDF.asset` として保存し、Atlas Population Mode を Static にする
  - TMP Settings：Default Font Asset = `Digits SDF`、Fallback Font Assets を空にする

- [ ] **Step 4: 実行して通ることを確認する** — Expected: すべて成功

- [ ] **Step 5: Commit** — `git commit -m "feat: digit-only font and no-text guard tests"`

---

### Task 4: 移動ベクトルの計算（担当 A）

**Files:**
- Create: `Assets/_Project/Scripts/Core/MoveVectorResolver.cs`
- Test: `Assets/_Project/Tests/EditMode/MoveVectorResolverTests.cs`

**Interfaces:**
- Produces: `public static class MoveVectorResolver` — `public const float DragDeadZonePixels = 10f;` と `public static Vector2 Resolve(Vector2 keyboard, bool dragActive, Vector2 dragOrigin, Vector2 dragCurrent, float dragRadiusPixels)`。返す値の長さは常に 1 以下。

規則：キーボードの入力が 0 でなければキーボードを使う（長さが 1 を超えたら 1 に縮める）。そうでなければ、ドラッグ中のときに `dragCurrent - dragOrigin` を使う。長さが `DragDeadZonePixels` 未満なら 0。それ以上なら `/ dragRadiusPixels` して長さを 1 以下に縮める。

- [ ] **Step 1: 失敗するテストを書く**

```csharp
public class MoveVectorResolverTests
{
    const float R = 80f;
    static Vector2 Kb(float x, float y) => new Vector2(x, y);

    [Test] public void NoInput_IsZero() =>
        Assert.AreEqual(Vector2.zero, MoveVectorResolver.Resolve(Vector2.zero, false, Vector2.zero, Vector2.zero, R));

    [Test] public void KeyboardDiagonal_IsNotFaster() =>
        Assert.AreEqual(1f, MoveVectorResolver.Resolve(Kb(1, 1), false, Vector2.zero, Vector2.zero, R).magnitude, 1e-4f);

    [Test] public void DragInsideDeadZone_IsZero() =>
        Assert.AreEqual(Vector2.zero, MoveVectorResolver.Resolve(Vector2.zero, true, Vector2.zero, new Vector2(9, 0), R));

    [Test] public void DragHalfRadius_IsHalfSpeed() =>
        Assert.AreEqual(new Vector2(0.5f, 0), MoveVectorResolver.Resolve(Vector2.zero, true, Vector2.zero, new Vector2(40, 0), R));

    [Test] public void DragBeyondRadius_IsClampedToOne() =>
        Assert.AreEqual(1f, MoveVectorResolver.Resolve(Vector2.zero, true, Vector2.zero, new Vector2(300, 400), R).magnitude, 1e-4f);

    [Test] public void DragNotActive_IsIgnored() =>
        Assert.AreEqual(Vector2.zero, MoveVectorResolver.Resolve(Vector2.zero, false, Vector2.zero, new Vector2(80, 0), R));

    // Review Focus 2
    [Test] public void KeyboardAndDragTogether_KeyboardWinsAndNotFaster()
    {
        var v = MoveVectorResolver.Resolve(Kb(0, 1), true, Vector2.zero, new Vector2(80, 0), R);
        Assert.AreEqual(new Vector2(0, 1), v);
    }
}
```

- [ ] **Step 2: 実行して失敗を確認する** — Expected: コンパイルエラー（`MoveVectorResolver` がない）
- [ ] **Step 3: 規則どおりに `Resolve` を実装する**（`Vector2.ClampMagnitude` を使う）
- [ ] **Step 4: 実行して通ることを確認する** — Expected: 7件成功
- [ ] **Step 5: Commit** — `git commit -m "feat: resolve move vector from keyboard and drag"`

---

### Task 5: 照準と連射（担当 A）

**Files:**
- Create: `Assets/_Project/Scripts/Core/TargetSelector.cs`
- Create: `Assets/_Project/Scripts/Core/FireTimer.cs`
- Test: `Assets/_Project/Tests/EditMode/TargetSelectorTests.cs`, `FireTimerTests.cs`

**Interfaces:**
- Produces: `public static class TargetSelector { public static int FindNearest(Vector2 from, IReadOnlyList<Vector2> candidates); }` — 空なら `-1`。同じ距離なら番号が小さいほう。
- Produces: `public sealed class FireTimer` — `FireTimer(float interval, float moveFireRateMultiplier)`, `float Interval { get; set; }`, `float MoveFireRateMultiplier { get; set; }`, `bool Tick(float deltaTime, bool hasTarget, bool isMoving)`。true のとき1本撃つ。

FireTimer の規則：
- 待ち時間をためていき、実際の間隔は `Interval / (isMoving ? MoveFireRateMultiplier : 1)`。
- 1回の `Tick` で撃つのは最大1本。撃ったら待ち時間を 0 に戻す（余りを持ち越さない）。
- 敵がいない間も待ち時間はたまるが、間隔の分で止まる。敵が出たらすぐ1本撃てる。
- 作った直後は撃てる状態。

- [ ] **Step 1: 失敗するテストを書く**

```csharp
public class TargetSelectorTests
{
    [Test] public void Empty_ReturnsMinusOne() =>
        Assert.AreEqual(-1, TargetSelector.FindNearest(Vector2.zero, new List<Vector2>()));

    [Test] public void PicksNearest() =>
        Assert.AreEqual(1, TargetSelector.FindNearest(Vector2.zero,
            new List<Vector2> { new(5, 0), new(0, 2), new(-3, 0) }));

    [Test] public void Tie_PicksLowerIndex() =>
        Assert.AreEqual(0, TargetSelector.FindNearest(Vector2.zero,
            new List<Vector2> { new(2, 0), new(-2, 0) }));
}

public class FireTimerTests
{
    [Test] public void FiresImmediatelyWhenCreated() =>
        Assert.IsTrue(new FireTimer(0.5f, 1f).Tick(0f, true, false));

    [Test] public void WaitsForInterval()
    {
        var t = new FireTimer(0.5f, 1f);
        t.Tick(0f, true, false);
        Assert.IsFalse(t.Tick(0.3f, true, false));
        Assert.IsTrue(t.Tick(0.3f, true, false));
    }

    [Test] public void NoTarget_DoesNotFire_ButIsReadyWhenTargetAppears()
    {
        var t = new FireTimer(0.5f, 1f);
        t.Tick(0f, true, false);
        Assert.IsFalse(t.Tick(1f, false, false));
        Assert.IsTrue(t.Tick(0f, true, false));
    }

    // Review Focus 3
    [Test] public void LongFrame_FiresOnlyOnce()
    {
        var t = new FireTimer(0.5f, 1f);
        t.Tick(0f, true, false);
        Assert.IsTrue(t.Tick(5f, true, false));
        Assert.IsFalse(t.Tick(0.01f, true, false));
    }

    [Test] public void MovingWithHalfRate_DoublesInterval()
    {
        var t = new FireTimer(0.5f, 0.5f);
        t.Tick(0f, true, true);
        Assert.IsFalse(t.Tick(0.6f, true, true));
        Assert.IsTrue(t.Tick(0.4f, true, true));
    }

    [Test] public void MovingWithFullRate_SameAsStanding()
    {
        var t = new FireTimer(0.5f, 1f);
        t.Tick(0f, true, true);
        Assert.IsTrue(t.Tick(0.5f, true, true));
    }
}
```

- [ ] **Step 2: 実行して失敗を確認する** — Expected: コンパイルエラー
- [ ] **Step 3: 規則どおりに `FindNearest` と `FireTimer` を実装する**（距離の比較は `sqrMagnitude`）
- [ ] **Step 4: 実行して通ることを確認する** — Expected: 9件成功
- [ ] **Step 5: Commit** — `git commit -m "feat: nearest target selection and fire timer"`

---

### Task 6: HPと無敵時間（担当 B）

**Files:**
- Create: `Assets/_Project/Scripts/Core/Health.cs`, `HitInvulnerability.cs`
- Test: `Assets/_Project/Tests/EditMode/HealthTests.cs`, `HitInvulnerabilityTests.cs`

**Interfaces:**
- Produces: `public sealed class Health` — `Health(int max)`, `int Max { get; }`, `int Current { get; }`, `bool IsDead { get; }`, `event Action Died`, `int ApplyDamage(int amount)`（実際に減った量を返す。0以下の値や死亡後は 0 を返して何もしない）, `int Heal(int amount)`（実際に増えた量。死亡後は 0）。`Died` は1回だけ発生する。
- Produces: `public sealed class HitInvulnerability` — `HitInvulnerability(float duration)`, `bool TryHit(float now)`（無敵中でなければ true を返して無敵を開始）, `bool IsActive(float now)`

- [ ] **Step 1: 失敗するテストを書く**

```csharp
public class HealthTests
{
    [Test] public void Damage_ReducesAndClampsAtZero()
    {
        var h = new Health(30);
        Assert.AreEqual(10, h.ApplyDamage(10));
        Assert.AreEqual(20, h.ApplyDamage(50));
        Assert.AreEqual(0, h.Current);
        Assert.IsTrue(h.IsDead);
    }

    [Test] public void NonPositiveDamage_IsIgnored()
    {
        var h = new Health(30);
        Assert.AreEqual(0, h.ApplyDamage(0));
        Assert.AreEqual(0, h.ApplyDamage(-5));
        Assert.AreEqual(30, h.Current);
    }

    // Review Focus 4
    [Test] public void Died_FiresOnce_EvenWithExtraHits()
    {
        var h = new Health(10);
        int count = 0;
        h.Died += () => count++;
        h.ApplyDamage(10);
        Assert.AreEqual(0, h.ApplyDamage(10));
        Assert.AreEqual(1, count);
    }

    [Test] public void Heal_ClampsAtMax_AndDoesNothingWhenDead()
    {
        var h = new Health(30);
        h.ApplyDamage(5);
        Assert.AreEqual(5, h.Heal(100));
        h.ApplyDamage(30);
        Assert.AreEqual(0, h.Heal(10));
    }
}

public class HitInvulnerabilityTests
{
    [Test] public void BlocksHitsDuringWindow()
    {
        var inv = new HitInvulnerability(0.6f);
        Assert.IsTrue(inv.TryHit(1.0f));
        Assert.IsFalse(inv.TryHit(1.5f));
        Assert.IsTrue(inv.IsActive(1.5f));
        Assert.IsTrue(inv.TryHit(1.6f));
    }
}
```

- [ ] **Step 2: 実行して失敗を確認する** — Expected: コンパイルエラー
- [ ] **Step 3: Interfaces どおりに実装する**（`TryHit` は `now >= 無敵が終わる時刻` なら通す）
- [ ] **Step 4: 実行して通ることを確認する** — Expected: 5件成功
- [ ] **Step 5: Commit** — `git commit -m "feat: health and hit invulnerability"`

---

### Task 7: 部屋とランの進行（担当 C）

**Files:**
- Create: `Assets/_Project/Scripts/Core/RoomProgress.cs`, `RunLayout.cs`, `RunProgress.cs`
- Test: `Assets/_Project/Tests/EditMode/RoomProgressTests.cs`, `RunLayoutTests.cs`, `RunProgressTests.cs`

**Interfaces:**
- Produces: `public sealed class RoomProgress` — `void RegisterEnemy(int enemyId)`, `void MarkKilled(int enemyId)`（登録していない番号と2回目は無視）, `void FinishSpawning()`, `int AliveCount { get; }`, `bool IsCleared { get; }`（出現が終わっていて、生きている敵が 0）, `event Action Cleared`（1回だけ）, `bool TryConsumeExit()`（クリア後に1回だけ true）
- Produces: `public enum RoomType { Combat, MiniBoss, Angel, Boss }`、`public static class RunLayout { public const int RoomsPerChapter = 10; public static RoomType GetRoomType(int roomIndex); }` — 0〜3 Combat、4 MiniBoss、5 Angel、6〜8 Combat、9 Boss（番号は0始まり。設計書の「部屋1」が 0）。範囲外は `ArgumentOutOfRangeException`。
- Produces: `public enum RoomDot { Done, Current, Upcoming }`、`public sealed class RunProgress` — `int RoomIndex { get; }`（0から）, `bool IsChapterComplete { get; }`, `bool Advance()`（次の部屋へ進めたら true。最後の部屋からは `IsChapterComplete = true` にして false）, `RoomDot GetDot(int index)`

- [ ] **Step 1: 失敗するテストを書く**

```csharp
public class RoomProgressTests
{
    [Test] public void NotClearedUntilSpawningFinished()
    {
        var r = new RoomProgress();
        r.RegisterEnemy(1);
        r.MarkKilled(1);
        Assert.IsFalse(r.IsCleared);
        r.FinishSpawning();
        Assert.IsTrue(r.IsCleared);
    }

    [Test] public void EmptyRoom_ClearsWhenSpawningFinished()
    {
        var r = new RoomProgress();
        r.FinishSpawning();
        Assert.IsTrue(r.IsCleared);
    }

    // Review Focus 4
    [Test] public void DuplicateAndUnknownKills_AreIgnored()
    {
        var r = new RoomProgress();
        r.RegisterEnemy(1); r.RegisterEnemy(2); r.FinishSpawning();
        r.MarkKilled(1); r.MarkKilled(1); r.MarkKilled(99);
        Assert.AreEqual(1, r.AliveCount);
        Assert.IsFalse(r.IsCleared);
    }

    [Test] public void Cleared_FiresOnce()
    {
        var r = new RoomProgress();
        int count = 0;
        r.Cleared += () => count++;
        r.RegisterEnemy(1); r.FinishSpawning();
        r.MarkKilled(1); r.MarkKilled(1);
        Assert.AreEqual(1, count);
    }

    // Review Focus 5
    [Test] public void Exit_OnlyAfterClear_AndOnlyOnce()
    {
        var r = new RoomProgress();
        r.RegisterEnemy(1); r.FinishSpawning();
        Assert.IsFalse(r.TryConsumeExit());
        r.MarkKilled(1);
        Assert.IsTrue(r.TryConsumeExit());
        Assert.IsFalse(r.TryConsumeExit());
    }
}

public class RunLayoutTests
{
    [Test] public void ChapterLayout_MatchesSpec()
    {
        var expected = new[] { RoomType.Combat, RoomType.Combat, RoomType.Combat, RoomType.Combat,
            RoomType.MiniBoss, RoomType.Angel, RoomType.Combat, RoomType.Combat, RoomType.Combat, RoomType.Boss };
        Assert.AreEqual(10, RunLayout.RoomsPerChapter);
        for (int i = 0; i < 10; i++) Assert.AreEqual(expected[i], RunLayout.GetRoomType(i), $"room {i}");
    }

    [Test] public void OutOfRange_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => RunLayout.GetRoomType(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => RunLayout.GetRoomType(10));
    }
}

public class RunProgressTests
{
    [Test] public void AdvancesThroughTenRooms_ThenCompletes()
    {
        var p = new RunProgress();
        for (int i = 0; i < 9; i++) Assert.IsTrue(p.Advance());
        Assert.AreEqual(9, p.RoomIndex);
        Assert.IsFalse(p.Advance());
        Assert.IsTrue(p.IsChapterComplete);
        Assert.AreEqual(9, p.RoomIndex);
    }

    [Test] public void Dots_ShowDoneCurrentUpcoming()
    {
        var p = new RunProgress();
        p.Advance(); p.Advance();
        Assert.AreEqual(RoomDot.Done, p.GetDot(1));
        Assert.AreEqual(RoomDot.Current, p.GetDot(2));
        Assert.AreEqual(RoomDot.Upcoming, p.GetDot(3));
    }
}
```

- [ ] **Step 2: 実行して失敗を確認する** — Expected: コンパイルエラー
- [ ] **Step 3: Interfaces どおりに実装する**（`RoomProgress` は生きている敵の番号を `HashSet<int>` で持つ）
- [ ] **Step 4: 実行して通ることを確認する** — Expected: 9件成功
- [ ] **Step 5: Commit** — `git commit -m "feat: room clear, exit and run progress rules"`

---

### Task 8: プレイヤー（担当 A）

MonoBehaviour のつなぎ込みなので、確認は自分の試し用シーン `Sandbox_A.unity` での手動確認にする。ルールはタスク4・5・6でテスト済み。

**Files:**
- Create: `Assets/_Project/Scripts/Game/Player/PlayerController.cs`, `PlayerShooter.cs`, `PlayerHealth.cs`, `Arrow.cs`
- Create: `Assets/_Project/Prefabs/Player.prefab`, `Arrow.prefab`
- Create: `Assets/_Project/Scenes/Sandbox/Sandbox_A.unity`

**Interfaces:**
- Consumes: `PlayerStats`、`IHittable`、`EnemyRegistry`（タスク2）、`MoveVectorResolver`（タスク4）、`TargetSelector`・`FireTimer`（タスク5）、`Health`・`HitInvulnerability`（タスク6）
- Produces:
  - `PlayerController : MonoBehaviour` — `[SerializeField] PlayerStats stats`, `[SerializeField] InputActionReference move`（`Player/Move`）, `bool IsMoving { get; }`, `void Teleport(Vector2 position)`
  - `PlayerShooter : MonoBehaviour` — `[SerializeField] Arrow arrowPrefab`
  - `PlayerHealth : MonoBehaviour, IHittable` — `Health Health { get; }`, `event Action Died`
  - `Arrow : MonoBehaviour` — `void Launch(Vector2 direction, float speed, int damage, float lifetime)`
- Prefab：`Player` はタグ `Player`、レイヤー `Player`、`Rigidbody2D`（Dynamic、Gravity Scale 0、回転固定）と `CircleCollider2D`。`Arrow` はレイヤー `PlayerArrow`、`Rigidbody2D`（Kinematic）と Trigger の `Collider2D`。

- [ ] **Step 1: `PlayerController` を実装する**。毎フレーム、`move` の値、`Mouse.current.leftButton`（押した瞬間の位置をドラッグの中心にする）と `Mouse.current.position` を `MoveVectorResolver.Resolve` に渡し、`FixedUpdate` で `Rigidbody2D.linearVelocity = v * stats.moveSpeed`。`IsMoving` は v が 0 でないとき true。
- [ ] **Step 2: `PlayerShooter` を実装する**。`EnemyRegistry.Active` の位置を `TargetSelector.FindNearest` に渡し、`FireTimer.Tick(Time.deltaTime, 敵がいる, controller.IsMoving)` が true なら、その敵の方向へ `Arrow` を出す。
- [ ] **Step 3: `Arrow` を実装する**。`Launch` の方向へ進み、`lifetime` 秒で消える。`OnTriggerEnter2D` で相手に `IHittable` があれば `TakeHit(damage)` して消える。レイヤー `Wall` に当たっても消える。
- [ ] **Step 4: `PlayerHealth` を実装する**。`TakeHit` は `HitInvulnerability.TryHit(Time.time)` が true のときだけ `Health.ApplyDamage`。無敵中はスプライトを点滅させる。`Health.Died` を `Died` として外に出す。
- [ ] **Step 5: プレハブを作り、`Sandbox_A` で確認する**。シーンに Player、壁（レイヤー `Wall`）、`IHittable` を持つ的の代わりとして四角を2つ置き（`EnemyRegistry.Register` する簡単な試し用スクリプトは Sandbox 内にだけ置く）、再生する。
  Expected:
  - WASD・矢印キーで動く。斜めでも速くならない
  - マウスを押したままドラッグすると、押した位置からの方向へ動く。キーボードと同時ならキーボードが勝つ
  - 止まっていても動いていても、近い的に向かって 0.5 秒ごとに矢が飛ぶ
  - 的がないときは撃たない
  - 矢が壁で消える
  - 画面に文字が出ていない
- [ ] **Step 6: テストを実行する** — Run: `tools/run-editmode-tests.sh` / Expected: すべて成功（特に `SerializedTexts_ContainNoLetters`）
- [ ] **Step 7: Commit** — `git commit -m "feat: player movement, auto fire and arrows"`

---

### Task 9: スライムと出現予兆（担当 B）

**Files:**
- Create: `Assets/_Project/Scripts/Game/Enemies/EnemyActor.cs`, `ChaseMovement.cs`, `SpawnTelegraph.cs`
- Create: `Assets/_Project/Prefabs/Slime.prefab`, `SpawnTelegraph.prefab`
- Create: `Assets/_Project/Data/Enemies/Slime.asset`
- Create: `Assets/_Project/Scenes/Sandbox/Sandbox_B.unity`

**Interfaces:**
- Consumes: `EnemyDefinition`、`IHittable`、`EnemyRegistry`（タスク2）、`Health`（タスク6）
- Produces:
  - `EnemyActor : MonoBehaviour, IHittable` — `void Init(EnemyDefinition def, int enemyId, Transform target)`, `int EnemyId { get; }`, `event Action<EnemyActor> Died`。`Init` で `EnemyRegistry.Register`、死亡したら `Died` を発生させて `Unregister` し、自分を消す。当たったら一瞬白く光る。
  - `ChaseMovement : MonoBehaviour` — `target` へ `def.moveSpeed` で進む。`OnCollisionStay2D` で相手がタグ `Player` で `IHittable` なら `TakeHit(def.contactDamage)`（連続して当たらないのはプレイヤー側の無敵時間で防ぐ）。
  - `SpawnTelegraph : MonoBehaviour` — `void Play(Vector2 position, float seconds, Action onFinished)`。赤い円と `!` のスプライトを `seconds` 秒表示（点滅しながら少し大きくなる）してから `onFinished` を呼び、自分を消す。
- `Slime.asset`：`maxHp = 30`, `moveSpeed = 2`, `contactDamage = 10`, `xpReward = 1`, `coinReward = 1`（計画での判断、調整値）
- Prefab：`Slime` はレイヤー `Enemy`、`Rigidbody2D`（Dynamic、Gravity Scale 0、回転固定）、`CircleCollider2D`

- [ ] **Step 1: `EnemyActor` を実装する**（`Health` を `def.maxHp` で作り、`TakeHit` → `ApplyDamage`、`Health.Died` → 上の死亡処理）
- [ ] **Step 2: `ChaseMovement` を実装する**
- [ ] **Step 3: `SpawnTelegraph` を実装する**。赤と `!` の画像は CC0 素材から選び、`docs/assets-list.md` に記録する
- [ ] **Step 4: `Sandbox_B` で確認する**。Player プレハブ（タスク8が未完成なら、タグ `Player` の四角を矢印キーで動かす試し用スクリプトを Sandbox 内にだけ置く）と、`SpawnTelegraph.Play` の後に Slime を出す試し用スクリプトを置いて再生する。
  Expected:
  - 赤い円と `!` が 0.8 秒出て、その場所にスライムが出る
  - スライムがプレイヤーを追ってくる
  - 触れると HP が 10 減り、0.6 秒は続けて減らない
  - 矢を3本当てると（矢のダメージ10、HP30）消える。同じフレームに2本当たっても `Died` は1回
  - 画面に文字が出ていない
- [ ] **Step 5: テストを実行する** — Expected: すべて成功
- [ ] **Step 6: Commit** — `git commit -m "feat: slime enemy and spawn telegraph"`

---

### Task 10: 部屋の流れとHUD（担当 C）

`Run.unity` を編集するのはこのタスクだけ。A と B はプレハブを渡す。

**Files:**
- Create: `Assets/_Project/Scripts/Game/Run/RoomController.cs`, `Door.cs`, `RunController.cs`
- Create: `Assets/_Project/Scripts/Game/UI/HudView.cs`
- Create: `Assets/_Project/Prefabs/Door.prefab`, `Room_Test.prefab`（壁で囲った1画面の部屋と扉）
- Create: `Assets/_Project/Data/Rooms/Room_Test.asset`（スライム3体）
- Modify: `Assets/_Project/Scenes/Run.unity`

**Interfaces:**
- Consumes: `RoomDefinition`・`EnemyRegistry`（タスク2）、`RoomProgress`・`RunProgress`・`RunLayout`（タスク7）、`PlayerController.Teleport`・`PlayerHealth.Died`・`PlayerHealth.Health`（タスク8）、`EnemyActor.Init`・`EnemyActor.Died`・`SpawnTelegraph.Play`（タスク9）
- Produces:
  - `RoomController : MonoBehaviour` — `void Begin(RoomDefinition def, Transform player)`, `event Action ExitRequested`。全員の予兆が終わって敵を出したら `FinishSpawning`。`Cleared` で扉を開く。
  - `Door : MonoBehaviour` — `void Open()`。閉じた見た目と、開いて光る ➜ の見た目を切り替える。プレイヤーが触れたら `RoomController` に知らせ、`RoomProgress.TryConsumeExit()` が true のときだけ `ExitRequested` を発生させる。
  - `RunController : MonoBehaviour` — `[SerializeField] RoomDefinition testRoom`。開始時に `EnemyRegistry.Clear()`。今の部屋を消して次の部屋のプレハブを出し、プレイヤーを入口へ `Teleport` する。計画1では10部屋すべてに `testRoom` を使う。10部屋目を出たとき、またはプレイヤーが死んだときはシーンを読み直す（クリア画面と死亡画面は計画3）。
  - `HudView : MonoBehaviour` — `void Bind(Health playerHealth, RunProgress progress)`。プレイヤー頭上の HP バーと数字（`Digits SDF`）。画面上部の進行ドット：`Done`／`Current` は ●（`Current` は大きく光らせる）、`Upcoming` は ○、最後（ボス）は 💀 のスプライト。

- [ ] **Step 1: `RoomController` と `Door` を実装する**（敵ごとに番号を振り、`EnemyActor.Died` → `RoomProgress.MarkKilled`）
- [ ] **Step 2: `RunController` を実装する**
- [ ] **Step 3: `HudView` を実装する**。数字の表示には TextMeshPro を使い、TMP コンポーネントを置いたら最初に入っている「New Text」を消す（`SerializedTexts_ContainNoLetters` で落ちる）
- [ ] **Step 4: カメラを設定する**。1部屋が1画面にちょうど収まる Orthographic サイズにし、16:9 以外では上下か左右に黒帯を出して部屋全体を見せる
- [ ] **Step 5: `Run.unity` を組み立て、再生して確認する**
  Expected:
  - 入室すると赤い `!` 円が出て、スライム3体が出る
  - 全部倒すと扉が開いて ➜ が光る。倒す前に扉に触れても進まない
  - 扉に入ると次の部屋になり、進行ドットが1つ進む。扉に触れ続けても1部屋しか進まない
  - 10部屋目の扉を出るとシーンが最初からになる
  - HP が 0 になるとシーンが最初からになる
  - ウィンドウを 4:3 と 21:9 に変えても部屋全体が見える
  - 画面のどこにも文字が出ていない
- [ ] **Step 6: テストを実行する** — Expected: すべて成功
- [ ] **Step 7: Commit** — `git commit -m "feat: room flow, door, run loop and HUD"`

---

## 計画1の完了条件

- `tools/run-editmode-tests.sh` がすべて成功する
- `Run.unity` で上のタスク10 Step 5 の確認項目がすべて満たされる
- 3人それぞれがこの状態を自分の PC で再生して確認した

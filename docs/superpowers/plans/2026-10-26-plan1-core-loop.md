# 計画1 コアループ 実装計画（第2版）

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 1つのフィールドで数ウェーブ続けて遊べるようにする（赤い `!` 円 → 追う敵が出現 → 移動しながら自動射撃 → 全滅 → 約1.5秒の休みで `1/50` が `2/50` に変わる → 次のウェーブ）。

**Architecture:** ゲームのルール（移動ベクトルの計算、照準、連射、HP、ウェーブの進行）は MonoBehaviour を持たない純粋な C# クラスとして `PictoArcher.Core` に置き、EditMode テストで固める。Unity 上の部品（プレハブ、物理、表示）は `PictoArcher.Game` に置き、Core のクラスを呼ぶだけにする。数値はすべて ScriptableObject に置く。描画は Pixel Perfect Camera（基準 480×270）で、カメラは動かさない。

**Tech Stack:** Unity 6000.5.10f1、URP 2D（17.6、Pixel Perfect Camera）、Input System 1.20、TextMeshPro（`Unity.TextMeshPro`）、Tilemap、Unity Test Framework 1.7（NUnit）

**Spec:** `docs/superpowers/specs/2026-10-26-pictoarcher-design-v2.md`（全体の分け方は `docs/superpowers/plans/2026-10-26-roadmap.md`）

前の版（`2026-10-24-plan1-core-loop.md`）の「部屋と扉」「進行ドット」は、この版で「ウェーブの進行」「`n/50` の表示」に置き換えた。

## Global Constraints

- Unity は全員 6000.5.10f1 に固定する（設計書 §3）。
- 画面に文字を出さない。アルファベット・ひらがな・カタカナ・漢字、`LV` `HP` `BOSS` `x2` `s`（秒）などの略語・単位もすべて禁止（§5）。
- 使ってよい表記：レベル `★5`／倍率 `×2`／時間 `⏱ 30`／割合 `+20%`／進行 `23/50`（§5）。
- 色の意味：赤＝危険・攻撃／緑＝回復／黄＝コイン・経験値／青＝防御・氷／紫＝レア。この5色は敵の体の主な色やフィールドの照明には使わない。プレイヤーの弾は白（§5）。
- 敵の攻撃は必ず先に赤い線・赤い円で予告する（§5）。
- 絵文字をフォントで出さない。ピクトグラムはすべてスプライト画像（ドット絵）で用意する（§5）。
- TextMeshPro のフォントアセットには数字と記号だけを登録する。ドット絵に合うピクセルフォントを使う（§5）。
- 開発ビルドやデバッグ表示でも画面に文字を出さない（ログはコンソールのみ）（§10）。
- 描画の基準解像度は 480×270（1920×1080 の4倍）。Pixel Perfect Camera を使う。画像は Filter Mode を Point、圧縮なしで取り込む（§11, §12）。
- フィールドは1画面＝480×270 の中に収める（壁を含む）。戦える広さは壁の内側でおよそ 440×220。タイルは 16px。カメラは動かない（§6, §11）。
- プレイヤーと通常の敵は 24〜32px 四方。向きは左右だけ（右向きを描き、左は反転）（§11）。
- 入力は Input System。キーボードとマウスのドラッグを同じ「移動ベクトル」に変換する（§12）。
- 射撃は常に自動で、移動中も撃てる。一番近い敵を狙う。敵がいないときは撃たない（§4）。
- 移動中の連射速度は調整値で、初期値は止まっているときと同じ 100%（§4）。
- 被弾したら短い無敵時間（点滅）がある（§4）。
- 1ウェーブの流れ：赤い `!` 円 → 出現 → 戦闘 → 全滅 → 約1.5秒の休み（`23/50` が `24/50` に変わる）→ 次のウェーブ（§6）。
- 数値（HP、攻撃力、速度、出現数）はすべて ScriptableObject の調整値にする（§8）。
- シーンは同時に編集しない。担当ごとにプレハブを分け、シーンを編集するのは1人に決める（§12）。
- `main` へは直接入れず、ほかの1人が確認してからマージする。Claude が書いたコードも、マージ前に人が読む（§12）。
- 純粋なロジック（ダメージ計算、ウェーブの進行など）には EditMode テストを書く（§12）。
- 名前空間：ルールは `PictoArcher.Core`、Unity 上の部品は `PictoArcher.Game`、エディタ用は `PictoArcher.Editor`。【計画での判断】
- コードの名前は世界観に依存させず、役割で付ける（弾は `Projectile`、追う敵は `Chaser`）。【計画での判断】
- 1タスク＝1ブランチ（`<担当>/t<番号>-<短い名前>`、例 `c/t1-foundation`）。【計画での判断】
- 長さの単位：1ユニット＝16px（Assets PPU 16）。フィールド 480×270px は 30×16.875 ユニット。【計画での判断】

## Review Focus

1. **TextMeshPro を置いたときの既定文字「New Text」がシーンやプレハブに残る** → 画面に英字が出る。保存されたテキストに文字が1つでも入っていたらテストで落とす（タスク3）。
2. **キーボードとマウスのドラッグを同時に使う** → 速さが最大を超えない。キーボードを優先する（タスク4）。
3. **ウィンドウを掴んでいる間などにフレームが大きく止まる** → 再開したときに弾がまとめて何本も出ない。休み中に止まっても、ウェーブは1つだけ進む（タスク5、タスク7）。
4. **同じフレームで2発の弾が最後の敵を倒す** → 倒した数は1回だけ数え、死亡の通知も全滅の通知も1回だけ。ウェーブは1つだけ進む（タスク6、タスク7）。
5. **最後の敵と相打ちになる（同じフレームで全滅とプレイヤーの死亡）** → 死亡を優先し、次のウェーブは始まらない。どちらの通知が先に来ても同じ（タスク7）。

16:9 以外のウィンドウ（4:3、21:9）でもフィールド全体が見えることは、タスク1のテスト（カメラの設定）とタスク10の手動確認で見る。

## ファイル構成

```
CLAUDE.md                  全員の Claude が読むルール（タスク1）
Assets/_Project/
  Scripts/Core/            PictoArcher.Core（純粋な C#、テスト対象）
    PictoArcher.Core.asmdef
    MoveVectorResolver.cs  移動ベクトルの計算
    TargetSelector.cs      一番近い敵を選ぶ
    FireTimer.cs           連射の間隔
    Health.cs              HP
    HitInvulnerability.cs  無敵時間
    WaveLayout.cs          50ウェーブの種類（WaveKind を含む）
    WaveProgress.cs        1ウェーブの敵の数と全滅
    WaveSequence.cs        今のウェーブ番号、休み、次のウェーブへ（WavePhase を含む）
  Scripts/Game/            PictoArcher.Game（MonoBehaviour と ScriptableObject）
    PictoArcher.Game.asmdef
    Data/                  PlayerStats, EnemyDefinition, WaveDefinition, SkillId, SkillDefinition
    IHittable.cs           弾や体当たりが当たる相手
    EnemyRegistry.cs       今いる敵の一覧
    Player/                PlayerController, PlayerShooter, PlayerHealth, Projectile
    Enemies/               EnemyActor, ChaseMovement, SpawnTelegraph
    Run/                   WaveSpawner, RunController
    UI/                    HudView, HeadHpView
  Scripts/Editor/          PictoArcher.Editor（エディタ専用）
    PictoArcher.Editor.asmdef
    PixelArtImportPostprocessor.cs  ドット絵の取り込み設定
  Tests/EditMode/          PictoArcher.Tests.EditMode
  Data/                    ScriptableObject のアセット（Enemies/, Waves/）
  Prefabs/                 MainCamera, Player, Projectile, Chaser, SpawnTelegraph, Field_Test
  Scenes/Run.unity         Cさんだけが編集する
  Scenes/Sandbox/          Sandbox_A.unity, Sandbox_B.unity（各自の試し用、他の人は触らない）
  Fonts/                   数字と記号だけのピクセルフォント
  Art/                     絵（フリー素材と、絵の担当から届いたスプライトシート）
tools/run-editmode-tests.sh
docs/assets-list.md
```

## 担当と順番

見積もりは設計書 §13 のプログラマーの工数から割り振った。2週目の終わりに「実績」を各自が書き込み、3人で比べる（ロードマップ「見積もりと実際の日数を比べる」）。

| タスク | 担当 | 週 | 先に終わっている必要があるもの | 見積もり（人日） | 実績 |
|---|---|---|---|---|---|
| 1 土台と `CLAUDE.md` | C | 1（初日） | なし | 0.75 | |
| 2 データ形式と共通部品 | A（形式は全員で合意） | 1 | 1 | 0.5 | |
| 3 文字ゼロとドット絵の取り込み | C | 1 | 1 | 0.75 | |
| 4 移動ベクトル | A | 1 | 1 | 0.5 | |
| 5 照準と連射 | A | 1 | 1 | 0.5 | |
| 6 HPと無敵時間 | B | 1 | 1 | 0.5 | |
| 7 ウェーブの進行 | C | 1 | 1 | 1 | |
| 8 プレイヤーと弾 | A | 1–2 | 2, 3, 4, 5, 6 | 1.5 | |
| 9 追う敵と出現予兆 | B | 1–2 | 2, 3, 6 | 2.25 | |
| 10 フィールド・ウェーブの流れ・HUD | C | 2 | 3, 7, 8, 9 | 1.5 | |

1週目の終わりの目安（設計書 §13）：タスク8とタスク9の Sandbox で、プレイヤーが動いて自動で撃ち、追う敵が1体動く。

テストの実行（Mac）：Unity エディタを閉じてから `tools/run-editmode-tests.sh`。Windows の人やエディタを開いたままのときは Window → General → Test Runner → EditMode → Run All。

---

### Task 1: 土台と `CLAUDE.md`（担当 C）

**Files:**
- Create: `CLAUDE.md`
- Create: `Assets/_Project/Scripts/Core/PictoArcher.Core.asmdef`
- Create: `Assets/_Project/Scripts/Game/PictoArcher.Game.asmdef`
- Create: `Assets/_Project/Scripts/Editor/PictoArcher.Editor.asmdef`
- Create: `Assets/_Project/Tests/EditMode/PictoArcher.Tests.EditMode.asmdef`
- Create: `Assets/_Project/Tests/EditMode/ProjectSmokeTests.cs`
- Create: `Assets/_Project/Prefabs/MainCamera.prefab`
- Create: `tools/run-editmode-tests.sh`
- Create: `docs/assets-list.md`
- Create: `README.md`
- Modify: `ProjectSettings/ProjectSettings.asset`、`ProjectSettings/TagManager.asset`、`ProjectSettings/Physics2DSettings.asset`（エディタ上で設定する）
- Move: `Assets/Scenes/SampleScene.unity` → `Assets/_Project/Scenes/Run.unity`

**Interfaces:**
- Produces: アセンブリ
  - `PictoArcher.Core`（参照なし）
  - `PictoArcher.Game`（参照：`PictoArcher.Core`, `Unity.InputSystem`, `Unity.TextMeshPro`, `Unity.RenderPipelines.Universal.Runtime`, `Unity.RenderPipelines.Universal.2D.Runtime`）
  - `PictoArcher.Editor`（参照：`PictoArcher.Core`, `PictoArcher.Game`。`"includePlatforms": ["Editor"]`）
  - `PictoArcher.Tests.EditMode`（参照：`PictoArcher.Core`, `PictoArcher.Game`, `PictoArcher.Editor`, `Unity.TextMeshPro`, `Unity.RenderPipelines.Universal.Runtime`, `Unity.RenderPipelines.Universal.2D.Runtime`, `UnityEngine.TestRunner`, `UnityEditor.TestRunner`、`nunit.framework.dll`。Editor のみ、`UNITY_INCLUDE_TESTS`）
- Produces: レイヤー `Player`=6, `Enemy`=7, `PlayerShot`=8, `Wall`=9（障害物も `Wall`）。タグ `Player`。
- Produces: `MainCamera.prefab` — `Camera`（Orthographic、背景は黒）と `UnityEngine.Rendering.Universal.PixelPerfectCamera`（`assetsPPU = 16`, `refResolutionX = 480`, `refResolutionY = 270`, `cropFrame = StretchFill`, `gridSnapping = UpscaleRenderTexture`）。位置は (0, 0, -10) で動かさない。`Run.unity` と各自の Sandbox はこのプレハブを置く。
- Produces: `tools/run-editmode-tests.sh`（失敗すると終了コードが 0 以外）
- Produces: `CLAUDE.md`（見出し `## 文字ゼロのルール`、`## フォルダ構成`、`## テストの書き方`、`## シーンとブランチ`）

`cropFrame = StretchFill` は、16:9 以外のウィンドウで上下か左右に黒帯を出し、480×270 全体を見せる。【計画での判断】

- [ ] **Step 1: 4つの asmdef を作る**（上の Interfaces の参照のとおり。テスト用は `"includePlatforms": ["Editor"]`, `"overrideReferences": true`, `"precompiledReferences": ["nunit.framework.dll"]`, `"defineConstraints": ["UNITY_INCLUDE_TESTS"]`）

- [ ] **Step 2: 失敗するテストを書く**

```csharp
// Assets/_Project/Tests/EditMode/ProjectSmokeTests.cs
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

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
            Assert.AreEqual(8, LayerMask.NameToLayer("PlayerShot"));
            Assert.AreEqual(9, LayerMask.NameToLayer("Wall"));
        }

        [Test]
        public void PlayerShot_CollidesOnlyWithEnemyAndWall()
        {
            int shot = LayerMask.NameToLayer("PlayerShot");
            Assert.IsFalse(Physics2D.GetIgnoreLayerCollision(shot, LayerMask.NameToLayer("Enemy")));
            Assert.IsFalse(Physics2D.GetIgnoreLayerCollision(shot, LayerMask.NameToLayer("Wall")));
            Assert.IsTrue(Physics2D.GetIgnoreLayerCollision(shot, LayerMask.NameToLayer("Player")));
            Assert.IsTrue(Physics2D.GetIgnoreLayerCollision(shot, shot));
        }

        [Test]
        public void MainCamera_IsPixelPerfect480x270()
        {
            var go = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/MainCamera.prefab");
            Assert.IsNotNull(go);
            Assert.IsTrue(go.GetComponent<Camera>().orthographic);
            var ppc = go.GetComponent<PixelPerfectCamera>();
            Assert.AreEqual(16, ppc.assetsPPU);
            Assert.AreEqual(480, ppc.refResolutionX);
            Assert.AreEqual(270, ppc.refResolutionY);
            Assert.AreEqual(PixelPerfectCamera.CropFrame.StretchFill, ppc.cropFrame);
            Assert.AreEqual(PixelPerfectCamera.GridSnapping.UpscaleRenderTexture, ppc.gridSnapping);
        }

        [Test]
        public void ClaudeMd_HasFourRules()
        {
            var text = File.ReadAllText("CLAUDE.md");
            StringAssert.Contains("## 文字ゼロのルール", text);
            StringAssert.Contains("## フォルダ構成", text);
            StringAssert.Contains("## テストの書き方", text);
            StringAssert.Contains("## シーンとブランチ", text);
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
Expected: 終了コードが 0 以外。`Layers_AreDefined`、`PlayerShot_CollidesOnlyWithEnemyAndWall`、`MainCamera_IsPixelPerfect480x270`、`ClaudeMd_HasFourRules` が失敗（解像度とバージョンはテンプレートで設定済みなので通る）

- [ ] **Step 5: エディタで設定する**
  - Tags and Layers：上の Interfaces のレイヤーとタグ
  - Physics 2D → Layer Collision Matrix：`PlayerShot` は `Enemy` と `Wall` だけにチェック
  - Player → Splash Image → Show Splash Screen をオフ。**オフにできない場合はチームに報告**（設計書 §5 で、チームが判断することになっている）
  - Player → Resolution：Fullscreen Window、Resizable Window オン
  - Editor → Version Control：Visible Meta Files、Asset Serialization：Force Text（既定値になっているか確認だけ）
  - `SampleScene` を `Assets/_Project/Scenes/Run.unity` に移して名前を変え、Build Profiles のシーン一覧に登録する。中の Main Camera を消して `MainCamera.prefab` を置く

- [ ] **Step 6: `CLAUDE.md` を書く**。各見出しに書く中身（設計書 §12）：
  - `## 文字ゼロのルール`：設計書 §5 の表を写す（禁止、代わりの表記、色の意味、予兆、絵文字、フォント、素材）。「開発ビルドやデバッグ表示でも文字を出さない。ログはコンソールだけ」も書く
  - `## フォルダ構成`：この計画の「ファイル構成」を写す。ルールは `Scripts/Core`（MonoBehaviour を使わない）、Unity の部品は `Scripts/Game` に置くこと
  - `## テストの書き方`：Core のルールは先に EditMode テストを書いてから実装する。NUnit、`Assets/_Project/Tests/EditMode/<クラス名>Tests.cs`。実行方法。`NoTextGuardTests` を含む全テストが通ってからプルリクエストを出す
  - `## シーンとブランチ`：`Run.unity` を編集するのは Cさんだけ。ほかの人はプレハブと `Scenes/Sandbox/Sandbox_<担当>.unity` で作業する。ブランチ名（Global Constraints）。`main` へは直接入れず、ほかの1人が確認してからマージする。Claude が書いたコードもマージ前に人が読む

- [ ] **Step 7: もう一度実行して全部通ることを確認する**

Run: `tools/run-editmode-tests.sh`
Expected: 終了コード 0、`Logs/editmode-results.xml` に 6件成功

- [ ] **Step 8: `docs/assets-list.md` と `README.md` を作る**
  - `assets-list.md`：表（ファイル／入手先URL／ライセンス／使用箇所／文字の確認）。CC0 以外を入れるときは、ゲームの外（README と配布ページ）にクレジットを書く
  - `README.md`：Unity のバージョン、テストの実行方法、UnityYAMLMerge の設定方法（各自の `~/.gitconfig` に `[merge "unityyamlmerge"]` と `driver = '<Unity のパス>/Tools/UnityYAMLMerge' merge -p %O %B %A %A` を書く。Windows と Mac でパスが違うので両方書く。`.gitattributes` は設定済み）、CC-BY 素材のクレジット欄（空でよい）

- [ ] **Step 9: 3人で `CLAUDE.md` を読み、直すところがあれば直す**（設計書 §12「3人で確認する」）

- [ ] **Step 10: Commit**

```bash
git add CLAUDE.md Assets ProjectSettings tools docs/assets-list.md README.md
git commit -m "chore: set up assemblies, layers, pixel perfect camera, CLAUDE.md"
```

---

### Task 2: データ形式と共通部品（担当 A、形式は全員で合意）

1週目の最初に3人で15分集まり、下のフィールドで合意してから実装する（設計書 §13「データ形式を全員で決める」）。

**Files:**
- Create: `Assets/_Project/Scripts/Game/Data/PlayerStats.cs`
- Create: `Assets/_Project/Scripts/Game/Data/EnemyDefinition.cs`
- Create: `Assets/_Project/Scripts/Game/Data/WaveDefinition.cs`
- Create: `Assets/_Project/Scripts/Game/Data/SkillId.cs`
- Create: `Assets/_Project/Scripts/Game/Data/SkillDefinition.cs`
- Create: `Assets/_Project/Scripts/Game/IHittable.cs`
- Create: `Assets/_Project/Scripts/Game/EnemyRegistry.cs`
- Create: `Assets/_Project/Data/PlayerStats.asset`
- Test: `Assets/_Project/Tests/EditMode/DataFormatTests.cs`

**Interfaces:**
- Produces（すべて `PictoArcher.Game`）:
  - `PlayerStats : ScriptableObject` — `int maxHp = 100`, `float moveSpeed = 6f`（ユニット/秒、約96px/秒）, `float fireInterval = 0.5f`, `float moveFireRateMultiplier = 1f`, `int projectileDamage = 10`, `float projectileSpeed = 14f`, `float projectileLifetime = 3f`, `float invulnerabilitySeconds = 0.6f`, `float dragRadiusPixels = 80f`（画面のピクセル）。`moveFireRateMultiplier` 以外の初期値は計画での判断（調整値）。
  - `EnemyDefinition : ScriptableObject` — `GameObject prefab`, `int maxHp`, `float moveSpeed`, `int contactDamage`, `int xpReward`, `int coinReward`
  - `WaveDefinition : ScriptableObject` — `List<SpawnEntry> spawns`, `float telegraphSeconds = 0.8f`。`[Serializable] public struct SpawnEntry { public EnemyDefinition enemy; public Vector2 position; }`（位置はフィールドの中心を (0, 0) とするユニット）
  - `enum SkillId { FrontShot, DiagonalShot, RearShot, Pierce, Ricochet, Fire, Ice, AttackSpeed, Critical, AttackUp, MaxHpUp, Orbit }`（設計書 §9 の表の順）
  - `SkillDefinition : ScriptableObject` — `SkillId id`, `Sprite icon`, `int maxStacks`（アセットは計画2で作る）
  - `interface IHittable { void TakeHit(int damage); }`
  - `static class EnemyRegistry` — `IReadOnlyList<Transform> Active`, `void Register(Transform t)`, `void Unregister(Transform t)`, `void Clear()`。同じものを2回登録しない、登録していないものの解除は無視する。

50ウェーブの構成データ（どのウェーブでどの `WaveDefinition` を使うか）は計画3で作る。計画1では `RunController` がテスト用のウェーブを順に繰り返す（タスク10）。

- [ ] **Step 1: 失敗するテストを書く**

```csharp
public class DataFormatTests
{
    [Test]
    public void PlayerStats_MoveFireRateDefaultsToFull()
    {
        var stats = ScriptableObject.CreateInstance<PlayerStats>();
        Assert.AreEqual(1f, stats.moveFireRateMultiplier);
        Object.DestroyImmediate(stats);
    }

    [Test]
    public void SkillId_HasTwelveSkills() =>
        Assert.AreEqual(12, System.Enum.GetValues(typeof(SkillId)).Length);

    [Test]
    public void WaveDefinition_DefaultsToTelegraph08()
    {
        var wave = ScriptableObject.CreateInstance<WaveDefinition>();
        Assert.AreEqual(0.8f, wave.telegraphSeconds);
        Object.DestroyImmediate(wave);
    }

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

### Task 3: 文字ゼロの仕組みとドット絵の取り込み設定（担当 C）

**Files:**
- Create: `Assets/_Project/Fonts/Digits.asset`（元のフォントファイルも `Assets/_Project/Fonts/` に置く）
- Create: `Assets/_Project/Scripts/Editor/PixelArtImportPostprocessor.cs`
- Modify: `Assets/TextMesh Pro/Resources/TMP Settings.asset`
- Modify: `docs/assets-list.md`
- Test: `Assets/_Project/Tests/EditMode/NoTextGuardTests.cs`, `PixelArtImportTests.cs`

**Interfaces:**
- Produces: フォントアセット `Assets/_Project/Fonts/Digits.asset`。登録する文字は `0123456789+-×/%` だけ。TMP Settings の既定フォントにする。ピクセルフォントなので、Render Mode はラスター（ヒント付き）、アトラスの Filter Mode は Point。
- Produces: ガードテスト。以後、全員のタスクがこのテストに通る必要がある。
- Produces: `PixelArtImportPostprocessor : AssetPostprocessor`（`PictoArcher.Editor`）— `Assets/_Project/Art/` の下の画像を取り込むたびに、Filter Mode = Point、圧縮なし、ミップマップなしにする。初めて取り込むとき（`assetImporter.importSettingsMissing`）だけ、Texture Type = Sprite、Pixels Per Unit = 16 にする（スプライトシートの切り分けを後から変えても消えないように）。【計画での判断】

★ や ⏱ などの記号は、UI担当が2週目にフォントの確認をして足す（世界観の文書 §5）。足すのは文字でない記号だけで、ガードテストがそれを確かめる。

- [ ] **Step 1: 失敗するテストを書く**

```csharp
public class NoTextGuardTests
{
    const string FontPath = "Assets/_Project/Fonts/Digits.asset";
    const string Required = "0123456789+-×/%";

    [Test]
    public void DigitFont_HasOnlyDigitsAndSymbols()
    {
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        Assert.IsNotNull(font);
        Assert.AreEqual(AtlasPopulationMode.Static, font.atlasPopulationMode);
        Assert.IsEmpty(font.fallbackFontAssetTable ?? new List<TMP_FontAsset>());
        foreach (var c in font.characterTable)
            Assert.IsFalse(char.IsLetter((char)c.unicode), $"letter glyph U+{c.unicode:X4}");
        foreach (var r in Required)
            Assert.IsTrue(font.characterLookupTable.ContainsKey(r), $"missing {r}");
        Assert.AreEqual(FilterMode.Point, font.atlasTexture.filterMode);
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
        Assert.IsTrue(" New Text".Any(char.IsLetter) && !" 23/50 ×2 +20%".Any(char.IsLetter));
}

public class PixelArtImportTests
{
    [Test]
    public void ArtImport_IsPointUncompressedSprite16Ppu()
    {
        const string dir = "Assets/_Project/Art/_ImportTest";
        const string path = dir + "/dot.png";
        Directory.CreateDirectory(dir);
        var tex = new Texture2D(2, 2);
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        try
        {
            AssetDatabase.ImportAsset(path);
            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            Assert.AreEqual(TextureImporterType.Sprite, imp.textureType);
            Assert.AreEqual(FilterMode.Point, imp.filterMode);
            Assert.AreEqual(TextureImporterCompression.Uncompressed, imp.textureCompression);
            Assert.AreEqual(16f, imp.spritePixelsPerUnit);
            Assert.IsFalse(imp.mipmapEnabled);
        }
        finally
        {
            AssetDatabase.DeleteAsset(dir);
        }
    }
}
```

- [ ] **Step 2: 実行して失敗を確認する** — Expected: `DigitFont_HasOnlyDigitsAndSymbols`、`TmpDefaultFont_IsDigitFont`、`ArtImport_IsPointUncompressedSprite16Ppu` が失敗

- [ ] **Step 3: `PixelArtImportPostprocessor` を実装する**（`OnPreprocessTexture` で、パスが `Assets/_Project/Art/` で始まるときだけ Interfaces のとおりに設定する）

- [ ] **Step 4: フォントを用意する**
  - 数字と `/` `+` `-` `×` `%` が入った CC0 のピクセルフォント（候補：Kenney Fonts）を選んで `Assets/_Project/Fonts/` に置き、`docs/assets-list.md` に記録する
  - Window → TextMeshPro → Import TMP Essential Resources
  - Window → TextMeshPro → Font Asset Creator：Sampling Point Size はフォントの元の大きさ、Character Set = Custom Characters、`0123456789+-×/%`、Render Mode = ラスター（ヒント付き）→ `Digits.asset` として保存し、Atlas Population Mode を Static、アトラスの Filter Mode を Point にする
  - TMP Settings：Default Font Asset = `Digits`、Fallback Font Assets を空にする
  - 元のフォントに `×` がないときは、チームに報告して UI担当の「フォントの確認」（2週目）で足してもらう。それまでは `Required` から `×` を外してよい（外したことをプルリクエストに書く）

- [ ] **Step 5: 実行して通ることを確認する** — Expected: すべて成功

- [ ] **Step 6: Commit** — `git commit -m "feat: digit-only pixel font, no-text guards, pixel art import settings"`

---

### Task 4: 移動ベクトルの計算（担当 A）

**Files:**
- Create: `Assets/_Project/Scripts/Core/MoveVectorResolver.cs`
- Test: `Assets/_Project/Tests/EditMode/MoveVectorResolverTests.cs`

**Interfaces:**
- Produces: `public static class MoveVectorResolver` — `public const float DragDeadZonePixels = 10f;` と `public static Vector2 Resolve(Vector2 keyboard, bool dragActive, Vector2 dragOrigin, Vector2 dragCurrent, float dragRadiusPixels)`。返す値の長さは常に 1 以下。

規則：キーボードの入力が 0 でなければキーボードを使う（長さが 1 を超えたら 1 に縮める）。そうでなければ、ドラッグ中のときに `dragCurrent - dragOrigin` を使う。長さが `DragDeadZonePixels` 未満なら 0。それ以上なら `/ dragRadiusPixels` して長さを 1 以下に縮める。

`Vector2` は `UnityEngine.Vector2`。Core は MonoBehaviour を使わないが、`UnityEngine` の値型は使ってよい。【計画での判断】

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
- Produces: `public sealed class FireTimer` — `FireTimer(float interval, float moveFireRateMultiplier)`, `float Interval { get; set; }`, `float MoveFireRateMultiplier { get; set; }`, `bool Tick(float deltaTime, bool hasTarget, bool isMoving)`。true のとき1発撃つ。

FireTimer の規則：
- 待ち時間をためていき、実際の間隔は `Interval / (isMoving ? MoveFireRateMultiplier : 1)`。
- 1回の `Tick` で撃つのは最大1発。撃ったら待ち時間を 0 に戻す（余りを持ち越さない）。
- 敵がいない間も待ち時間はたまるが、間隔の分で止まる。敵が出たらすぐ1発撃てる。
- 作った直後は撃てる状態。

計画2の ⚡（連射）は `Interval` を書き換えて使う。

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

計画2の ♥↑（最大HPアップ）のために `Max` を増やす方法が要るが、この計画では作らない。

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

### Task 7: ウェーブの進行（担当 C）

**Files:**
- Create: `Assets/_Project/Scripts/Core/WaveLayout.cs`, `WaveProgress.cs`, `WaveSequence.cs`
- Test: `Assets/_Project/Tests/EditMode/WaveLayoutTests.cs`, `WaveProgressTests.cs`, `WaveSequenceTests.cs`

**Interfaces:**
- Produces: `public enum WaveKind { Tutorial, Combat, MiniBoss, Boss, FinalBoss, Angel }`、`public static class WaveLayout { public const int TotalWaves = 50; public static WaveKind GetKind(int wave); }`
  - 番号は画面に出す番号のまま（1始まり）。0 はチュートリアル（設計書 §7 のウェーブ0）。
  - 5・15・25・35・45 は MiniBoss、10・20・30・40 は Boss、50 は FinalBoss、11・21・31・41 は Angel、0 は Tutorial、それ以外は Combat（設計書 §6）。範囲外（0 未満、50 より大きい）は `ArgumentOutOfRangeException`。
- Produces: `public sealed class WaveProgress` — 1ウェーブ分の敵を数える。`void RegisterEnemy(int enemyId)`, `void MarkKilled(int enemyId)`（登録していない番号と2回目は無視）, `void FinishSpawning()`, `int AliveCount { get; }`, `bool IsCleared { get; }`（出現が終わっていて、生きている敵が 0）, `event Action Cleared`（1回だけ）。ウェーブごとに新しく作る。
- Produces: `public enum WavePhase { NotStarted, Fighting, Resting, Completed, Stopped }`、`public sealed class WaveSequence`
  - `public const float DefaultRestSeconds = 1.5f;`
  - `WaveSequence(int startWave, float restSeconds)` — `startWave` は 0〜50。範囲外は `ArgumentOutOfRangeException`。好きなウェーブから始めるデバッグ操作（計画3）もこれを使う。
  - `int CurrentWave { get; }` — 画面に出す番号。全滅した瞬間に次の番号になり、休みの間はその番号を出す。【計画での判断：設計書 §6 の「休みの間に `23/50` が `24/50` に変わる」を、休みの始めに変えると決めた】
  - `WavePhase Phase { get; }`
  - `event Action<int> WaveStarted` — 敵を出し始めるときに、そのウェーブの番号を渡す。
  - `event Action Completed` — ウェーブ50を全滅させたとき1回だけ。
  - `void Begin()` — `NotStarted` のときだけ `Fighting` にして `WaveStarted(startWave)`。
  - `void NotifyCleared()` — `Fighting` のときだけ受け付ける。`CurrentWave == 50` なら `Completed` にして `Completed` を発生。そうでなければ `CurrentWave` を1増やして `Resting`。
  - `void Tick(float deltaTime)` — `Resting` のときだけ休みの時間をためる。`restSeconds` 以上になったら `Fighting` にして `WaveStarted(CurrentWave)`。余りは持ち越さない（1回の `Tick` で始まるウェーブは最大1つ）。
  - `void Stop()` — プレイヤーが死んだときに呼ぶ。`Completed` 以外なら `Stopped` にし、以後はどの呼び出しも何もしない。

- [ ] **Step 1: 失敗するテストを書く**

```csharp
public class WaveLayoutTests
{
    [Test] public void Layout_MatchesSpec()
    {
        Assert.AreEqual(50, WaveLayout.TotalWaves);
        Assert.AreEqual(WaveKind.Tutorial, WaveLayout.GetKind(0));
        foreach (var w in new[] { 5, 15, 25, 35, 45 }) Assert.AreEqual(WaveKind.MiniBoss, WaveLayout.GetKind(w), $"wave {w}");
        foreach (var w in new[] { 10, 20, 30, 40 }) Assert.AreEqual(WaveKind.Boss, WaveLayout.GetKind(w), $"wave {w}");
        foreach (var w in new[] { 11, 21, 31, 41 }) Assert.AreEqual(WaveKind.Angel, WaveLayout.GetKind(w), $"wave {w}");
        Assert.AreEqual(WaveKind.FinalBoss, WaveLayout.GetKind(50));
        Assert.AreEqual(36, Enumerable.Range(1, 50).Count(w => WaveLayout.GetKind(w) == WaveKind.Combat));
    }

    [Test] public void OutOfRange_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => WaveLayout.GetKind(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => WaveLayout.GetKind(51));
    }
}

public class WaveProgressTests
{
    [Test] public void NotClearedUntilSpawningFinished()
    {
        var p = new WaveProgress();
        p.RegisterEnemy(1);
        p.MarkKilled(1);
        Assert.IsFalse(p.IsCleared);
        p.FinishSpawning();
        Assert.IsTrue(p.IsCleared);
    }

    [Test] public void EmptyWave_ClearsWhenSpawningFinished()
    {
        var p = new WaveProgress();
        p.FinishSpawning();
        Assert.IsTrue(p.IsCleared);
    }

    // Review Focus 4
    [Test] public void DuplicateAndUnknownKills_AreIgnored()
    {
        var p = new WaveProgress();
        p.RegisterEnemy(1); p.RegisterEnemy(2); p.FinishSpawning();
        p.MarkKilled(1); p.MarkKilled(1); p.MarkKilled(99);
        Assert.AreEqual(1, p.AliveCount);
        Assert.IsFalse(p.IsCleared);
    }

    // Review Focus 4
    [Test] public void Cleared_FiresOnce()
    {
        var p = new WaveProgress();
        int count = 0;
        p.Cleared += () => count++;
        p.RegisterEnemy(1); p.FinishSpawning();
        p.MarkKilled(1); p.MarkKilled(1);
        Assert.AreEqual(1, count);
    }
}

public class WaveSequenceTests
{
    static (WaveSequence seq, List<int> started) Make(int start)
    {
        var seq = new WaveSequence(start, 1.5f);
        var started = new List<int>();
        seq.WaveStarted += started.Add;
        return (seq, started);
    }

    [Test] public void Begin_StartsFirstWave()
    {
        var (seq, started) = Make(1);
        seq.Begin();
        CollectionAssert.AreEqual(new[] { 1 }, started);
        Assert.AreEqual(WavePhase.Fighting, seq.Phase);
        Assert.AreEqual(1, seq.CurrentWave);
    }

    [Test] public void Clear_ShowsNextNumber_ThenStartsAfterRest()
    {
        var (seq, started) = Make(1);
        seq.Begin();
        seq.NotifyCleared();
        Assert.AreEqual(2, seq.CurrentWave);
        Assert.AreEqual(WavePhase.Resting, seq.Phase);
        seq.Tick(1.0f);
        CollectionAssert.AreEqual(new[] { 1 }, started);
        seq.Tick(0.5f);
        CollectionAssert.AreEqual(new[] { 1, 2 }, started);
        Assert.AreEqual(WavePhase.Fighting, seq.Phase);
    }

    // Review Focus 4
    [Test] public void DoubleClear_AdvancesOnce()
    {
        var (seq, started) = Make(1);
        seq.Begin();
        seq.NotifyCleared();
        seq.NotifyCleared();
        Assert.AreEqual(2, seq.CurrentWave);
        seq.Tick(1.5f);
        seq.Tick(1.5f);
        CollectionAssert.AreEqual(new[] { 1, 2 }, started);
    }

    // Review Focus 3
    [Test] public void LongFrameDuringRest_StartsOnlyOneWave()
    {
        var (seq, started) = Make(1);
        seq.Begin();
        seq.NotifyCleared();
        seq.Tick(100f);
        CollectionAssert.AreEqual(new[] { 1, 2 }, started);
        Assert.AreEqual(2, seq.CurrentWave);
    }

    [Test] public void LastWaveCleared_CompletesOnce()
    {
        var (seq, started) = Make(50);
        int completed = 0;
        seq.Completed += () => completed++;
        seq.Begin();
        seq.NotifyCleared();
        seq.NotifyCleared();
        seq.Tick(10f);
        Assert.AreEqual(1, completed);
        Assert.AreEqual(50, seq.CurrentWave);
        Assert.AreEqual(WavePhase.Completed, seq.Phase);
        CollectionAssert.AreEqual(new[] { 50 }, started);
    }

    // Review Focus 5
    [Test] public void StopThenClear_DoesNotAdvance()
    {
        var (seq, started) = Make(1);
        seq.Begin();
        seq.Stop();
        seq.NotifyCleared();
        seq.Tick(10f);
        Assert.AreEqual(1, seq.CurrentWave);
        Assert.AreEqual(WavePhase.Stopped, seq.Phase);
        CollectionAssert.AreEqual(new[] { 1 }, started);
    }

    // Review Focus 5
    [Test] public void ClearThenStop_NextWaveNeverStarts()
    {
        var (seq, started) = Make(1);
        seq.Begin();
        seq.NotifyCleared();
        seq.Stop();
        seq.Tick(10f);
        Assert.AreEqual(WavePhase.Stopped, seq.Phase);
        CollectionAssert.AreEqual(new[] { 1 }, started);
    }

    [Test] public void StartWaveOutOfRange_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new WaveSequence(-1, 1.5f));
        Assert.Throws<ArgumentOutOfRangeException>(() => new WaveSequence(51, 1.5f));
    }
}
```

- [ ] **Step 2: 実行して失敗を確認する** — Expected: コンパイルエラー
- [ ] **Step 3: Interfaces どおりに実装する**（`WaveProgress` は生きている敵の番号を `HashSet<int>` で持つ）
- [ ] **Step 4: 実行して通ることを確認する** — Expected: 14件成功
- [ ] **Step 5: Commit** — `git commit -m "feat: wave layout, wave clear and wave sequence rules"`

---

### Task 8: プレイヤーと弾（担当 A）

> **注（2026-11-14）：** このタスクは `docs/superpowers/specs/2026-11-14-coding-rules-design.md` の規則に読み替えて作る。下のコードの `Update`・`OnTriggerEnter2D`・static の `EnemyRegistry`・public のフィールドは、そのまま写さない。シーンの Tick から呼ばれる System と View、`CollisionRelay2D`、`RunScene` が持つ `EnemyRegistry`、SO のプロパティを使う。書く前にスキル `pictoarcher-coding-rules` を読む。

MonoBehaviour のつなぎ込みなので、確認は自分の試し用シーン `Sandbox_A.unity` での手動確認にする。ルールはタスク4・5・6でテスト済み。プレイヤーの絵は2週目に届く（世界観の文書 §4）ので、それまでは 24px の白い四角などの仮の図形で作る（設計書 §11）。

**Files:**
- Create: `Assets/_Project/Scripts/Game/Player/PlayerController.cs`, `PlayerShooter.cs`, `PlayerHealth.cs`, `Projectile.cs`
- Create: `Assets/_Project/Prefabs/Player.prefab`, `Projectile.prefab`
- Create: `Assets/_Project/Scenes/Sandbox/Sandbox_A.unity`

**Interfaces:**
- Consumes: `PlayerStats`、`IHittable`、`EnemyRegistry`（タスク2）、`MoveVectorResolver`（タスク4）、`TargetSelector`・`FireTimer`（タスク5）、`Health`・`HitInvulnerability`（タスク6）、`MainCamera.prefab`・レイヤー（タスク1）
- Produces:
  - `PlayerController : MonoBehaviour` — `[SerializeField] PlayerStats stats`, `[SerializeField] InputActionReference move`（`Player/Move`）, `bool IsMoving { get; }`, `void Teleport(Vector2 position)`。移動の向きで左右の反転（`SpriteRenderer.flipX`）を切り替える。
  - `PlayerShooter : MonoBehaviour` — `[SerializeField] Projectile projectilePrefab`
  - `PlayerHealth : MonoBehaviour, IHittable` — `Health Health { get; }`, `event Action Died`
  - `Projectile : MonoBehaviour` — `void Launch(Vector2 direction, float speed, int damage, float lifetime)`
- Prefab：`Player` はタグ `Player`、レイヤー `Player`、`Rigidbody2D`（Dynamic、Gravity Scale 0、回転固定、Interpolate）と `CircleCollider2D`。`Projectile` はレイヤー `PlayerShot`、白い小さな弾（4px 前後）、`Rigidbody2D`（Kinematic）と Trigger の `Collider2D`。

- [ ] **Step 1: `PlayerController` を実装する**。毎フレーム、`move` の値、`Mouse.current.leftButton`（押した瞬間の位置をドラッグの中心にする）と `Mouse.current.position` を `MoveVectorResolver.Resolve` に渡し、`FixedUpdate` で `Rigidbody2D.linearVelocity = v * stats.moveSpeed`。`IsMoving` は v が 0 でないとき true。
- [ ] **Step 2: `PlayerShooter` を実装する**。`EnemyRegistry.Active` の位置を `TargetSelector.FindNearest` に渡し、`FireTimer.Tick(Time.deltaTime, 敵がいる, controller.IsMoving)` が true なら、その敵の方向へ `Projectile` を出す。
- [ ] **Step 3: `Projectile` を実装する**。`Launch` の方向へ進み、`lifetime` 秒で消える。`OnTriggerEnter2D` で相手に `IHittable` があれば `TakeHit(damage)` して消える。レイヤー `Wall`（壁と障害物）に当たっても消える。1発が2体に当たらないよう、最初に当たった時点で以後の当たりを無視する。
- [ ] **Step 4: `PlayerHealth` を実装する**。`TakeHit` は `HitInvulnerability.TryHit(Time.time)` が true のときだけ `Health.ApplyDamage`。無敵中はスプライトを点滅させる。`Health.Died` を `Died` として外に出す。
- [ ] **Step 5: プレハブを作り、`Sandbox_A` で確認する**。シーンに `MainCamera.prefab`、Player、壁と柱（レイヤー `Wall` の四角でよい）、`IHittable` を持つ的の代わりとして四角を2つ置き（`EnemyRegistry.Register` する簡単な試し用スクリプトは Sandbox 内にだけ置く）、再生する。
  Expected:
  - WASD・矢印キーで動く。斜めでも速くならない
  - マウスを押したままドラッグすると、押した位置からの方向へ動く。キーボードと同時ならキーボードが勝つ
  - 左へ動くと左を向く
  - 止まっていても動いていても、近い的に向かって 0.5 秒ごとに白い弾が飛ぶ
  - 的がないときは撃たない
  - 弾が壁と柱で消える
  - 動いても絵がにじまず、ドット単位で動く
  - 画面に文字が出ていない
- [ ] **Step 6: テストを実行する** — Run: `tools/run-editmode-tests.sh` / Expected: すべて成功（特に `SerializedTexts_ContainNoLetters`）
- [ ] **Step 7: Commit** — `git commit -m "feat: player movement, auto fire and projectiles"`

---

### Task 9: 追う敵と出現予兆（担当 B）

> **注（2026-11-14）：** このタスクは `docs/superpowers/specs/2026-11-14-coding-rules-design.md` の規則に読み替えて作る。下のコードの `Update`・`OnTriggerEnter2D`・static の `EnemyRegistry`・public のフィールドは、そのまま写さない。シーンの Tick から呼ばれる System と View、`CollisionRelay2D`、`RunScene` が持つ `EnemyRegistry`、SO のプロパティを使う。書く前にスキル `pictoarcher-coding-rules` を読む。

追う敵の絵は3週目に届く（世界観の文書 §4）ので、それまでは 24px の灰色の丸などの仮の図形で作る。色の意味の5色は使わない（設計書 §5）。

**Files:**
- Create: `Assets/_Project/Scripts/Game/Enemies/EnemyActor.cs`, `ChaseMovement.cs`, `SpawnTelegraph.cs`
- Create: `Assets/_Project/Prefabs/Chaser.prefab`, `SpawnTelegraph.prefab`
- Create: `Assets/_Project/Data/Enemies/Chaser.asset`
- Create: `Assets/_Project/Scenes/Sandbox/Sandbox_B.unity`
- Modify: `docs/assets-list.md`

**Interfaces:**
- Consumes: `EnemyDefinition`、`IHittable`、`EnemyRegistry`（タスク2）、`Health`（タスク6）、`MainCamera.prefab`・レイヤー（タスク1）、ドット絵の取り込み設定（タスク3）
- Produces:
  - `EnemyActor : MonoBehaviour, IHittable` — `void Init(EnemyDefinition def, int enemyId, Transform target)`, `int EnemyId { get; }`, `EnemyDefinition Definition { get; }`, `Transform Target { get; }`, `event Action<EnemyActor> Died`。`Init` で `EnemyRegistry.Register`、死亡したら `Died` を発生させて `Unregister` し、自分を消す。当たったら一瞬白く光る（計画1は色を白にするだけ。シェーダーは計画4）。
  - `ChaseMovement : MonoBehaviour` — `EnemyActor.Target` へ `Definition.moveSpeed` で進む。進む向きで左右を反転する。`OnCollisionStay2D` で相手がタグ `Player` で `IHittable` なら `TakeHit(Definition.contactDamage)`（連続して当たらないのはプレイヤー側の無敵時間で防ぐ）。障害物を避ける道探しはしない（ぶつかったら物理で滑る）。【計画での判断】
  - `SpawnTelegraph : MonoBehaviour` — `void Play(Vector2 position, float seconds, Action onFinished)`。赤い円と `!` のスプライトを `seconds` 秒表示（点滅しながら少し大きくなる）してから `onFinished` を呼び、自分を消す。
- `Chaser.asset`：`maxHp = 30`, `moveSpeed = 2.5`（約40px/秒）, `contactDamage = 10`, `xpReward = 1`, `coinReward = 1`（計画での判断、調整値）
- Prefab：`Chaser` はレイヤー `Enemy`、`Rigidbody2D`（Dynamic、Gravity Scale 0、回転固定）、`CircleCollider2D`

- [ ] **Step 1: `EnemyActor` を実装する**（`Health` を `def.maxHp` で作り、`TakeHit` → `ApplyDamage`、`Health.Died` → 上の死亡処理）
- [ ] **Step 2: `ChaseMovement` を実装する**
- [ ] **Step 3: `SpawnTelegraph` を実装する**。赤い円と `!` は 16px 前後のドット絵にする。CC0 素材から選ぶか、仮の図形で作り、素材を使ったら `docs/assets-list.md` に記録する
- [ ] **Step 4: `Sandbox_B` で確認する**。`MainCamera.prefab`、Player プレハブ（タスク8が未完成なら、タグ `Player` の四角を矢印キーで動かす試し用スクリプトを Sandbox 内にだけ置く）と、`SpawnTelegraph.Play` の後に Chaser を出す試し用スクリプトを置いて再生する。
  Expected:
  - 赤い円と `!` が 0.8 秒出て、その場所に追う敵が出る
  - 追う敵がプレイヤーを追ってくる。左右の向きが変わる
  - 触れると HP が 10 減り、0.6 秒は続けて減らない
  - 弾を3発当てると（弾のダメージ10、HP30）消える。同じフレームに2発当たっても `Died` は1回
  - 敵の体の色が赤・緑・黄・青・紫になっていない
  - 画面に文字が出ていない
- [ ] **Step 5: テストを実行する** — Expected: すべて成功
- [ ] **Step 6: Commit** — `git commit -m "feat: chaser enemy and spawn telegraph"`

---

### Task 10: フィールド・ウェーブの流れ・HUD（担当 C）

> **注（2026-11-14）：** このタスクは `docs/superpowers/specs/2026-11-14-coding-rules-design.md` の規則に読み替えて作る。下のコードの `Update`・`OnTriggerEnter2D`・static の `EnemyRegistry`・public のフィールドは、そのまま写さない。シーンの Tick から呼ばれる System と View、`CollisionRelay2D`、`RunScene` が持つ `EnemyRegistry`、SO のプロパティを使う。書く前にスキル `pictoarcher-coding-rules` を読む。

`Run.unity` を編集するのはこのタスクだけ。A と B はプレハブを渡す。

**Files:**
- Create: `Assets/_Project/Scripts/Game/Run/WaveSpawner.cs`, `RunController.cs`
- Create: `Assets/_Project/Scripts/Game/UI/HudView.cs`, `HeadHpView.cs`
- Create: `Assets/_Project/Prefabs/Field_Test.prefab`
- Create: `Assets/_Project/Data/Waves/Wave_Test_A.asset`（追う敵3体）、`Wave_Test_B.asset`（追う敵5体）
- Modify: `Assets/_Project/Scenes/Run.unity`
- Modify: `docs/assets-list.md`

**Interfaces:**
- Consumes: `WaveDefinition`・`EnemyRegistry`（タスク2）、`Digits.asset`（タスク3）、`WaveProgress`・`WaveSequence`・`WaveLayout`（タスク7）、`PlayerController.Teleport`・`PlayerHealth.Died`・`PlayerHealth.Health`（タスク8）、`EnemyActor.Init`・`EnemyActor.Died`・`SpawnTelegraph.Play`（タスク9）、`MainCamera.prefab`（タスク1）
- Produces:
  - `Field_Test.prefab` — Tilemap（16px のマス）で床と壁。壁はレイヤー `Wall`（`TilemapCollider2D` と `CompositeCollider2D`）。壁を含めて 480×270 に収め、壁の内側はおよそ 440×220。中に柱か箱を3〜4個置く（レイヤー `Wall`）。床と壁はフリー素材から選び、なければ仮の図形で作る。フィールドの中心を (0, 0) に置く。
  - `WaveSpawner : MonoBehaviour` — `[SerializeField] SpawnTelegraph telegraphPrefab`, `void Spawn(WaveDefinition def, Transform player, WaveProgress progress)`。すべての出現位置で同時に `SpawnTelegraph.Play(position, def.telegraphSeconds, …)` を出し、終わったものからその位置に敵を出して `EnemyActor.Init` し、`progress.RegisterEnemy`。`EnemyActor.Died` → `progress.MarkKilled`。全部の予兆が終わったら `progress.FinishSpawning()`。敵の番号は通し番号（ラン全体で重ならない）。
  - `RunController : MonoBehaviour` — `[SerializeField] List<WaveDefinition> testWaves`, `[SerializeField] int startWave = 1`（デバッグ用。インスペクターで変える）。開始時に `EnemyRegistry.Clear()` し、プレイヤーをフィールドの中心へ `Teleport`、`new WaveSequence(startWave, WaveSequence.DefaultRestSeconds)` を作って `Begin()`。`WaveStarted(n)` ごとに新しい `WaveProgress` を作り、`testWaves[(n - 1) % testWaves.Count]` を `WaveSpawner.Spawn` に渡す。`WaveProgress.Cleared` → `NotifyCleared()`。`Update` で `Tick(Time.deltaTime)`。`PlayerHealth.Died` → `Stop()` してシーンを読み直す。`Completed` でもシーンを読み直す（クリア画面と死亡画面は計画3）。計画1では、ウェーブの種類（`WaveLayout.GetKind`）にかかわらず全ウェーブでテスト用のウェーブを出す。
  - `HudView : MonoBehaviour` — `void Bind(WaveSequence sequence)`。画面上部の中央にウェーブ `n/50`（`CurrentWave` と `WaveLayout.TotalWaves`、`Digits` フォント）。番号が変わったときだけ文字列を作り直す。Canvas は Screen Space - Camera（`MainCamera`）、Canvas Scaler は Scale With Screen Size（参照 480×270）。【計画での判断】
  - `HeadHpView : MonoBehaviour` — `void Bind(Health health, Transform follow)`。プレイヤーの頭上に HP バーと数字（`Current`）。ワールド上の `SpriteRenderer` 2枚と `TextMeshPro`（UGUI ではない方）で作り、`follow` に追従させる。バーの色は緑（回復の色ではなく HP なので、色はチームで見て決め直してよい）。

`n/50` の位置と大きさは、UI担当の HUD（5週目）が届いたら差し替える。

- [ ] **Step 1: `Field_Test.prefab` を作る**。床と壁の素材を使ったら `docs/assets-list.md` に記録し、文字が入っていないか確かめる
- [ ] **Step 2: `WaveSpawner` を実装する**
- [ ] **Step 3: `RunController` を実装する**
- [ ] **Step 4: `HudView` と `HeadHpView` を実装する**。数字の表示には TextMeshPro を使い、TMP コンポーネントを置いたら最初に入っている「New Text」を消す（`SerializedTexts_ContainNoLetters` で落ちる）
- [ ] **Step 5: `Wave_Test_A.asset` と `Wave_Test_B.asset` を作る**（出現位置は壁の内側で、フィールドの中心から 4 ユニット以上離す。プレイヤーの真上に出ないようにするため）【計画での判断】
- [ ] **Step 6: `Run.unity` を組み立て、再生して確認する**（`MainCamera.prefab`、`Field_Test`、Player、`RunController`、`WaveSpawner`、HUD）
  Expected:
  - 始まるとすぐ赤い `!` 円が出て、追う敵3体が出る。画面上部に `1/50`
  - 全部倒すと `2/50` に変わり、約1.5秒あとに `!` 円が出て追う敵5体が出る
  - 休みの間は弾を撃たない
  - 3ウェーブ以上続けて遊べる。ウェーブごとに `n/50` が1ずつ増える
  - 最後の1体に2発同時に当てても、ウェーブは1つだけ進む
  - HP が 0 になるとシーンが最初からになる（休みの間に死んでも、次のウェーブは始まらない）
  - `startWave` を 49 にして再生すると `49/50` から始まり、`50/50` を全滅させるとシーンが最初からになる
  - ウィンドウを 1920×1080、4:3、21:9 に変えても、フィールド全体（壁まで）が見え、外側は黒帯になる
  - 画面のどこにも文字が出ていない
- [ ] **Step 7: テストを実行する** — Expected: すべて成功
- [ ] **Step 8: Commit** — `git commit -m "feat: field, wave flow and HUD"`

---

## 計画1の完了条件

- `tools/run-editmode-tests.sh` がすべて成功する
- `Run.unity` で上のタスク10 Step 6 の確認項目がすべて満たされる
- 3人それぞれがこの状態を自分の PC（Windows と Mac）で再生して確認した
- 「担当と順番」の表の「実績」を3人が書き込み、見積もりと比べた（ロードマップ「見積もりと実際の日数を比べる」）

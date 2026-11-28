using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using PictoArcher.Core;
using PictoArcher.Game;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace PictoArcher.Tests.PlayMode
{
    public class WaveFlowPlayModeTests
    {
        const float Step = 0.1f;

        sealed class FakePlayer : ITarget, IHittable, IPlayerStatus
        {
            public Vector2 Position { get; set; } = new Vector2(50f, 0f);
            public bool IsAlive => !IsDead;
            public bool IsDead { get; set; }
            public int Hp => 100;
            public int MaxHp => 100;
            public bool IsInvulnerable => false;
            public void TakeHit(Damage damage) { }
        }

        sealed class Rig
        {
            public readonly EnemyRegistry Registry = new EnemyRegistry();
            public readonly FakePlayer Player = new FakePlayer();
            public readonly List<IEnemyTarget> Spawned = new List<IEnemyTarget>();
            public EnemySystem Enemies;
            public WaveSpawnSystem Waves;
            public RunSystem Run;
            RunDefinition _definition;
            readonly bool _ownsDefinition;

            // definition を渡すとそのアセットで回す（渡さなければ試しの2ウェーブ）
            public Rig(int startWave, RunDefinition definition = null)
            {
                _ownsDefinition = definition == null;
                _definition = _ownsDefinition ? NewTestDefinition(startWave) : definition;

                var telegraph = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Enemies/SpawnTelegraph.prefab").GetComponent<SpawnTelegraphView>();
                var settings = AssetDatabase.LoadAssetAtPath<SpawnTelegraphSettings>("Assets/_Project/Data/Enemies/SpawnTelegraphSettings.asset");
                var attack = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Enemies/AttackTelegraph.prefab").GetComponent<AttackTelegraphView>();
                Enemies = new EnemySystem(Registry, telegraph, settings, attack, new EnemyProjectileSystem(Player), Player, Player);
                Enemies.Spawned += e => Spawned.Add(e);
                Waves = new WaveSpawnSystem(Enemies);
                Run = new RunSystem(_definition, Waves, Player);
                Run.Begin();
            }

            static RunDefinition NewTestDefinition(int startWave)
            {
                var definition = ScriptableObject.CreateInstance<RunDefinition>();
                var so = new SerializedObject(definition);
                var waves = so.FindProperty("_waves");
                waves.arraySize = 2;
                waves.GetArrayElementAtIndex(0).objectReferenceValue = AssetDatabase.LoadAssetAtPath<WaveDefinition>("Assets/_Project/Data/Waves/Wave_Test_A.asset");
                waves.GetArrayElementAtIndex(1).objectReferenceValue = AssetDatabase.LoadAssetAtPath<WaveDefinition>("Assets/_Project/Data/Waves/Wave_Test_B.asset");
                so.FindProperty("_startWave").intValue = startWave;
                so.FindProperty("_restSeconds").floatValue = 1.5f;
                so.FindProperty("_endDelaySeconds").floatValue = 0.5f;
                so.ApplyModifiedPropertiesWithoutUndo();
                return definition;
            }

            // BattleScene と同じ順に回す
            public void Tick(float seconds)
            {
                for (float t = 0f; t < seconds - 0.0001f; t += Step)
                {
                    Enemies.Tick(Step);
                    Waves.Tick(Step);
                    Run.Tick(Step);
                }
            }

            public void KillAll(int damage = 1000)
            {
                var alive = new List<IEnemyTarget>(Registry.Active);
                for (int i = 0; i < alive.Count; i++) alive[i].TakeHit(new Damage(damage));
                // 最後の1体に2発同時に当たっても、ウェーブは1つだけ進む
                if (alive.Count > 0) alive[alive.Count - 1].TakeHit(new Damage(damage));
            }

            public void Dispose()
            {
                Run.Dispose();
                Waves.Dispose();
                Enemies.Dispose();
                Registry.Clear();
                if (_ownsDefinition) Object.DestroyImmediate(_definition);
            }
        }

        bool _battleLoaded;

        [UnityTest]
        public IEnumerator Waves_SpawnAfterTelegraph_AdvanceOneAtATime_AndRestBeforeNext()
        {
            var rig = new Rig(1);
            Assert.AreEqual(1, rig.Run.CurrentWave);
            Assert.AreEqual(3, rig.Enemies.PendingCount, "始まるとすぐ予兆が3つ出る");
            rig.Tick(1f);
            Assert.AreEqual(3, rig.Spawned.Count, "予兆のあとに3体出る");

            rig.KillAll();
            rig.Tick(Step);
            Assert.AreEqual(2, rig.Run.CurrentWave, "全滅すると 2/50 になる");

            rig.Tick(1f);
            Assert.AreEqual(0, rig.Enemies.PendingCount, "休みの間は出さない");
            rig.Tick(0.6f);
            Assert.AreEqual(5, rig.Enemies.PendingCount, "約1.5秒あとに5つの予兆");
            rig.Tick(1f);
            Assert.AreEqual(8, rig.Spawned.Count);

            rig.KillAll();
            rig.Tick(1.6f);
            Assert.AreEqual(3, rig.Run.CurrentWave, "3ウェーブ目へ（1つずつ進む）");
            rig.Tick(1f);
            Assert.AreEqual(11, rig.Spawned.Count, "3ウェーブ目は最初のウェーブを使い回す");
            Assert.IsFalse(rig.Run.IsOver);
            rig.Dispose();
            yield return null;
        }

        [UnityTest]
        public IEnumerator WaveCleared_RaisesRestStartedWithTheNextWave()
        {
            var rig = new Rig(1);
            var rests = new List<int>();
            rig.Run.RestStarted += n => rests.Add(n);
            rig.Tick(1f);
            rig.KillAll();
            rig.Tick(Step);
            CollectionAssert.AreEqual(new[] { 2 }, rests, "休みに入ると次のウェーブの番号が1回だけ届く");
            rig.Tick(1.6f);
            CollectionAssert.AreEqual(new[] { 2 }, rests, "休みが終わっても増えない");
            rig.Dispose();
            yield return null;
        }

        [UnityTest]
        public IEnumerator ClearingWave50_DoesNotRaiseRestStarted()
        {
            var rig = new Rig(50);
            int rests = 0;
            rig.Run.RestStarted += n => rests++;
            rig.Tick(1f);
            rig.KillAll();
            rig.Tick(Step);
            Assert.AreEqual(0, rests, "最後のウェーブのあとは休まない");
            rig.Dispose();
            yield return null;
        }

        [UnityTest]
        public IEnumerator PlayerDeathDuringRest_StopsTheNextWave_AndEndsAfterDelay()
        {
            var rig = new Rig(1);
            rig.Tick(1f);
            rig.KillAll();
            rig.Tick(Step);
            rig.Player.IsDead = true;
            rig.Tick(Step);
            Assert.IsFalse(rig.Run.IsOver, "少し待ってから終わる");
            rig.Tick(3f);

            Assert.AreEqual(0, rig.Enemies.PendingCount, "死んだあとは次のウェーブを始めない");
            Assert.AreEqual(3, rig.Spawned.Count);
            Assert.IsTrue(rig.Run.IsOver);
            Assert.IsFalse(rig.Run.IsCleared);
            Assert.AreEqual(1f + Step, rig.Run.ElapsedSeconds, 0.0001f, "死んだあとの時間は数えない");
            rig.Dispose();
            yield return null;
        }

        [UnityTest]
        public IEnumerator ClearingWave50_EndsAsCleared()
        {
            var rig = new Rig(50);
            Assert.AreEqual(50, rig.Run.CurrentWave);
            rig.Tick(1f);
            rig.KillAll();
            rig.Tick(Step);
            Assert.IsTrue(rig.Run.IsCleared, "50/50 を全滅させるとクリア");
            rig.Tick(1f);
            Assert.IsTrue(rig.Run.IsOver);
            Assert.AreEqual(50, rig.Run.CurrentWave);
            Assert.AreEqual(1f, rig.Run.ElapsedSeconds, 0.0001f, "最後の敵を倒した所で止まり、そのあとの時間は数えない");
            rig.Dispose();
            yield return null;
        }

        [UnityTest]
        public IEnumerator RealRun_AllFiftyWavesSpawnAndClear_InOrder()
        {
            var run = AssetDatabase.LoadAssetAtPath<RunDefinition>("Assets/_Project/Data/Waves/RunDefinition.asset");
            var rig = new Rig(1, run);
            for (int n = 1; n <= 50; n++)
            {
                Assert.AreEqual(n, rig.Run.CurrentWave);
                int before = rig.Spawned.Count;
                rig.Tick(1f);   // 予兆が終わって敵が出る
                Assert.GreaterOrEqual(rig.Spawned.Count - before, 1, "ウェーブ " + n + " の敵が出る");
                // 分裂する中ボスは子を出すので、いなくなるまで倒し続ける
                for (int i = 0; i < 20 && rig.Registry.Active.Count > 0; i++)
                {
                    rig.KillAll(100000);
                    rig.Tick(Step);
                }
                if (n < 50) rig.Tick(1.6f);   // 休み
            }
            rig.Tick(1f);
            Assert.IsTrue(rig.Run.IsCleared, "50 ウェーブを全部クリアした");
            rig.Dispose();
            yield return null;
        }

        [UnityTest]
        public IEnumerator WaveTier_IsPassedToTheSpawnedEnemies()
        {
            var wave = ScriptableObject.CreateInstance<WaveDefinition>();
            var so = new SerializedObject(wave);
            var spawns = so.FindProperty("_spawns");
            spawns.arraySize = 1;
            spawns.GetArrayElementAtIndex(0).FindPropertyRelative("_enemy").objectReferenceValue = AssetDatabase.LoadAssetAtPath<EnemyDefinition>("Assets/_Project/Data/Enemies/Splitter.asset");
            so.FindProperty("_telegraphSeconds").floatValue = 0.3f;
            so.FindProperty("_tier").intValue = 2;
            so.ApplyModifiedPropertiesWithoutUndo();

            var rig = new Rig(1);
            var spawner = new WaveSpawnSystem(rig.Enemies);
            spawner.Begin(wave);
            for (int i = 0; i < 10; i++) { rig.Enemies.Tick(Step); spawner.Tick(Step); }
            var boss = rig.Spawned.Find(e => ((EnemyUnit)e).Definition.name == "Splitter");
            Assert.IsNotNull(boss);
            Assert.AreEqual(2, ((EnemyUnit)boss).Tier);

            spawner.Dispose();
            rig.Dispose();
            Object.DestroyImmediate(wave);
            yield return null;
        }

        [UnityTest]
        public IEnumerator SplitterWave_ClearsOnlyAfterChildrenDie_EvenWhenParentDiesBeforeSplitting()
        {
            var wave = ScriptableObject.CreateInstance<WaveDefinition>();
            var so = new SerializedObject(wave);
            var spawns = so.FindProperty("_spawns");
            spawns.arraySize = 1;
            spawns.GetArrayElementAtIndex(0).FindPropertyRelative("_enemy").objectReferenceValue = AssetDatabase.LoadAssetAtPath<EnemyDefinition>("Assets/_Project/Data/Enemies/Splitter.asset");
            spawns.GetArrayElementAtIndex(0).FindPropertyRelative("_position").vector2Value = Vector2.zero;
            so.FindProperty("_telegraphSeconds").floatValue = 0.3f;
            so.ApplyModifiedPropertiesWithoutUndo();

            var rig = new Rig(1);
            rig.Tick(1f);
            rig.KillAll();
            rig.Tick(1.6f);   // 2ウェーブ目の予兆が出ている（使わない）
            var spawner = new WaveSpawnSystem(rig.Enemies);
            var spawned = new List<IEnemyTarget>();
            rig.Enemies.Spawned += e => spawned.Add(e);
            int cleared = 0;
            spawner.Cleared += () => cleared++;
            spawner.Begin(wave);
            for (int i = 0; i < 10; i++) { rig.Enemies.Tick(Step); spawner.Tick(Step); }
            var boss = spawned.Find(e => e.Position == Vector2.zero);
            Assert.IsNotNull(boss, "中ボスが出る");

            boss.TakeHit(new Damage(110));    // 分裂を頼む（子はまだ出ていない）
            boss.TakeHit(new Damage(1000));   // 次の Tick より前に本体が死ぬ
            for (int i = 0; i < 3; i++) { rig.Enemies.Tick(Step); spawner.Tick(Step); }
            Assert.AreEqual(0, cleared, "子が生きている間はクリアにならない");

            foreach (var e in spawned.FindAll(e => e.IsAlive)) e.TakeHit(new Damage(10000));
            rig.Enemies.Tick(Step);
            spawner.Tick(Step);
            Assert.AreEqual(1, cleared, "子も全部倒すとクリア");

            spawner.Dispose();
            rig.Dispose();
            Object.DestroyImmediate(wave);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Battle_ShowsWaveAndHeadHp_AndSpawnsTheFirstWave()
        {
            _battleLoaded = true;
            SceneManager.LoadScene(SceneTransform.RootSceneName);
            yield return null;   // 前のテストの Home が残っていても、読み込み直しを待つ
            for (int i = 0; i < 300 && SceneManager.GetActiveScene().name != "Home"; i++) yield return null;
            var home = Object.FindAnyObjectByType<HomeScene>();
            var start = (UiButtonRelay)typeof(HomeScene).GetField("_startButton", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(home);
            start.GetComponent<Button>().onClick.Invoke();
            for (int i = 0; i < 300 && SceneManager.GetActiveScene().name != "Battle"; i++) yield return null;
            Assert.AreEqual("Battle", SceneManager.GetActiveScene().name);
            yield return new WaitForSeconds(1.3f);

            Assert.AreEqual(1, Object.FindObjectsByType<PlayerView>(FindObjectsSortMode.None).Length, "プレイヤーが1人");
            Assert.AreEqual(1, Object.FindObjectsByType<HeadHpView>(FindObjectsSortMode.None).Length, "頭上の HP");
            Assert.GreaterOrEqual(Object.FindObjectsByType<EnemyView>(FindObjectsSortMode.None).Length, 1, "最初のウェーブの敵が出る");
            Assert.AreEqual(4, Object.FindObjectsByType<ObstacleView>(FindObjectsSortMode.None).Length, "1ウェーブ目の障害物（四隅）が出る");
            var hud = Object.FindAnyObjectByType<HudView>();
            // テストの asmdef は TextMeshPro を参照しないので、文字はリフレクションで読む
            var wave = (Component)typeof(HudView).GetField("_wave", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(hud);
            Assert.AreEqual("1/50", (string)wave.GetType().GetProperty("text").GetValue(wave));
        }

        [UnityTearDown]
        public IEnumerator CleanUp()
        {
            if (!_battleLoaded) yield break;
            _battleLoaded = false;
            SceneManager.LoadScene(SceneTransform.RootSceneName);
            yield return null;
            yield return null;
        }
    }
}

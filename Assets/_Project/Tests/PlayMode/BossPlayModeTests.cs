using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using PictoArcher.Core;
using PictoArcher.Game;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace PictoArcher.Tests.PlayMode
{
    /// <summary>ボスA（ウェーブ10）が予兆つきで雑魚を呼び、ボスの HP バーの値（IBossStatus）が HP を写すか。</summary>
    public class BossPlayModeTests
    {
        const string DataPath = "Assets/_Project/Data/Enemies/";
        const float Step = 0.05f;

        sealed class FakePlayer : ITarget, IHittable
        {
            public Vector2 Position { get; set; }
            public bool IsAlive => true;
            public void TakeHit(Damage damage) { }
        }

        EnemySystem _system;
        EnemyProjectileSystem _projectiles;
        readonly List<IEnemyTarget> _spawned = new List<IEnemyTarget>();
        readonly List<string> _spawnedNames = new List<string>();

        [SetUp]
        public void SetUp()
        {
            var player = new FakePlayer { Position = new Vector2(0f, -8f) };
            _projectiles = new EnemyProjectileSystem(player);
            var spawn = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Enemies/SpawnTelegraph.prefab").GetComponent<SpawnTelegraphView>();
            var settings = AssetDatabase.LoadAssetAtPath<SpawnTelegraphSettings>(DataPath + "SpawnTelegraphSettings.asset");
            var attack = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Enemies/AttackTelegraph.prefab").GetComponent<AttackTelegraphView>();
            _system = new EnemySystem(new EnemyRegistry(), spawn, settings, attack, _projectiles, player, player);
            _spawned.Clear();
            _spawnedNames.Clear();
            _system.Spawned += HandleSpawned;
        }

        [TearDown]
        public void TearDown()
        {
            _system.Spawned -= HandleSpawned;
            _system.Dispose();
            _projectiles.Dispose();
        }

        void HandleSpawned(IEnemyTarget enemy)
        {
            _spawned.Add(enemy);
            _spawnedNames.Add(((EnemyUnit)enemy).Definition.name);
        }

        static EnemyDefinition Load(string name) => AssetDatabase.LoadAssetAtPath<EnemyDefinition>(DataPath + name + ".asset");

        int Count(string name) => _spawnedNames.FindAll(n => n == name).Count;

        /// <summary>ボスA を出し、雑魚呼びの予兆が出るまで回す。出たボスを返す。</summary>
        IEnumerator SpawnBossUntilSummon(System.Action<IEnemyTarget> result)
        {
            _system.Spawn(Load("BossA"), new Vector2(0f, 6f), 0.1f);
            for (int i = 0; i < 300; i++)
            {
                _system.Tick(Step);
                yield return null;
                if (_spawned.Count > 0 && _system.PendingCount > 0) break;
            }
            result(_spawned.Count > 0 ? _spawned[0] : null);
        }

        [UnityTest]
        public IEnumerator BossA_SummonsChasersAfterTelegraph_AndBossStatusFollowsItsHp()
        {
            Assert.IsFalse(_system.HasBoss, "出る前はバーを出さない");
            IEnemyTarget boss = null;
            yield return SpawnBossUntilSummon(b => boss = b);
            Assert.IsNotNull(boss, "ボスが出る");
            Assert.IsTrue(_system.HasBoss);
            Assert.AreEqual(600, _system.BossMaxHp);
            Assert.AreEqual(600, _system.BossHp);
            var summon = (ChargeSummonBehaviorDefinition)Load("BossA").Behavior;
            Assert.AreEqual(summon.SummonCount, _system.PendingCount, "呼ぶ雑魚の数だけ予兆が出る");
            Assert.AreEqual(0, Count("Chaser"), "予兆の間はまだ出ない");

            for (float t = 0f; t < summon.SummonWindupSeconds + 0.2f; t += Step)
            {
                _system.Tick(Step);
                yield return null;
            }
            Assert.AreEqual(summon.SummonCount, Count("Chaser"), "予兆のあとに雑魚が出る");

            boss.TakeHit(new Damage(150));
            Assert.AreEqual(450, _system.BossHp);
            Assert.IsTrue(_system.HasBoss);
            boss.TakeHit(new Damage(10000));
            Assert.IsFalse(_system.HasBoss, "倒したらバーを隠す");
            Assert.AreEqual(0, _system.BossHp);
        }

        [UnityTest]
        public IEnumerator DeadBoss_CancelsMinionsStillInTelegraph()
        {
            IEnemyTarget boss = null;
            yield return SpawnBossUntilSummon(b => boss = b);
            Assert.Greater(_system.PendingCount, 0);

            boss.TakeHit(new Damage(10000));
            Assert.AreEqual(0, _system.PendingCount, "呼んだボスが死んだら、予兆の途中の雑魚は出さない");
            for (int i = 0; i < 40; i++)
            {
                _system.Tick(Step);
                yield return null;
            }
            Assert.AreEqual(0, Count("Chaser"));
            Assert.AreEqual(0, Object.FindObjectsByType<SpawnTelegraphView>(FindObjectsSortMode.None).Length, "予兆の絵も消える");
        }

        [Test]
        public void BossDefinitions_ShowTheBossBar_AndBossAGrowsEachAppearance()
        {
            var bossA = Load("BossA");
            Assert.IsTrue(bossA.ShowsBossBar);
            Assert.IsTrue(Load("FinalBoss").ShowsBossBar);
            Assert.IsFalse(Load("Splitter").ShowsBossBar, "中ボスはバーを出さない");
            Assert.IsInstanceOf<ChargeSummonBehaviorDefinition>(bossA.Behavior);
            Assert.AreSame(Load("Chaser"), ((ChargeSummonBehaviorDefinition)bossA.Behavior).SummonEnemy);
            Assert.Greater(bossA.HpGrowthPerTier, 0f, "ウェーブ 20・30・40 のボスA は強くなる");
        }
    }
}

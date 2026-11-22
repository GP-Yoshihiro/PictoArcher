using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using PictoArcher.Core;
using PictoArcher.Game;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace PictoArcher.Tests.PlayMode
{
    /// <summary>中ボス Splitter：⚠ の予兆、HP が減ると分裂、登場のたびに強くなる。</summary>
    public class SplitterPlayModeTests
    {
        const string DataPath = "Assets/_Project/Data/Enemies/";

        sealed class FakePlayer : ITarget, IHittable
        {
            public Vector2 Position { get; set; } = new Vector2(100f, 100f);   // 遠くに置いて、触れて邪魔されないようにする
            public bool IsAlive => true;
            public void TakeHit(Damage damage) { }
        }

        EnemySystem _system;
        EnemyRegistry _registry;
        readonly List<IEnemyTarget> _spawned = new List<IEnemyTarget>();
        int _died;

        [SetUp]
        public void SetUp()
        {
            _spawned.Clear();
            _died = 0;
            var player = new FakePlayer();
            _registry = new EnemyRegistry();
            var spawn = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Enemies/SpawnTelegraph.prefab").GetComponent<SpawnTelegraphView>();
            var settings = AssetDatabase.LoadAssetAtPath<SpawnTelegraphSettings>(DataPath + "SpawnTelegraphSettings.asset");
            var attack = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Enemies/AttackTelegraph.prefab").GetComponent<AttackTelegraphView>();
            _system = new EnemySystem(_registry, spawn, settings, attack, new EnemyProjectileSystem(player), player, player);
            _system.Spawned += e => _spawned.Add(e);
            _system.Died += _ => _died++;
        }

        [TearDown]
        public void TearDown() => _system.Dispose();

        static EnemyDefinition Load(string name) => AssetDatabase.LoadAssetAtPath<EnemyDefinition>(DataPath + name + ".asset");

        IEnemyTarget SpawnNow(string name, int tier = 0)
        {
            _system.Spawn(Load(name), Vector2.zero, 0.1f, tier);
            _system.Tick(0.2f);
            return _spawned.Last();
        }

        [UnityTest]
        public IEnumerator Splitter_UsesBigWarning_BeforeAppearing()
        {
            _system.Spawn(Load("Splitter"), Vector2.zero, 0.5f);
            _system.Tick(0.1f);
            var views = Object.FindObjectsByType<SpawnTelegraphView>(FindObjectsSortMode.None);
            Assert.AreEqual(1, views.Length);
            Assert.AreEqual("MidBossWarning", views[0].name.Replace("(Clone)", ""), "中ボスは専用の大きい ⚠ の予兆");

            _system.Tick(0.5f);
            yield return null;   // Destroy は次のフレームで反映される
            Assert.AreEqual(0, Object.FindObjectsByType<SpawnTelegraphView>(FindObjectsSortMode.None).Length, "出たら予兆は消える");
            Assert.AreEqual(1, _spawned.Count);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Splitter_SplitsWhenHpDrops_AndEachThresholdOnlyOnce()
        {
            var boss = SpawnNow("Splitter");   // HP 300、しきい値 0.66 と 0.33、1回で2体
            Assert.AreEqual(1, _spawned.Count);

            boss.TakeHit(new Damage(110));   // 190/300 = 0.63 → 1つ目を切った
            Assert.AreEqual(1, _spawned.Count, "当たった瞬間には出さない（次の Tick）");
            _system.Tick(0.016f);
            Assert.AreEqual(3, _spawned.Count, "本体 + 分裂2体");

            boss.TakeHit(new Damage(10));    // まだ 2つ目の手前
            _system.Tick(0.016f);
            Assert.AreEqual(3, _spawned.Count, "同じしきい値では増えない");

            boss.TakeHit(new Damage(100));   // 80/300 = 0.27 → 2つ目
            _system.Tick(0.016f);
            Assert.AreEqual(5, _spawned.Count);
            Assert.IsTrue(boss.IsAlive);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Splitter_BigHit_CrossingBothThresholds_SpawnsBothSplits()
        {
            var boss = SpawnNow("Splitter");
            boss.TakeHit(new Damage(250));   // 50/300 → 2つとも切る
            _system.Tick(0.016f);
            Assert.AreEqual(1 + 4, _spawned.Count);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Splitter_KilledInOneHit_DoesNotSplit()
        {
            var boss = SpawnNow("Splitter");
            boss.TakeHit(new Damage(1000));
            _system.Tick(0.016f);
            Assert.AreEqual(1, _spawned.Count);
            Assert.AreEqual(1, _died);
            yield return null;
        }

        /// <summary>ウェーブの数え方を、本番と同じ形（Spawned で登録、Died で減らす）でつなぐ。Died は位置から対応する敵を探す。</summary>
        sealed class WaveWiring
        {
            readonly WaveProgress _progress = new WaveProgress();
            readonly Dictionary<IEnemyTarget, int> _ids = new Dictionary<IEnemyTarget, int>();
            public int ClearedCount;

            public WaveWiring(EnemySystem system)
            {
                _progress.Cleared += () => ClearedCount++;
                system.Spawned += enemy =>
                {
                    _ids[enemy] = _ids.Count;
                    _progress.RegisterEnemy(_ids[enemy]);
                };
                system.Died += death =>
                {
                    foreach (var pair in _ids)
                    {
                        if (pair.Key.IsAlive || pair.Key.Position != death.Position) continue;
                        _progress.MarkKilled(pair.Value);
                    }
                };
            }

            public WaveProgress Progress => _progress;
            public List<IEnemyTarget> All => _ids.Keys.ToList();
        }

        [UnityTest]
        public IEnumerator Wave_EndsOnlyAfterEveryEnemyIncludingSplitChildrenIsDead()
        {
            var wave = new WaveWiring(_system);
            var boss = SpawnNow("Splitter");
            boss.TakeHit(new Damage(110));
            _system.Tick(0.016f);
            wave.Progress.FinishSpawning();
            Assert.AreEqual(3, wave.Progress.AliveCount, "本体と分裂2体");

            foreach (var enemy in wave.All)
            {
                Assert.AreEqual(0, wave.ClearedCount, "全部倒すまで終わらない");
                enemy.TakeHit(new Damage(10000));
            }

            Assert.AreEqual(1, wave.ClearedCount, "全部倒すとちょうど1回だけ終わる");
            Assert.AreEqual(3, _died, "本体と分裂2体で Died が3回");
            Assert.AreEqual(0, _registry.Active.Count, "全部倒すと Registry は空");
            yield return null;
        }

        [UnityTest]
        public IEnumerator Wave_DoesNotClear_WhenSplitterDiesRightAfterSplitRequest_BeforeNextTick()
        {
            var wave = new WaveWiring(_system);
            var boss = SpawnNow("Splitter");
            wave.Progress.FinishSpawning();

            boss.TakeHit(new Damage(110));    // 分裂を要求（子はまだ出ていない）
            boss.TakeHit(new Damage(1000));   // 次の Tick より前に本体が死ぬ
            Assert.AreEqual(0, _died, "子が出るまで本体の Died は保留される");
            Assert.AreEqual(0, wave.ClearedCount, "子が出る前にウェーブが終わってはいけない");

            _system.Tick(0.016f);   // 子が出て、そのあとで保留の Died が届く
            Assert.AreEqual(3, wave.All.Count, "本体 + 分裂2体");
            Assert.AreEqual(1, _died);
            Assert.AreEqual(0, wave.ClearedCount, "子が生きているのでまだ終わらない");
            Assert.AreEqual(2, wave.Progress.AliveCount);

            foreach (var enemy in wave.All.Where(e => e.IsAlive).ToList()) enemy.TakeHit(new Damage(10000));
            Assert.AreEqual(1, wave.ClearedCount, "子を全部倒したら終わる");
            yield return null;
        }

        [UnityTest]
        public IEnumerator Tier_RaisesHp_AndSplitCount()
        {
            var tier2 = SpawnNow("Splitter", 2);   // HP 300 × (1 + 0.5×2) = 600、1回の分裂 2 + 1×2 = 4体
            int before = _spawned.Count;
            tier2.TakeHit(new Damage(300));   // 600 のうち 300 → 0.5 → 1つ目だけ切る
            _system.Tick(0.016f);
            Assert.IsTrue(tier2.IsAlive, "HP が増えているので 300 では倒れない");
            Assert.AreEqual(before + 4, _spawned.Count, "tier 2 は1回の分裂で 4 体");
            yield return null;
        }
    }
}

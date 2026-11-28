using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using PictoArcher.Game;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace PictoArcher.Tests.PlayMode
{
    public class PickupSystemPlayModeTests
    {
        sealed class FakePlayer : ITarget
        {
            public Vector2 Position { get; set; } = new Vector2(100f, 100f);
            public bool IsAlive { get; set; } = true;
        }

        sealed class FakeEnemies : IEnemyEvents
        {
            public event Action<DamageReport> Damaged { add { } remove { } }   // このテストでは使わない
            public event Action<EnemyDeath> Died;
            public void Kill(Vector2 at, int xp, int coins) => Died?.Invoke(new EnemyDeath(null, at, xp, coins));
            public bool HasListeners => Died != null;
        }

        FakePlayer _player;
        FakeEnemies _enemies;
        PickupSettings _settings;
        PickupSystem _system;
        readonly List<(PickupKind, int)> _collected = new List<(PickupKind, int)>();

        [SetUp]
        public void SetUp()
        {
            _player = new FakePlayer();
            _enemies = new FakeEnemies();
            _collected.Clear();
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Field/Pickup.prefab").GetComponent<PickupView>();
            _settings = AssetDatabase.LoadAssetAtPath<PickupSettings>("Assets/_Project/Data/Player/PickupSettings.asset");
            _system = new PickupSystem(prefab, _settings, _enemies, _player);
            _system.Collected += (k, n) => _collected.Add((k, n));
        }

        [TearDown]
        public void TearDown() => _system.Dispose();

        static PickupView[] Views() => Object.FindObjectsByType<PickupView>(FindObjectsSortMode.None);

        void TickFor(float seconds)
        {
            for (float t = 0f; t < seconds - 0.0001f; t += 0.02f) _system.Tick(0.02f);
        }

        [UnityTest]
        public IEnumerator EnemyDied_DropsExperienceLeftAndCoinRight()
        {
            _enemies.Kill(new Vector2(1f, 2f), 3, 1);
            yield return null;
            var views = Views();
            Assert.AreEqual(2, views.Length);
            Assert.AreEqual(2, _system.ActiveCount);
            var xs = new List<float> { views[0].transform.position.x, views[1].transform.position.x };
            xs.Sort();
            Assert.AreEqual(1f - _settings.DropSpread, xs[0], 1e-4f);
            Assert.AreEqual(1f + _settings.DropSpread, xs[1], 1e-4f);
            foreach (var v in views) Assert.IsNotNull(v.GetComponent<SpriteRenderer>().sprite);
            Assert.IsNull(views[0].GetComponent<Collider2D>(), "当たり判定は持たない（距離で拾う）");
        }

        [UnityTest]
        public IEnumerator ZeroReward_DropsNothing()
        {
            _enemies.Kill(Vector2.zero, 0, 0);
            yield return null;
            Assert.AreEqual(0, Views().Length);
            Assert.AreEqual(0, _system.ActiveCount);
        }

        [UnityTest]
        public IEnumerator FarPlayer_PickupsStayPut()
        {
            _enemies.Kill(Vector2.zero, 3, 1);
            TickFor(1f);
            yield return null;
            Assert.AreEqual(2, _system.ActiveCount);
            Assert.AreEqual(0, _system.Experience);
        }

        [UnityTest]
        public IEnumerator NearPlayer_AttractsAndCollects_AddsTotals()
        {
            _enemies.Kill(Vector2.zero, 3, 2);
            _player.Position = new Vector2(0f, 2f);   // 吸い寄せの半径（2.5）の内側
            TickFor(1f);
            yield return null;
            Assert.AreEqual(0, _system.ActiveCount);
            Assert.AreEqual(0, Views().Length);
            Assert.AreEqual(3, _system.Experience);
            Assert.AreEqual(2, _system.Coins);
            CollectionAssert.AreEquivalent(new[] { (PickupKind.Experience, 3), (PickupKind.Coin, 2) }, _collected);
        }

        [UnityTest]
        public IEnumerator AttractAll_PullsFarPickups_IncludingOnesDroppedLaterInTheSameFrame()
        {
            _enemies.Kill(new Vector2(-5f, 0f), 1, 0);
            _system.AttractAll();
            _enemies.Kill(new Vector2(5f, 0f), 0, 4);   // 全滅の知らせのあとに落ちた分
            Assert.AreEqual(4, _system.PendingCoins);
            _player.Position = new Vector2(0f, 10f);
            TickFor(3f);
            yield return null;
            Assert.AreEqual(0, _system.ActiveCount);
            Assert.AreEqual(1, _system.Experience);
            Assert.AreEqual(4, _system.Coins);
            Assert.AreEqual(0, _system.PendingCoins);
        }

        [UnityTest]
        public IEnumerator DeadPlayer_DoesNotCollect()
        {
            _enemies.Kill(Vector2.zero, 3, 1);
            _player.Position = Vector2.zero;
            _player.IsAlive = false;
            TickFor(0.5f);
            yield return null;
            Assert.AreEqual(2, _system.ActiveCount);
            Assert.AreEqual(0, _system.Experience);
        }

        [UnityTest]
        public IEnumerator Dispose_DestroysViewsAndStopsListening()
        {
            _enemies.Kill(Vector2.zero, 3, 1);
            _system.Dispose();
            yield return null;
            Assert.AreEqual(0, Views().Length);
            Assert.IsFalse(_enemies.HasListeners);
        }
    }
}

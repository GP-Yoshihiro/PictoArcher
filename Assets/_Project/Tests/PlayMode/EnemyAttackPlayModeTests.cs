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
    /// <summary>Shooter・SpreadShooter・Bomber が、赤い線・円の予兆のあとに攻撃するか。</summary>
    public class EnemyAttackPlayModeTests
    {
        const string DataPath = "Assets/_Project/Data/Enemies/";
        const float Step = 0.05f;

        sealed class FakePlayer : ITarget, IHittable
        {
            public Vector2 Position { get; set; }
            public bool IsAlive => true;
            public readonly List<Damage> Hits = new List<Damage>();
            public void TakeHit(Damage damage) => Hits.Add(damage);
        }

        readonly List<GameObject> _created = new List<GameObject>();
        EnemySystem _system;
        EnemyProjectileSystem _projectiles;
        FakePlayer _player;

        [TearDown]
        public void TearDown()
        {
            _system?.Dispose();
            _projectiles?.Dispose();
            foreach (var go in _created) if (go != null) Object.Destroy(go);
            _created.Clear();
        }

        void Setup(Vector2 playerPosition)
        {
            _player = new FakePlayer { Position = playerPosition };
            _projectiles = new EnemyProjectileSystem(_player);
            var spawn = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Enemies/SpawnTelegraph.prefab").GetComponent<SpawnTelegraphView>();
            var settings = AssetDatabase.LoadAssetAtPath<SpawnTelegraphSettings>(DataPath + "SpawnTelegraphSettings.asset");
            var attack = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Enemies/AttackTelegraph.prefab").GetComponent<AttackTelegraphView>();
            _system = new EnemySystem(new EnemyRegistry(), spawn, settings, attack, _projectiles, _player, _player);
        }

        static int VisibleRedMarks()
        {
            // 予兆の線・円は AttackTelegraph の子の SpriteRenderer。出ているものだけ数える
            return Object.FindObjectsByType<AttackTelegraphView>(FindObjectsSortMode.None)
                .Sum(view => view.GetComponentsInChildren<SpriteRenderer>().Count(r => r.enabled));
        }

        static EnemyDefinition Load(string name) => AssetDatabase.LoadAssetAtPath<EnemyDefinition>(DataPath + name + ".asset");

        /// <summary>撃つ敵を動かし、最初の弾が出たときに見えていた予兆の数と、出た弾の数を返す。</summary>
        IEnumerator RunUntilFirstShot(string enemyName, System.Action<int, int> result)
        {
            Setup(new Vector2(5f, 0f));   // 射程の中。敵は動かず狙い始める
            _system.Spawn(Load(enemyName), Vector2.zero, 0.1f);
            int marksBeforeShot = 0;
            for (int i = 0; i < 200; i++)
            {
                _system.Tick(Step);
                _projectiles.Tick(Step);
                yield return null;
                if (_projectiles.Count > 0)
                {
                    result(marksBeforeShot, _projectiles.Count);
                    yield break;
                }
                marksBeforeShot = Mathf.Max(marksBeforeShot, VisibleRedMarks());
            }
            result(marksBeforeShot, 0);
        }

        [UnityTest]
        public IEnumerator Shooter_ShowsOneRedLine_ThenFiresOneBullet()
        {
            int marks = -1, bullets = -1;
            yield return RunUntilFirstShot("Shooter", (m, b) => { marks = m; bullets = b; });
            Assert.AreEqual(1, marks, "撃つ前に赤い線が1本出ている");
            Assert.AreEqual(1, bullets, "1発撃つ");
        }

        [UnityTest]
        public IEnumerator SpreadShooter_ShowsThreeRedLines_ThenFiresThreeBullets()
        {
            int marks = -1, bullets = -1;
            yield return RunUntilFirstShot("SpreadShooter", (m, b) => { marks = m; bullets = b; });
            Assert.AreEqual(3, marks, "撃つ前に赤い線が3本出ている");
            Assert.AreEqual(3, bullets, "3発を同時に撃つ");
        }

        [UnityTest]
        public IEnumerator Shooter_BulletHitsPlayer_AndDisappears()
        {
            Setup(new Vector2(5f, 0f));
            var wall = new GameObject("PlayerBody") { layer = Layers.Player };
            wall.transform.position = new Vector3(5f, 0f, 0f);
            wall.AddComponent<BoxCollider2D>().size = new Vector2(1f, 6f);
            _created.Add(wall);

            _system.Spawn(Load("Shooter"), Vector2.zero, 0.1f);
            for (float elapsed = 0f; elapsed < 8f && _player.Hits.Count == 0; elapsed += Time.deltaTime)
            {
                _system.Tick(Time.deltaTime);
                _projectiles.Tick(Time.deltaTime);
                yield return null;
            }

            Assert.AreEqual(1, _player.Hits.Count, "弾が当たると1回だけダメージ");
            Assert.AreEqual(10, _player.Hits[0].Amount);
            yield return null;
            Assert.AreEqual(0, _projectiles.Count, "当たった弾は消える");
        }

        [UnityTest]
        public IEnumerator Bomber_ShowsExpandingCircle_ThenExplodesOnce_AndDamagesNearbyPlayer()
        {
            Setup(new Vector2(1f, 0f));   // 近づく前から自爆の範囲に入っている
            int deaths = 0;
            _system.Died += _ => deaths++;
            _system.Spawn(Load("Bomber"), Vector2.zero, 0.1f);

            int circleSeen = 0;
            for (int i = 0; i < 120 && deaths == 0; i++)
            {
                _system.Tick(Step);
                yield return null;
                if (deaths == 0) circleSeen = Mathf.Max(circleSeen, VisibleRedMarks());
            }

            Assert.AreEqual(1, circleSeen, "爆発の前に赤い円が出ている");
            Assert.AreEqual(1, deaths, "自爆して倒れる（Died は1回）");
            Assert.AreEqual(1, _player.Hits.Count);
            Assert.AreEqual(DamageKind.Explosion, _player.Hits[0].Kind);
            Assert.AreEqual(20, _player.Hits[0].Amount);
        }

        [UnityTest]
        public IEnumerator Bomber_DoesNotDamagePlayerOutsideBlastRadius()
        {
            Setup(new Vector2(2f, 0f));   // 近づく前から止まる距離の中。円が出たら爆発の外へ逃げる
            int deaths = 0;
            _system.Died += _ => deaths++;
            _system.Spawn(Load("Bomber"), Vector2.zero, 0.1f);
            for (int i = 0; i < 120 && deaths == 0; i++)
            {
                _system.Tick(Step);
                if (VisibleRedMarks() > 0) _player.Position = new Vector2(20f, 0f);
                yield return null;
            }

            Assert.AreEqual(1, deaths);
            Assert.AreEqual(0, _player.Hits.Count, "円の外に逃げていればダメージを受けない");
        }
    }
}

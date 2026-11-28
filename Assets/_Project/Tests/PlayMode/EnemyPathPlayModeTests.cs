using System.Collections;
using NUnit.Framework;
using PictoArcher.Core;
using PictoArcher.Game;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace PictoArcher.Tests.PlayMode
{
    /// <summary>本物の障害物と物理で、追う敵が障害物を回り込んでプレイヤーに届くか確かめる。</summary>
    public class EnemyPathPlayModeTests
    {
        const string ChaserPath = "Assets/_Project/Data/Enemies/Chaser.asset";
        const string LayoutPath = "Assets/_Project/Data/Obstacles/Layout2_Columns.asset";
        const string PathSettingsPath = "Assets/_Project/Data/Enemies/EnemyPathSettings.asset";
        // Layout2_Columns の真ん中の右の柱（x 4.5, y -1.5）。左右どちらにも回り込める
        const int PillarIndex = 3;
        const float Gap = 2f;   // 柱の上と下に空ける距離
        const float Reach = 1f;   // プレイヤーにこの距離まで近づいたら届いたとみなす（追う敵の体の半径は 0.6）

        sealed class FakePlayer : ITarget, IHittable
        {
            public Vector2 Position { get; set; }
            public bool IsAlive => true;
            public void TakeHit(Damage damage) { }
        }

        [SetUp] public void ClearSave() => SaveFileIsolation.Clear();

        FakePlayer _player;
        GameObject _parent;
        ObstacleSystem _obstacles;
        EnemyPathSystem _path;
        EnemySystem _enemies;
        Rect _pillar;

        void Build(bool withRoute)
        {
            _player = new FakePlayer();
            _parent = new GameObject("Obstacles");
            var obstaclePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Field/Obstacle.prefab").GetComponent<ObstacleView>();
            var switchSettings = AssetDatabase.LoadAssetAtPath<ObstacleSwitchSettings>("Assets/_Project/Data/Obstacles/ObstacleSwitchSettings.asset");
            var layout = AssetDatabase.LoadAssetAtPath<ObstacleLayout>(LayoutPath);
            _pillar = layout.Placements[PillarIndex].Bounds;
            // プレイヤーは柱の真下。敵は真上から出す（まっすぐ進むと柱の面に正面からぶつかる）
            _player.Position = new Vector2(_pillar.center.x, _pillar.yMin - Gap);
            _obstacles = new ObstacleSystem(obstaclePrefab, _parent.transform, switchSettings, _player);
            _obstacles.ShowImmediately(layout);

            var telegraph = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Enemies/SpawnTelegraph.prefab").GetComponent<SpawnTelegraphView>();
            var telegraphSettings = AssetDatabase.LoadAssetAtPath<SpawnTelegraphSettings>("Assets/_Project/Data/Enemies/SpawnTelegraphSettings.asset");
            var attack = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Enemies/AttackTelegraph.prefab").GetComponent<AttackTelegraphView>();
            _path = withRoute ? new EnemyPathSystem(AssetDatabase.LoadAssetAtPath<EnemyPathSettings>(PathSettingsPath), _obstacles, _player) : null;
            _enemies = new EnemySystem(new EnemyRegistry(), telegraph, telegraphSettings, attack, new EnemyProjectileSystem(_player), _player, _player, _path);
        }

        [TearDown]
        public void TearDown()
        {
            _enemies?.Dispose();
            _path?.Dispose();
            _obstacles?.Dispose();
            if (_parent != null) Object.Destroy(_parent);
        }

        IEnumerator ChaseFor(float seconds)
        {
            var definition = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(ChaserPath);
            _enemies.Spawn(definition, new Vector2(_pillar.center.x, _pillar.yMax + Gap), 0.05f);
            for (float t = 0f; t < seconds && DistanceToPlayer() > Reach; t += Time.deltaTime)
            {
                yield return null;
                _obstacles.Tick(Time.deltaTime);
                _path?.Tick(Time.deltaTime);
                _enemies.Tick(Time.deltaTime);
            }
        }

        EnemyView Enemy() => Object.FindAnyObjectByType<EnemyView>();

        float DistanceToPlayer()
        {
            var enemy = Enemy();
            return enemy == null ? float.PositiveInfinity : Vector2.Distance(enemy.transform.position, _player.Position);
        }

        [UnityTest]
        public IEnumerator Chaser_WithRoute_GoesAroundThePillar_AndReachesThePlayer()
        {
            Build(withRoute: true);
            yield return ChaseFor(8f);

            Assert.IsNotNull(Enemy());
            Assert.LessOrEqual(DistanceToPlayer(), Reach, "柱を回り込んでプレイヤーのそばまで来る");
        }

        // 回り込みがないと、柱の上の面に止まったまま届かない（直す前の不具合。テストが意味を持つことの確かめ）
        [UnityTest]
        public IEnumerator Chaser_WithoutRoute_StaysStuckAboveThePillar()
        {
            Build(withRoute: false);
            yield return ChaseFor(3f);

            var enemy = Enemy();
            Assert.IsNotNull(enemy);
            Assert.Greater(((Vector2)enemy.transform.position).y, _pillar.yMax - 0.1f, "柱の上の面で止まっている");
            Assert.Greater(DistanceToPlayer(), Reach);
        }
    }
}

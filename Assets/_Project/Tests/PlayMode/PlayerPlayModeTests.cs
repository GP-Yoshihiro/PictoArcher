using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using PictoArcher.Core;
using PictoArcher.Game;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace PictoArcher.Tests.PlayMode
{
    public class PlayerPlayModeTests
    {
        // 保存は Main が読むので、前のテストの記録や武器を持ち越さないよう、毎回消してから始める
        [SetUp] public void ClearSave() => SaveFileIsolation.Clear();

        const string SandboxPath = "Assets/_Project/Scenes/Sandbox/Sandbox_A.unity";
        const float Step = 0.02f;

        readonly List<GameObject> _spawned = new List<GameObject>();
        bool _sandboxLoaded;

        sealed class FakeInput : IMoveInput
        {
            public Vector2 Move;
            public Vector2 ReadMove(float dragRadiusPixels) => Move;
        }

        sealed class FakeEnemy : IEnemyTarget
        {
            public Vector2 Position { get; set; }
            public bool IsAlive { get; set; } = true;
            public int Hits;
            public void TakeHit(Damage damage) => Hits++;
            public void ApplyStatus(StatusEffect effect) { }
        }

        static PlayerProjectileView ShotPrefab => AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Projectiles/PlayerShot.prefab").GetComponent<PlayerProjectileView>();
        static PlayerView PlayerPrefab => AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Player/Player.prefab").GetComponent<PlayerView>();
        static PlayerStats Stats => AssetDatabase.LoadAssetAtPath<PlayerStats>("Assets/_Project/Data/Player/PlayerStats.asset");
        static SpriteVisual ShotVisual => AssetDatabase.LoadAssetAtPath<SpriteVisual>("Assets/_Project/Data/Visuals/Projectiles/PlayerBullet.asset");
        static ShotEffectView EffectPrefab => AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Projectiles/ShotEffect.prefab").GetComponent<ShotEffectView>();
        static IWeapon Gun => AssetDatabase.LoadAssetAtPath<GunWeaponDefinition>("Assets/_Project/Data/Player/GunWeapon.asset").Create();
        static LaserWeaponDefinition Laser => AssetDatabase.LoadAssetAtPath<LaserWeaponDefinition>("Assets/_Project/Data/Player/LaserWeapon.asset");
        static LauncherWeaponDefinition Launcher => AssetDatabase.LoadAssetAtPath<LauncherWeaponDefinition>("Assets/_Project/Data/Player/LauncherWeapon.asset");
        static CharacterVisual GunVisual => AssetDatabase.LoadAssetAtPath<PlayerVisual>("Assets/_Project/Data/Visuals/PlayerVisual.asset").Gun;

        static PlayerProjectileSystem NewShots(EnemyRegistry registry) => new PlayerProjectileSystem(registry, ShotPrefab, EffectPrefab);

        Collider2D Block(int layer, Vector2 position)
        {
            var go = new GameObject("Block") { layer = layer };
            go.transform.position = position;
            go.AddComponent<BoxCollider2D>().size = Vector2.one;
            _spawned.Add(go);
            Physics2D.SyncTransforms();
            return go.GetComponent<Collider2D>();
        }

        FakeEnemy AddEnemy(EnemyRegistry registry, Vector2 position)
        {
            var enemy = new FakeEnemy { Position = position };
            registry.Register(enemy, Block(Layers.Enemy, position));
            return enemy;
        }

        [UnityTest]
        public IEnumerator Laser_HitsEveryEnemyOnTheLine_ButStopsAtAWall()
        {
            var registry = new EnemyRegistry();
            var shots = NewShots(registry);
            var first = AddEnemy(registry, new Vector2(2f, 0f));
            var second = AddEnemy(registry, new Vector2(4f, 0.3f));
            var aside = AddEnemy(registry, new Vector2(3f, 3f));
            var dead = AddEnemy(registry, new Vector2(3f, 0f));
            dead.IsAlive = false;
            Block(Layers.Wall, new Vector2(6f, 0f));
            var behind = AddEnemy(registry, new Vector2(8f, 0f));

            Laser.Create().Fire(Vector2.zero, Vector2.right, shots);

            Assert.AreEqual(1, first.Hits, "線の上の敵すべてに1回ずつ当たる");
            Assert.AreEqual(1, second.Hits);
            Assert.AreEqual(0, aside.Hits, "線から離れた敵には当たらない");
            Assert.AreEqual(0, dead.Hits, "死んだ敵には当たらない");
            Assert.AreEqual(0, behind.Hits, "壁の向こうには届かない");
            Assert.AreEqual(0, shots.Count, "光線は飛ぶ弾にならない");
            Assert.AreEqual(1, shots.EffectCount, "光線が見える");

            for (int i = 0; i < 20; i++)
            {
                yield return null;
                shots.Tick(Step);
            }
            Assert.AreEqual(0, shots.EffectCount, "少しすると消える");
            shots.Dispose();
        }

        [UnityTest]
        public IEnumerator Launcher_ExplodesOnHit_AndHitsEnemiesAround()
        {
            var registry = new EnemyRegistry();
            var shots = NewShots(registry);
            var target = AddEnemy(registry, new Vector2(3f, 0f));
            var near = AddEnemy(registry, new Vector2(3f, 1.5f));
            var far = AddEnemy(registry, new Vector2(3f, Launcher.BlastRadius + 2f));

            Launcher.Create().Fire(Vector2.zero, Vector2.right, shots);
            for (int i = 0; i < 60 && shots.Count > 0; i++)
            {
                yield return new WaitForFixedUpdate();
                shots.Tick(Step);
            }

            Assert.AreEqual(0, shots.Count, "当たった弾は消える");
            Assert.AreEqual(1, target.Hits, "当たった敵には1回だけ");
            Assert.AreEqual(1, near.Hits, "爆発の半径の内の敵にも当たる");
            Assert.AreEqual(0, far.Hits, "半径の外には当たらない");
            Assert.AreEqual(1, shots.EffectCount, "爆発が見える");
            shots.Dispose();
        }

        [UnityTest]
        public IEnumerator Launcher_VanishesWithoutExploding_WhenItHitsNothing()
        {
            var registry = new EnemyRegistry();
            var shots = NewShots(registry);
            var aside = AddEnemy(registry, new Vector2(0f, 1.5f));

            Launcher.Create().Fire(Vector2.zero, Vector2.right, shots);
            for (int i = 0; i < 400 && shots.Count > 0; i++)
            {
                yield return new WaitForFixedUpdate();
                shots.Tick(Step);
            }

            Assert.AreEqual(0, shots.Count, "寿命が切れると消える");
            Assert.AreEqual(0, shots.EffectCount, "何にも当たらずに消えたら爆発しない");
            Assert.AreEqual(0, aside.Hits);
            shots.Dispose();
        }

        [UnityTest]
        public IEnumerator Launcher_ExplodesOnAWall_AndHitsEnemiesNearIt()
        {
            var registry = new EnemyRegistry();
            var shots = NewShots(registry);
            Block(Layers.Wall, new Vector2(3f, 0f));
            var byWall = AddEnemy(registry, new Vector2(2.5f, 1.4f));
            var behind = AddEnemy(registry, new Vector2(3f + Launcher.BlastRadius + 2f, 0f));

            Launcher.Create().Fire(Vector2.zero, Vector2.right, shots);
            for (int i = 0; i < 60 && shots.Count > 0; i++)
            {
                yield return new WaitForFixedUpdate();
                shots.Tick(Step);
            }

            Assert.AreEqual(0, shots.Count, "壁で爆発して消える");
            Assert.AreEqual(1, byWall.Hits, "壁のそばの敵に爆発が当たる");
            Assert.AreEqual(0, behind.Hits);
            shots.Dispose();
        }

        [UnityTest]
        public IEnumerator Shot_HitsOnlyOneOfOverlappingEnemies_AndDisappears()
        {
            var registry = new EnemyRegistry();
            var shots = NewShots(registry);
            var a = AddEnemy(registry, new Vector2(3f, 0f));
            var b = AddEnemy(registry, new Vector2(3f, 0f));

            shots.Fire(Vector2.zero, Vector2.right, 14f, 10, 3f, ShotVisual);
            for (int i = 0; i < 40 && shots.Count > 0; i++)
            {
                yield return new WaitForFixedUpdate();
                shots.Tick(Step);
            }

            Assert.AreEqual(0, shots.Count, "当たった弾は消える");
            Assert.AreEqual(1, a.Hits + b.Hits, "1発は1体にだけ当たる");
            shots.Dispose();
        }

        [UnityTest]
        public IEnumerator Shot_DisappearsOnWall()
        {
            var registry = new EnemyRegistry();
            var shots = NewShots(registry);
            Block(Layers.Wall, new Vector2(3f, 0f));
            var behind = AddEnemy(registry, new Vector2(5f, 0f));

            shots.Fire(Vector2.zero, Vector2.right, 14f, 10, 3f, ShotVisual);
            for (int i = 0; i < 60 && shots.Count > 0; i++)
            {
                yield return new WaitForFixedUpdate();
                shots.Tick(Step);
            }

            Assert.AreEqual(0, shots.Count, "壁で消える");
            Assert.AreEqual(0, behind.Hits, "壁の向こうの敵には当たらない");
            shots.Dispose();
        }

        [UnityTest]
        public IEnumerator Player_FiresAtNearestEnemy_OnlyWhenThereIsOne()
        {
            var registry = new EnemyRegistry();
            var shots = NewShots(registry);
            var player = new PlayerSystem(PlayerPrefab, Vector2.zero, Stats, GunVisual, Gun, new FakeInput(), registry, shots);

            for (int i = 0; i < 50; i++)
            {
                player.Tick(Step);
                yield return null;
            }
            Assert.AreEqual(0, shots.Count, "的がないと撃たない");

            var far = AddEnemy(registry, new Vector2(0f, 9f));
            var near = AddEnemy(registry, new Vector2(4f, 0f));
            for (int i = 0; i < 60; i++)
            {
                player.Tick(Step);
                yield return new WaitForFixedUpdate();
                shots.Tick(Step);
            }

            Assert.Greater(near.Hits, 0, "近い的に当たる");
            Assert.AreEqual(0, far.Hits, "遠い的は狙わない");
            player.Dispose();
            shots.Dispose();
        }

        [UnityTest]
        public IEnumerator Player_TakeHit_IsBlockedWhileInvulnerable_AndDiesOnce()
        {
            var registry = new EnemyRegistry();
            var shots = NewShots(registry);
            var player = new PlayerSystem(PlayerPrefab, Vector2.zero, Stats, GunVisual, Gun, new FakeInput(), registry, shots);
            int died = 0;
            player.Died += () => died++;

            player.TakeHit(new Damage(10));
            player.TakeHit(new Damage(10));
            Assert.AreEqual(Stats.MaxHp - 10, player.Hp, "無敵時間の間の2発目は効かない");
            Assert.IsTrue(player.IsInvulnerable);

            player.Tick(Stats.InvulnerabilitySeconds + 0.01f);
            Assert.IsFalse(player.IsInvulnerable, "無敵時間が終わる");
            player.TakeHit(new Damage(Stats.MaxHp));
            player.Tick(Stats.InvulnerabilitySeconds + 0.01f);
            player.TakeHit(new Damage(10));

            Assert.IsTrue(player.IsDead);
            Assert.IsFalse(player.IsAlive);
            Assert.AreEqual(1, died, "死亡は1回だけ知らせる");
            player.Dispose();
            shots.Dispose();
            yield return null;
        }

        [UnityTest]
        public IEnumerator Player_MovesAtMoveSpeed_AndFacesLeftWhenMovingLeft()
        {
            var registry = new EnemyRegistry();
            var shots = NewShots(registry);
            var input = new FakeInput { Move = new Vector2(-1f, 0f) };
            var player = new PlayerSystem(PlayerPrefab, Vector2.zero, Stats, GunVisual, Gun, input, registry, shots);

            player.Tick(Step);
            yield return new WaitForFixedUpdate();
            var view = Object.FindObjectsByType<PlayerView>(FindObjectsSortMode.None)[0];
            var body = view.GetComponent<Rigidbody2D>();

            Assert.AreEqual(Stats.MoveSpeed, body.linearVelocity.magnitude, 0.01f);
            Assert.Less(body.linearVelocity.x, 0f);
            Assert.IsTrue(view.Sprite.flipX, "左へ動くと左を向く");
            player.Dispose();
            shots.Dispose();
        }

        [UnityTest]
        public IEnumerator Sandbox_PlayerShootsSpawnedEnemy()
        {
            _sandboxLoaded = true;
            EditorSceneManager.LoadSceneInPlayMode(SandboxPath, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            SceneManager.LoadScene(SceneTransform.RootSceneName, LoadSceneMode.Additive);
            yield return null;
            yield return null;

            Assert.AreEqual(1, Object.FindObjectsByType<PlayerView>(FindObjectsSortMode.None).Length, "プレイヤーが1人出る");
            bool shot = false;
            for (float t = 0f; t < 3f && !shot; t += Time.deltaTime)
            {
                shot = Object.FindObjectsByType<PlayerProjectileView>(FindObjectsSortMode.None).Length > 0;
                yield return null;
            }
            Assert.IsTrue(shot, "敵が出たら自動で撃つ");
        }

        [UnityTearDown]
        public IEnumerator CleanUp()
        {
            foreach (var go in _spawned) if (go != null) Object.Destroy(go);
            _spawned.Clear();
            if (_sandboxLoaded)
            {
                _sandboxLoaded = false;
                SceneManager.LoadScene(SceneTransform.RootSceneName);
                yield return null;
            }
            yield return null;
        }
    }
}

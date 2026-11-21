using PictoArcher.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PictoArcher.Game
{
    /// <summary>
    /// B さんの試し用のシーン。矢印キーで仮のプレイヤーを動かし、一定の間隔で予兆→敵（_enemyKinds を順に）を出す。
    /// マウスの左クリックで、プレイヤーに一番近い敵にダメージ 10（弾の代わり）。結果はコンソールに出る。
    /// </summary>
    public sealed class SandboxBScene : SceneBase
    {
        [SerializeField] Rigidbody2D _playerBody;
        [SerializeField] SpawnTelegraphView _telegraphPrefab;
        [SerializeField] SpawnTelegraphSettings _telegraphSettings;
        [SerializeField] AttackTelegraphView _attackTelegraphPrefab;
        [SerializeField, Tooltip("出す敵を順に使う")] EnemyDefinition[] _enemyKinds;
        [SerializeField, Tooltip("これだけ敵がいるときは新しく出さない")] int _maxEnemies = 3;
        [SerializeField] Vector2[] _spawnPoints;
        [SerializeField] float _spawnInterval = 3f;
        [SerializeField] float _telegraphSeconds = 0.8f;
        [SerializeField] int _playerMaxHp = 100;
        [SerializeField] float _playerInvulnerableSeconds = 0.6f;
        [SerializeField] float _playerSpeed = 4f;
        [SerializeField] int _testShotDamage = 10;

        EnemyRegistry _registry;
        SandboxPlayer _player;
        EnemyProjectileSystem _projectiles;
        EnemySystem _enemies;
        int _nextKind;
        float _spawnTimer;
        int _nextSpawnPoint;

        public override void Init(GameContext context)
        {
            _registry = new EnemyRegistry();
            _player = new SandboxPlayer(_playerBody, _playerMaxHp, _playerInvulnerableSeconds, _playerSpeed);
            _projectiles = new EnemyProjectileSystem(_player);
            _enemies = new EnemySystem(_registry, _telegraphPrefab, _telegraphSettings, _attackTelegraphPrefab, _projectiles, _player, _player);
            _enemies.Spawned += OnSpawned;
            _enemies.Damaged += OnDamaged;
            _enemies.Died += OnDied;
            _spawnTimer = 0f;
        }

        public override void Tick(float deltaTime)
        {
            _player.Tick(deltaTime, ReadMove());
            TickSpawn(deltaTime);
            TickTestShot();
            _enemies.Tick(deltaTime);
            _projectiles.Tick(deltaTime);
        }

        public override void Dispose()
        {
            _enemies.Died -= OnDied;
            _enemies.Damaged -= OnDamaged;
            _enemies.Spawned -= OnSpawned;
            _enemies.Dispose();
            _projectiles.Dispose();
            _registry.Clear();
        }

        static Vector2 ReadMove()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return Vector2.zero;
            float x = (keyboard.rightArrowKey.isPressed ? 1f : 0f) - (keyboard.leftArrowKey.isPressed ? 1f : 0f);
            float y = (keyboard.upArrowKey.isPressed ? 1f : 0f) - (keyboard.downArrowKey.isPressed ? 1f : 0f);
            return new Vector2(x, y);
        }

        void TickSpawn(float deltaTime)
        {
            if (_spawnPoints.Length == 0 || _enemyKinds.Length == 0) return;
            _spawnTimer -= deltaTime;
            if (_spawnTimer > 0f) return;
            _spawnTimer = _spawnInterval;
            if (_registry.Active.Count + _enemies.PendingCount >= _maxEnemies) return;
            _enemies.Spawn(_enemyKinds[_nextKind], _spawnPoints[_nextSpawnPoint], _telegraphSeconds);
            _nextKind = (_nextKind + 1) % _enemyKinds.Length;
            _nextSpawnPoint = (_nextSpawnPoint + 1) % _spawnPoints.Length;
        }

        void TickTestShot()
        {
            var mouse = Mouse.current;
            if (mouse == null || !mouse.leftButton.wasPressedThisFrame) return;
            if (_registry.TryFindNearest(_player.Position, out var nearest)) nearest.TakeHit(new Damage(_testShotDamage));
        }

        void OnSpawned(IEnemyTarget enemy) => Debug.Log($"Spawned at {enemy.Position}");
        void OnDamaged(DamageReport report) => Debug.Log($"Damaged {report.Amount} killed={report.Killed}");
        void OnDied(EnemyDeath death) => Debug.Log($"Died xp={death.Experience} coin={death.Coins}");
    }
}

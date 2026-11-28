using UnityEngine;

namespace PictoArcher.Game
{
    /// <summary>
    /// プレイヤーと弾の試し用のシーン。WASD・矢印キーかドラッグでプレイヤーを動かし、一番近い敵に自動で撃つ。
    /// 敵は一定の間隔で予兆→出現（_enemyKinds を順に）。フィールド（壁と柱）はシーンに置いたプレハブ。
    /// </summary>
    public sealed class SandboxAScene : SceneBase
    {
        [SerializeField] PlayerView _playerPrefab;
        [SerializeField] PlayerProjectileView _shotPrefab;
        [SerializeField] PlayerStats _playerStats;
        [SerializeField] PlayerVisual _playerVisual;
        [SerializeField] WeaponCatalog _weapons;
        [SerializeField, Tooltip("試す武器")] WeaponId _weapon;
        [SerializeField] ShotEffectView _shotEffectPrefab;
        [SerializeField] Vector2 _playerStart = new Vector2(0f, -6f);
        [SerializeField] SpawnTelegraphView _telegraphPrefab;
        [SerializeField] SpawnTelegraphSettings _telegraphSettings;
        [SerializeField] AttackTelegraphView _attackTelegraphPrefab;
        [SerializeField, Tooltip("出す敵を順に使う")] EnemyDefinition[] _enemyKinds;
        [SerializeField, Tooltip("これだけ敵がいるときは新しく出さない")] int _maxEnemies = 3;
        [SerializeField] Vector2[] _spawnPoints;
        [SerializeField] float _spawnInterval = 3f;
        [SerializeField] float _telegraphSeconds = 0.8f;

        EnemyRegistry _registry;
        PlayerProjectileSystem _shots;
        PlayerSystem _player;
        EnemyProjectileSystem _enemyShots;
        EnemySystem _enemies;
        float _spawnTimer;
        int _nextKind;
        int _nextSpawnPoint;

        public override void Init(GameContext context)
        {
            _registry = new EnemyRegistry();
            var weapon = _weapons.DefinitionOf(_weapon);
            _shots = new PlayerProjectileSystem(_registry, _shotPrefab, _shotEffectPrefab);
            _player = new PlayerSystem(_playerPrefab, _playerStart, _playerStats, _playerVisual.For(weapon.Id), weapon.Create(), new PlayerMoveInput(), _registry, _shots);
            _enemyShots = new EnemyProjectileSystem(_player);
            _enemies = new EnemySystem(_registry, _telegraphPrefab, _telegraphSettings, _attackTelegraphPrefab, _enemyShots, _player, _player);
            _player.Damaged += HandleDamaged;
            _player.Died += HandleDied;
        }

        public override void Tick(float deltaTime)
        {
            _player.Tick(deltaTime);
            TickSpawn(deltaTime);
            _enemies.Tick(deltaTime);
            _shots.Tick(deltaTime);
            _enemyShots.Tick(deltaTime);
        }

        public override void Dispose()
        {
            _player.Died -= HandleDied;
            _player.Damaged -= HandleDamaged;
            _enemies.Dispose();
            _enemyShots.Dispose();
            _player.Dispose();
            _shots.Dispose();
            _registry.Clear();
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

        void HandleDamaged(int amount) => Debug.Log($"Player HP {_player.Hp}/{_player.MaxHp} (-{amount})");
        void HandleDied() => Debug.Log("Player died");
    }
}

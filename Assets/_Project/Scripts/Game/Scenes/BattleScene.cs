using UnityEngine;

namespace PictoArcher.Game
{
    /// <summary>
    /// 1回のプレイのシーン。ここがお手本の形：
    /// Init で System を作る → Tick で作った順に1行ずつ呼ぶ → Dispose で逆の順に片付ける。
    /// ウェーブを順に戦い、死ぬか 50 ウェーブを全部クリアしたら結果の画面へ移る。⏸ で Pause を重ねる。
    /// </summary>
    public sealed class BattleScene : SceneBase
    {
        [SerializeField] UiButtonRelay _pauseButton;
        [SerializeField] RunDefinition _run;
        [SerializeField] PlayerView _playerPrefab;
        [SerializeField] PlayerProjectileView _shotPrefab;
        [SerializeField] PlayerStats _playerStats;
        [SerializeField] PlayerVisual _playerVisual;
        [SerializeField] SpriteVisual _shotVisual;
        [SerializeField] SpawnTelegraphView _telegraphPrefab;
        [SerializeField] SpawnTelegraphSettings _telegraphSettings;
        [SerializeField] AttackTelegraphView _attackTelegraphPrefab;
        [SerializeField] HudView _hud;
        [SerializeField] HeadHpView _headHpPrefab;
        [SerializeField] ObstacleView _obstaclePrefab;
        [SerializeField] ObstacleSwitchSettings _obstacleSettings;
        [SerializeField, Tooltip("フィールドの Obstacles の子")] Transform _obstacleRoot;
        [SerializeField] PickupView _pickupPrefab;
        [SerializeField] PickupSettings _pickupSettings;

        GameContext _context;
        EnemyRegistry _enemies;
        PlayerProjectileSystem _shots;
        PlayerSystem _player;
        ObstacleSystem _obstacles;
        EnemyProjectileSystem _enemyShots;
        EnemySystem _enemySystem;
        WaveSpawnSystem _waves;
        PickupSystem _pickups;
        RunSystem _runSystem;
        HudSystem _hudSystem;
        bool _leaving;

        public override void Init(GameContext context)
        {
            _context = context;
            _pauseButton.Init();
            _enemies = new EnemyRegistry();
            _shots = new PlayerProjectileSystem(_enemies, _shotPrefab);
            _player = new PlayerSystem(_playerPrefab, _run.PlayerStart, _playerStats, _playerVisual.Gun, _shotVisual, new PlayerMoveInput(), _enemies, _shots);
            _obstacles = new ObstacleSystem(_obstaclePrefab, _obstacleRoot, _obstacleSettings, _player);
            _enemyShots = new EnemyProjectileSystem(_player);
            _enemySystem = new EnemySystem(_enemies, _telegraphPrefab, _telegraphSettings, _attackTelegraphPrefab, _enemyShots, _player, _player);
            _waves = new WaveSpawnSystem(_enemySystem);
            _pickups = new PickupSystem(_pickupPrefab, _pickupSettings, _enemySystem, _player);
            _waves.Cleared += HandleWaveCleared;
            _runSystem = new RunSystem(_run, _waves, _player);
            _hudSystem = new HudSystem(_hud, _headHpPrefab, _player, _player, _runSystem);
            _runSystem.RestStarted += HandleRestStarted;
            _obstacles.ShowImmediately(_run.WaveFor(_run.StartWave).ObstacleLayout);
            _runSystem.Begin();
        }

        public override void Tick(float deltaTime)
        {
            if (_pauseButton.ConsumeClicked()) _context.OpenOverlay(OverlayId.Pause);
            // 作った順に1行ずつ呼ぶ。順番がそのまま実行順になる。
            _player.Tick(deltaTime);
            _obstacles.Tick(deltaTime);
            _enemySystem.Tick(deltaTime);
            _waves.Tick(deltaTime);
            _pickups.Tick(deltaTime);
            _shots.Tick(deltaTime);
            _enemyShots.Tick(deltaTime);
            _runSystem.Tick(deltaTime);
            _hudSystem.Tick(deltaTime);
            if (_runSystem.IsOver && !_leaving) _leaving = _context.FinishRun(NewResult());
        }

        // 武器・強化は、それぞれのタスク（10・05）で本物の値につなぐ。今は銃・なし。
        // クリアしたときは、全滅で吸い寄せ中のコインも足す（拾い終わる前に結果へ移るため）。
        RunResult NewResult() => new RunResult(
            _runSystem.IsCleared ? RunOutcome.Cleared : RunOutcome.Died,
            _runSystem.CurrentWave,
            _runSystem.ElapsedSeconds,
            _pickups.Coins + (_runSystem.IsCleared ? _pickups.PendingCoins : 0),
            WeaponId.Gun,
            null);

        // ウェーブを全滅させたら、落ちている経験値とコインを全部吸い寄せる。
        void HandleWaveCleared() => _pickups.AttractAll();

        // 休みに入ったら、次のウェーブの障害物の並びへ切り替える（空なら今のまま）。
        void HandleRestStarted(int nextWave) => _obstacles.SwitchTo(_run.WaveFor(nextWave).ObstacleLayout);

        public override void OnOverlayClosed(OverlayId id, int result)
        {
            // 止まっている間（Pause の読み込み中）に ⏸ をもう一度押していても、閉じた直後に開き直さないよう捨てる。
            _pauseButton.ConsumeClicked();
        }

        public override void Dispose()
        {
            // 作った順の逆に片付ける。
            _hudSystem.Dispose();
            _runSystem.RestStarted -= HandleRestStarted;
            _runSystem.Dispose();
            _waves.Cleared -= HandleWaveCleared;
            _pickups.Dispose();
            _waves.Dispose();
            _enemySystem.Dispose();
            _enemyShots.Dispose();
            _obstacles.Dispose();
            _player.Dispose();
            _shots.Dispose();
            _enemies.Clear();
            _pauseButton.Dispose();
        }
    }
}

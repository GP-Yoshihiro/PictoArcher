using UnityEngine;

namespace PictoArcher.Game
{
    /// <summary>
    /// 1回のプレイのシーン。ここがお手本の形：
    /// Init で System を作る → Tick で作った順に1行ずつ呼ぶ → Dispose で逆の順に片付ける。
    /// System はまだない。各タスクで、ここに1行ずつ足す。⏸ で Pause を重ねる。
    /// </summary>
    public sealed class BattleScene : SceneBase
    {
        [SerializeField] UiButtonRelay _pauseButton;

        GameContext _context;
        EnemyRegistry _enemies;

        public override void Init(GameContext context)
        {
            _context = context;
            _pauseButton.Init();
            _enemies = new EnemyRegistry();
            // 例：_input = new PlayerInputSystem(...);
            //     _player = new PlayerSystem(_input, ...);
            //     _enemySystem = new EnemySystem(_enemies, ...);
        }

        public override void Tick(float deltaTime)
        {
            if (_pauseButton.ConsumeClicked()) _context.OpenOverlay(OverlayId.Pause);
            // 作った順に1行ずつ呼ぶ。順番がそのまま実行順になる。
            // 例：_input.Tick(deltaTime);
            //     _player.Tick(deltaTime);
            //     _enemySystem.Tick(deltaTime);
        }

        public override void Dispose()
        {
            // 作った順の逆に片付ける。
            _enemies.Clear();
            _pauseButton.Dispose();
        }
    }
}

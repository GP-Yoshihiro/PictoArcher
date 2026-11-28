using UnityEngine;

namespace PictoArcher.Game
{
    /// <summary>ホームのシーン。▶ と最高記録を出し、▶ が押されたら Battle へ進む。</summary>
    public sealed class HomeScene : SceneBase
    {
        [SerializeField] UiButtonRelay _startButton;
        [SerializeField] UiIconVisual _icons;
        [SerializeField] HomeView _view;

        GameContext _context;

        public override void Init(GameContext context)
        {
            _context = context;
            _startButton.Init();
            _view.Show(context.Records, _icons);
        }

        public override void Tick(float deltaTime)
        {
            if (_startButton.ConsumeClicked()) _context.RequestScene(SceneId.Battle);
        }

        public override void Dispose()
        {
            _startButton.Dispose();
        }
    }
}

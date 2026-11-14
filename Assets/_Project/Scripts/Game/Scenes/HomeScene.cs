using UnityEngine;

namespace PictoArcher.Game
{
    /// <summary>ホームのシーン。今は開始ボタン（▶）だけ。押されたら Battle へ進む。</summary>
    public sealed class HomeScene : SceneBase
    {
        [SerializeField] UiButtonRelay _startButton;

        GameContext _context;

        public override void Init(GameContext context)
        {
            _context = context;
            _startButton.Init();
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

using UnityEngine;

namespace PictoArcher.Game
{
    /// <summary>結果のシーン。🏠 で Home へ、↻ でもう一度 Battle へ。</summary>
    public sealed class ResultScene : SceneBase
    {
        [SerializeField] UiButtonRelay _homeButton;
        [SerializeField] UiButtonRelay _retryButton;

        GameContext _context;

        public override void Init(GameContext context)
        {
            _context = context;
            _homeButton.Init();
            _retryButton.Init();
        }

        public override void Tick(float deltaTime)
        {
            if (_homeButton.ConsumeClicked()) _context.RequestScene(SceneId.Home);
            else if (_retryButton.ConsumeClicked()) _context.RequestScene(SceneId.Battle);
        }

        public override void Dispose()
        {
            _retryButton.Dispose();
            _homeButton.Dispose();
        }
    }
}

using UnityEngine;

namespace PictoArcher.Game
{
    /// <summary>一時停止の重ねシーン。開いている間は下のシーンが止まる。▶ で閉じて戻り、🏠 で Home へ。</summary>
    public sealed class PauseScene : SceneBase
    {
        [SerializeField] UiButtonRelay _resumeButton;
        [SerializeField] UiButtonRelay _homeButton;

        GameContext _context;

        public override void Init(GameContext context)
        {
            _context = context;
            _resumeButton.Init();
            _homeButton.Init();
        }

        public override void Tick(float deltaTime)
        {
            if (_resumeButton.ConsumeClicked()) _context.CloseOverlay(0);
            else if (_homeButton.ConsumeClicked()) _context.RequestScene(SceneId.Home);
        }

        public override void Dispose()
        {
            _homeButton.Dispose();
            _resumeButton.Dispose();
        }
    }
}

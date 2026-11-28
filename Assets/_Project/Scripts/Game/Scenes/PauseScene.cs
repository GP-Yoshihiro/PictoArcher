using UnityEngine;
using UnityEngine.UI;

namespace PictoArcher.Game
{
    /// <summary>
    /// 一時停止の重ねシーン。開いている間は下のシーンが止まる。
    /// ▶（または Esc）で閉じて戻り、↻ で同じ武器のまま Battle をはじめからやり直し、🏠 で Home へ。
    /// </summary>
    public sealed class PauseScene : SceneBase
    {
        [SerializeField] UiButtonRelay _resumeButton;
        [SerializeField] UiButtonRelay _restartButton;
        [SerializeField] UiButtonRelay _homeButton;
        [SerializeField] UiIconVisual _icons;
        [SerializeField] Image _resumeIcon;
        [SerializeField] Image _restartIcon;
        [SerializeField] Image _homeIcon;

        GameContext _context;
        PauseKeyInput _pauseKey;
        bool _leaving;

        public override void Init(GameContext context)
        {
            _context = context;
            _pauseKey = new PauseKeyInput();
            _resumeButton.Init();
            _restartButton.Init();
            _homeButton.Init();
            _resumeIcon.sprite = _icons.Play;
            _restartIcon.sprite = _icons.Retry;
            _homeIcon.sprite = _icons.Home;
        }

        public override void Tick(float deltaTime)
        {
            // 依頼は1フレームに1つ。受け付けられたら、閉じるまでほかのボタンを受けない
            if (_leaving) return;
            if (_resumeButton.ConsumeClicked() || _pauseKey.WasPressed()) _leaving = _context.CloseOverlay(0);
            else if (_restartButton.ConsumeClicked()) _leaving = _context.RequestScene(SceneId.Battle);
            else if (_homeButton.ConsumeClicked()) _leaving = _context.RequestScene(SceneId.Home);
        }

        public override void Dispose()
        {
            _homeButton.Dispose();
            _restartButton.Dispose();
            _resumeButton.Dispose();
        }
    }
}

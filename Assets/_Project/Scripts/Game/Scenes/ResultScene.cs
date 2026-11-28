using UnityEngine;
using UnityEngine.UI;

namespace PictoArcher.Game
{
    /// <summary>結果のシーン。クリアか死亡の板を出し、🏠 で Home へ、↻ でもう一度 Battle へ。</summary>
    public sealed class ResultScene : SceneBase
    {
        [SerializeField] UiButtonRelay _homeButton;
        [SerializeField] UiButtonRelay _retryButton;
        [SerializeField] UiIconVisual _icons;
        [SerializeField] ClearView _clearView;
        [SerializeField] DefeatView _defeatView;
        [SerializeField] Image _retryIcon;
        [SerializeField] Image _homeIcon;

        GameContext _context;

        public override void Init(GameContext context)
        {
            _context = context;
            _homeButton.Init();
            _retryButton.Init();
            _retryIcon.sprite = _icons.Retry;
            _homeIcon.sprite = _icons.Home;

            // 結果なしで開かれたとき（外からの RequestScene）は、どちらの板も出さない
            var run = context.LastRun;
            if (run != null && run.IsCleared) _clearView.Show(run, _icons);
            else _clearView.Hide();
            if (run != null && !run.IsCleared) _defeatView.Show(run, _icons);
            else _defeatView.Hide();
        }

        public override void Tick(float deltaTime)
        {
            if (_homeButton.ConsumeClicked()) _context.RequestScene(SceneId.Home);
            else if (_retryButton.ConsumeClicked()) _context.RequestScene(SceneId.Battle);
        }

        public override void Dispose()
        {
            _defeatView.Dispose();
            _retryButton.Dispose();
            _homeButton.Dispose();
        }
    }
}

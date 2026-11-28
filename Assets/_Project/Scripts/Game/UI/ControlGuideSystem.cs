using PictoArcher.Core;

namespace PictoArcher.Game
{
    /// <summary>
    /// 戦いの始めに操作案内を出し、プレイヤーが動いたら消す（最小版。2回目以降に出さない扱いはチュートリアルで足す）。
    /// </summary>
    public sealed class ControlGuideSystem : ITickable
    {
        readonly ControlGuideView _view;
        readonly ITarget _player;
        readonly ControlGuide _guide;
        bool _hidden;

        public ControlGuideSystem(ControlGuideView view, UiIconVisual icons, ITarget player)
        {
            _view = view;
            _player = player;
            _view.Init(icons);
            _guide = new ControlGuide(player.Position, view.DismissDistance, view.StepSeconds);
        }

        public void Tick(float deltaTime)
        {
            if (_hidden) return;
            _guide.Tick(deltaTime, _player.Position);
            if (_guide.IsVisible)
            {
                _view.Show(_guide.HighlightedKey, _guide.DragOffset);
                return;
            }
            _view.Hide();
            _hidden = true;
        }
    }
}

using PictoArcher.Core;

namespace PictoArcher.Game
{
    /// <summary>Main が作ってシーンに渡す、全体で使うもの。必要になったら足す。</summary>
    public sealed class GameContext
    {
        readonly SceneFlow<SceneId, OverlayId> _flow;

        public GameContext(SceneFlow<SceneId, OverlayId> flow, GameClock clock)
        {
            _flow = flow;
            Clock = clock;
        }

        public GameClock Clock { get; }

        /// <summary>中身のシーンを入れ替える。開いている重ねシーンは全部閉じる。受け付けなかったら false（同じフレームの2回目、読み込み中）。</summary>
        public bool RequestScene(SceneId id) => _flow.Request(id);

        /// <summary>重ねシーンを開く。受け付けなかったら false（同じものがもう開いている、同じフレームの2回目、読み込み中）。</summary>
        public bool OpenOverlay(OverlayId id) => _flow.RequestOpenOverlay(id);

        /// <summary>一番上の重ねシーンを閉じ、すぐ下のシーンの OnOverlayClosed に result を渡す。重ねシーンがなければ false。</summary>
        public bool CloseOverlay(int result = 0) => _flow.RequestCloseOverlay(result);
    }
}

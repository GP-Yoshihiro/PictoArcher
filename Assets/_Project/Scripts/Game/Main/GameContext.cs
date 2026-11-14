using PictoArcher.Core;

namespace PictoArcher.Game
{
    /// <summary>Main が作ってシーンに渡す、全体で使うもの。必要になったら足す。</summary>
    public sealed class GameContext
    {
        readonly SceneFlow<SceneId> _flow;

        public GameContext(SceneFlow<SceneId> flow, GameClock clock)
        {
            _flow = flow;
            Clock = clock;
        }

        public GameClock Clock { get; }

        /// <summary>次のシーンを依頼する。受け付けなかったら false（同じフレームの2回目、読み込み中）。</summary>
        public bool RequestScene(SceneId id) => _flow.Request(id);
    }
}

using PictoArcher.Core;

namespace PictoArcher.Game
{
    /// <summary>Main が作ってシーンに渡す、全体で使うもの。必要になったら足す。</summary>
    public sealed class GameContext
    {
        readonly SceneFlow<SceneId, OverlayId> _flow;

        public GameContext(SceneFlow<SceneId, OverlayId> flow, GameClock clock, BestRecord records)
        {
            _flow = flow;
            Clock = clock;
            Records = records;
        }

        public GameClock Clock { get; }

        /// <summary>最高記録。Home が表示し、FinishRun で更新する。</summary>
        public BestRecord Records { get; }

        /// <summary>最後に終わったプレイの結果。まだなければ null。Result が表示する。</summary>
        public RunResult LastRun { get; private set; }

        /// <summary>プレイを終えて Result へ切り替え、結果と最高記録を残す。切り替えを受け付けなかったら false で、何も残さない。</summary>
        public bool FinishRun(RunResult result)
        {
            if (result == null) throw new System.ArgumentNullException(nameof(result));
            if (!_flow.Request(SceneId.Result)) return false;
            LastRun = result;
            Records.Submit(result.ReachedWave, result.IsCleared, result.ClearSeconds);
            return true;
        }

        /// <summary>中身のシーンを入れ替える。開いている重ねシーンは全部閉じる。受け付けなかったら false（同じフレームの2回目、読み込み中）。</summary>
        public bool RequestScene(SceneId id) => _flow.Request(id);

        /// <summary>重ねシーンを開く。受け付けなかったら false（同じものがもう開いている、同じフレームの2回目、読み込み中）。</summary>
        public bool OpenOverlay(OverlayId id) => _flow.RequestOpenOverlay(id);

        /// <summary>一番上の重ねシーンを閉じ、すぐ下のシーンの OnOverlayClosed に result を渡す。重ねシーンがなければ false。</summary>
        public bool CloseOverlay(int result = 0) => _flow.RequestCloseOverlay(result);
    }
}

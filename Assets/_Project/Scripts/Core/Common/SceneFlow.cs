using System;

namespace PictoArcher.Core
{
    /// <summary>
    /// シーンの切り替えの状態。依頼は「動いている」間に1フレーム1つだけ受け付ける。
    /// Unity とのつなぎ（読み込み）は Main が行う。
    /// </summary>
    public sealed class SceneFlow<TId> where TId : struct
    {
        TId _pending;

        public SceneFlowPhase Phase { get; private set; } = SceneFlowPhase.Empty;

        public bool HasPending { get; private set; }

        /// <summary>次のシーンを依頼する。受け付けなかったら false。</summary>
        public bool Request(TId id)
        {
            if (Phase != SceneFlowPhase.Running || HasPending) return false;
            _pending = id;
            HasPending = true;
            return true;
        }

        /// <summary>依頼があれば読み込み中にして、その行き先を返す。</summary>
        public bool TryBeginLoad(out TId id)
        {
            id = _pending;
            if (!HasPending) return false;
            HasPending = false;
            _pending = default;
            Phase = SceneFlowPhase.Loading;
            return true;
        }

        /// <summary>シーンの Init の直前に呼ぶ（Init の中の依頼を受け付けるため）。</summary>
        public void EnterRunning()
        {
            if (Phase == SceneFlowPhase.Running)
                throw new InvalidOperationException("SceneFlow is already running.");
            Phase = SceneFlowPhase.Running;
        }
    }
}

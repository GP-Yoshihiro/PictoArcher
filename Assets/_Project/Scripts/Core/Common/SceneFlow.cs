using System;
using System.Collections.Generic;

namespace PictoArcher.Core
{
    /// <summary>
    /// シーンの依頼と状態。中身のシーン（TScene）は1つ、重ねシーン（TOverlay）はその上に積む。
    /// 依頼は「動いている」間に1フレーム1つだけ受け付ける。Unity とのつなぎ（読み込み）は SceneTransform が行う。
    /// </summary>
    public sealed class SceneFlow<TScene, TOverlay>
        where TScene : struct
        where TOverlay : struct
    {
        static readonly EqualityComparer<TOverlay> OverlayComparer = EqualityComparer<TOverlay>.Default;

        readonly List<TOverlay> _overlays = new List<TOverlay>();
        SceneRequest<TScene, TOverlay> _pending;
        bool _loadingOverlay;

        public SceneFlowPhase Phase { get; private set; } = SceneFlowPhase.Empty;

        public bool HasPending { get; private set; }

        /// <summary>開いている重ねシーンの数（読み込み中のものを含む）。</summary>
        public int OverlayCount => _overlays.Count;

        /// <summary>重ねシーン。0 が一番下。</summary>
        public TOverlay OverlayAt(int index) => _overlays[index];

        public bool IsOverlayOpen(TOverlay overlay)
        {
            for (int i = 0; i < _overlays.Count; i++)
                if (OverlayComparer.Equals(_overlays[i], overlay)) return true;
            return false;
        }

        /// <summary>中身のシーンの入れ替えを依頼する。重ねシーンは全部閉じる。受け付けなかったら false。</summary>
        public bool Request(TScene scene) =>
            Accept(new SceneRequest<TScene, TOverlay>(SceneRequestKind.Change, scene, default, 0));

        /// <summary>重ねシーンを開く依頼。同じものがもう開いているときは false。</summary>
        public bool RequestOpenOverlay(TOverlay overlay)
        {
            if (IsOverlayOpen(overlay)) return false;
            return Accept(new SceneRequest<TScene, TOverlay>(SceneRequestKind.OpenOverlay, default, overlay, 0));
        }

        /// <summary>一番上の重ねシーンを閉じる依頼。重ねシーンがないときは false。</summary>
        public bool RequestCloseOverlay(int result)
        {
            if (_overlays.Count == 0) return false;
            var top = _overlays[_overlays.Count - 1];
            return Accept(new SceneRequest<TScene, TOverlay>(SceneRequestKind.CloseOverlay, default, top, result));
        }

        /// <summary>
        /// 依頼があれば取り出して状態を進める。
        /// Change：重ねシーンを空にして読み込み中に。OpenOverlay：積んで読み込み中に。CloseOverlay：外して動いているまま。
        /// </summary>
        public bool TryBegin(out SceneRequest<TScene, TOverlay> request)
        {
            request = _pending;
            if (!HasPending) return false;
            HasPending = false;
            _pending = default;

            switch (request.Kind)
            {
                case SceneRequestKind.Change:
                    _overlays.Clear();
                    _loadingOverlay = false;
                    Phase = SceneFlowPhase.Loading;
                    break;
                case SceneRequestKind.OpenOverlay:
                    _overlays.Add(request.Overlay);
                    _loadingOverlay = true;
                    Phase = SceneFlowPhase.Loading;
                    break;
                case SceneRequestKind.CloseOverlay:
                    _overlays.RemoveAt(_overlays.Count - 1);
                    break;
            }
            return true;
        }

        /// <summary>読み込んだシーンの Init の直前に呼ぶ（Init の中の依頼を受け付けるため）。</summary>
        public void EnterRunning()
        {
            if (Phase == SceneFlowPhase.Running)
                throw new InvalidOperationException("SceneFlow is already running.");
            Phase = SceneFlowPhase.Running;
            _loadingOverlay = false;
        }

        /// <summary>重ねシーンを開けなかったとき、積んだものを外して動いている状態に戻す。</summary>
        public void CancelOverlayOpen()
        {
            if (Phase != SceneFlowPhase.Loading || !_loadingOverlay)
                throw new InvalidOperationException("No overlay is being opened.");
            _overlays.RemoveAt(_overlays.Count - 1);
            _loadingOverlay = false;
            Phase = SceneFlowPhase.Running;
        }

        /// <summary>外からシーンが変えられたとき、重ねシーンと依頼を捨てて、動いている状態からやり直す。</summary>
        public void Restart()
        {
            _overlays.Clear();
            HasPending = false;
            _pending = default;
            _loadingOverlay = false;
            Phase = SceneFlowPhase.Running;
        }

        bool Accept(SceneRequest<TScene, TOverlay> request)
        {
            if (Phase != SceneFlowPhase.Running || HasPending) return false;
            _pending = request;
            HasPending = true;
            return true;
        }
    }
}

namespace PictoArcher.Core
{
    /// <summary>SceneFlow が受け付けた依頼。Kind によって Scene か Overlay（と Result）を使う。</summary>
    public readonly struct SceneRequest<TScene, TOverlay>
        where TScene : struct
        where TOverlay : struct
    {
        public SceneRequest(SceneRequestKind kind, TScene scene, TOverlay overlay, int result)
        {
            Kind = kind;
            Scene = scene;
            Overlay = overlay;
            Result = result;
        }

        public SceneRequestKind Kind { get; }

        /// <summary>Change の行き先。</summary>
        public TScene Scene { get; }

        /// <summary>OpenOverlay で開くもの、CloseOverlay で閉じたもの。</summary>
        public TOverlay Overlay { get; }

        /// <summary>CloseOverlay で下のシーンに渡す結果。</summary>
        public int Result { get; }
    }
}

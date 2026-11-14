using System;
using PictoArcher.Game;

namespace PictoArcher.Tests.PlayMode
{
    /// <summary>テスト用：GameContext を外に見せ、Tick の回数と、閉じた重ねシーンの結果を記録するシーン。</summary>
    public sealed class HostScene : SceneBase
    {
        public GameContext Context { get; private set; }
        public int TickCount { get; private set; }
        public OverlayId? LastClosed { get; private set; }
        public int LastResult { get; private set; }

        /// <summary>Init の中で呼ばれる（テストが「Init の時点」の様子を調べるため）。</summary>
        public Action OnInit { get; set; }

        public override void Init(GameContext context)
        {
            Context = context;
            OnInit?.Invoke();
        }

        public override void Tick(float deltaTime) => TickCount++;

        public override void Dispose() { }

        public override void OnOverlayClosed(OverlayId id, int result)
        {
            LastClosed = id;
            LastResult = result;
        }
    }
}

using NUnit.Framework;
using PictoArcher.Core;
using PictoArcher.Game;

namespace PictoArcher.Tests.EditMode
{
    public class GameContextTests
    {
        static (SceneFlow<SceneId, OverlayId> flow, GameContext context) Running()
        {
            var flow = new SceneFlow<SceneId, OverlayId>();
            var context = new GameContext(flow, new GameClock());
            flow.EnterRunning();
            return (flow, context);
        }

        [Test] public void RequestScene_ForwardsToFlow()
        {
            var (flow, context) = Running();
            Assert.IsTrue(context.RequestScene(SceneId.Home));
            Assert.IsTrue(flow.TryBegin(out var request));
            Assert.AreEqual(SceneRequestKind.Change, request.Kind);
            Assert.AreEqual(SceneId.Home, request.Scene);
        }

        [Test] public void OpenOverlay_ForwardsToFlow()
        {
            var (flow, context) = Running();
            Assert.IsTrue(context.OpenOverlay(OverlayId.Pause));
            Assert.IsTrue(flow.TryBegin(out var request));
            Assert.AreEqual(SceneRequestKind.OpenOverlay, request.Kind);
            Assert.AreEqual(OverlayId.Pause, request.Overlay);
        }

        [Test] public void CloseOverlay_ForwardsResult()
        {
            var (flow, context) = Running();
            context.OpenOverlay(OverlayId.Pause);
            flow.TryBegin(out _);
            flow.EnterRunning();
            Assert.IsTrue(context.CloseOverlay(3));
            Assert.IsTrue(flow.TryBegin(out var request));
            Assert.AreEqual(SceneRequestKind.CloseOverlay, request.Kind);
            Assert.AreEqual(OverlayId.Pause, request.Overlay);
            Assert.AreEqual(3, request.Result);
        }

        [Test] public void CloseOverlay_WithoutOverlay_ReturnsFalse() =>
            Assert.IsFalse(Running().context.CloseOverlay());
    }
}

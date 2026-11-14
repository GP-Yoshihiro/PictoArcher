using NUnit.Framework;
using PictoArcher.Core;
using PictoArcher.Game;

namespace PictoArcher.Tests.EditMode
{
    public class GameContextTests
    {
        [Test] public void RequestScene_ForwardsToFlow()
        {
            var flow = new SceneFlow<SceneId>();
            var context = new GameContext(flow, new GameClock());
            flow.EnterRunning();
            Assert.IsTrue(context.RequestScene(SceneId.Home));
            Assert.IsTrue(flow.TryBeginLoad(out var id));
            Assert.AreEqual(SceneId.Home, id);
        }
    }
}

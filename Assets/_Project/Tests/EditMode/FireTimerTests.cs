using NUnit.Framework;
using PictoArcher.Core;

namespace PictoArcher.Tests.EditMode
{
    public class FireTimerTests
    {
        [Test] public void FiresImmediatelyWhenCreated() =>
            Assert.IsTrue(new FireTimer(0.5f, 1f).Tick(0f, true, false));

        [Test] public void WaitsForInterval()
        {
            var t = new FireTimer(0.5f, 1f);
            t.Tick(0f, true, false);
            Assert.IsFalse(t.Tick(0.3f, true, false));
            Assert.IsTrue(t.Tick(0.3f, true, false));
        }

        [Test] public void NoTarget_DoesNotFire_ButIsReadyWhenTargetAppears()
        {
            var t = new FireTimer(0.5f, 1f);
            t.Tick(0f, true, false);
            Assert.IsFalse(t.Tick(1f, false, false));
            Assert.IsTrue(t.Tick(0f, true, false));
        }

        [Test] public void LongFrame_FiresOnlyOnce()
        {
            var t = new FireTimer(0.5f, 1f);
            t.Tick(0f, true, false);
            Assert.IsTrue(t.Tick(5f, true, false));
            Assert.IsFalse(t.Tick(0.01f, true, false));
        }

        [Test] public void MovingWithHalfRate_DoublesInterval()
        {
            var t = new FireTimer(0.5f, 0.5f);
            t.Tick(0f, true, true);
            Assert.IsFalse(t.Tick(0.6f, true, true));
            Assert.IsTrue(t.Tick(0.4f, true, true));
        }

        [Test] public void MovingWithFullRate_SameAsStanding()
        {
            var t = new FireTimer(0.5f, 1f);
            t.Tick(0f, true, true);
            Assert.IsTrue(t.Tick(0.5f, true, true));
        }
    }
}

using NUnit.Framework;
using PictoArcher.Core;

namespace PictoArcher.Tests.EditMode
{
    public class FrameClockTests
    {
        [Test] public void Play_StartsAtFirstFrame()
        {
            var clock = new FrameClock();
            clock.Play(4, 8f, true);
            Assert.AreEqual(0, clock.FrameIndex);
            Assert.IsFalse(clock.IsFinished);
        }

        [Test] public void Loop_AdvancesAndWrapsToFirstFrame()
        {
            var clock = new FrameClock();
            clock.Play(4, 8f, true);
            clock.Tick(0.125f);
            Assert.AreEqual(1, clock.FrameIndex);
            clock.Tick(0.375f);
            Assert.AreEqual(0, clock.FrameIndex);
            Assert.IsFalse(clock.IsFinished);
        }

        [Test] public void Loop_LongFrame_KeepsWrapping()
        {
            var clock = new FrameClock();
            clock.Play(4, 8f, true);
            clock.Tick(100.0625f);
            Assert.AreEqual(0, clock.FrameIndex);
            clock.Tick(0.25f);
            Assert.AreEqual(2, clock.FrameIndex);
        }

        [Test] public void Once_StopsOnLastFrameAndFinishes()
        {
            var clock = new FrameClock();
            clock.Play(3, 10f, false);
            clock.Tick(0.15f);
            Assert.AreEqual(1, clock.FrameIndex);
            Assert.IsFalse(clock.IsFinished);
            clock.Tick(1f);
            Assert.AreEqual(2, clock.FrameIndex);
            Assert.IsTrue(clock.IsFinished);
        }

        [Test] public void Play_AgainResetsToFirstFrame()
        {
            var clock = new FrameClock();
            clock.Play(3, 10f, false);
            clock.Tick(1f);
            clock.Play(3, 10f, false);
            Assert.AreEqual(0, clock.FrameIndex);
            Assert.IsFalse(clock.IsFinished);
        }

        [Test] public void ZeroFramesPerSecond_StaysOnFirstFrame()
        {
            var clock = new FrameClock();
            clock.Play(4, 0f, true);
            Assert.DoesNotThrow(() => clock.Tick(1f));
            Assert.AreEqual(0, clock.FrameIndex);
        }

        // やられのアニメを待つ側が止まらないよう、進められない1回きりのアニメはすぐ終わりにする
        [Test] public void ZeroFramesPerSecond_Once_IsFinishedAtOnce()
        {
            var clock = new FrameClock();
            clock.Play(4, 0f, false);
            Assert.IsTrue(clock.IsFinished);
        }

        [Test] public void NoFrames_DoesNotThrow()
        {
            var clock = new FrameClock();
            clock.Play(0, 8f, true);
            Assert.DoesNotThrow(() => clock.Tick(1f));
            Assert.AreEqual(0, clock.FrameIndex);
        }
    }
}

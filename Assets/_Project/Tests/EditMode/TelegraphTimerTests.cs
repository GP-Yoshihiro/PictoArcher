using NUnit.Framework;
using PictoArcher.Core;

namespace PictoArcher.Tests.EditMode
{
    public class TelegraphTimerTests
    {
        [Test] public void StartsUnfinishedAtProgressZero()
        {
            var timer = new TelegraphTimer(0.8f, 4f);
            Assert.IsFalse(timer.IsFinished);
            Assert.AreEqual(0f, timer.Progress, 1e-5f);
        }

        [Test] public void Tick_AdvancesProgress_AndFinishesAtDuration()
        {
            var timer = new TelegraphTimer(0.8f, 4f);
            timer.Tick(0.4f);
            Assert.AreEqual(0.5f, timer.Progress, 1e-4f);
            Assert.IsFalse(timer.IsFinished);
            timer.Tick(0.4f);
            Assert.IsTrue(timer.IsFinished);
            Assert.AreEqual(1f, timer.Progress, 1e-5f);
        }

        [Test] public void Tick_PastDuration_ClampsProgress()
        {
            var timer = new TelegraphTimer(0.8f, 4f);
            timer.Tick(5f);
            Assert.AreEqual(1f, timer.Progress, 1e-5f);
        }

        [Test] public void IsVisible_Blinks()
        {
            var timer = new TelegraphTimer(1f, 4f);   // 1秒に4回点滅 = 0.125秒ごとに表と裏
            Assert.IsTrue(timer.IsVisible);
            timer.Tick(0.13f);
            Assert.IsFalse(timer.IsVisible);
            timer.Tick(0.12f);
            Assert.IsTrue(timer.IsVisible);
        }

        [Test] public void Constructor_NonPositiveDuration_Throws()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() => new TelegraphTimer(0f, 4f));
        }

        [Test] public void Tick_NegativeDelta_Throws()
        {
            var timer = new TelegraphTimer(1f, 4f);
            Assert.Throws<System.ArgumentOutOfRangeException>(() => timer.Tick(-0.1f));
        }
    }
}

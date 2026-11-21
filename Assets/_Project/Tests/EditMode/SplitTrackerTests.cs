using NUnit.Framework;
using PictoArcher.Core;

namespace PictoArcher.Tests.EditMode
{
    public class SplitTrackerTests
    {
        [Test] public void NoCrossing_ReturnsZero()
        {
            var tracker = new SplitTracker(new[] { 0.66f, 0.33f });
            Assert.AreEqual(0, tracker.Update(70, 100));
        }

        [Test] public void CrossingOneThreshold_ReturnsOne_AndOnlyOnce()
        {
            var tracker = new SplitTracker(new[] { 0.66f, 0.33f });
            Assert.AreEqual(1, tracker.Update(60, 100));
            Assert.AreEqual(0, tracker.Update(55, 100), "同じしきい値では2回分裂しない");
        }

        [Test] public void BigHit_CrossingTwoThresholds_ReturnsTwo()
        {
            var tracker = new SplitTracker(new[] { 0.66f, 0.33f });
            Assert.AreEqual(2, tracker.Update(20, 100));
            Assert.AreEqual(0, tracker.Update(10, 100));
        }

        [Test] public void ExactlyOnThreshold_Counts()
        {
            var tracker = new SplitTracker(new[] { 0.5f });
            Assert.AreEqual(1, tracker.Update(50, 100));
        }

        [Test] public void Dead_DoesNotSplit()
        {
            var tracker = new SplitTracker(new[] { 0.5f });
            Assert.AreEqual(0, tracker.Update(0, 100));
        }

        [Test] public void NoThresholds_NeverSplits()
        {
            var tracker = new SplitTracker(new float[0]);
            Assert.AreEqual(0, tracker.Update(1, 100));
        }

        [Test] public void Constructor_OutOfRangeThreshold_Throws()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() => new SplitTracker(new[] { 1.2f }));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => new SplitTracker(new[] { 0f }));
        }

        [Test] public void Update_InvalidMax_Throws()
        {
            var tracker = new SplitTracker(new[] { 0.5f });
            Assert.Throws<System.ArgumentOutOfRangeException>(() => tracker.Update(10, 0));
        }
    }
}

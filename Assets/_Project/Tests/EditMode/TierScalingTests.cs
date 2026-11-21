using NUnit.Framework;
using PictoArcher.Core;

namespace PictoArcher.Tests.EditMode
{
    public class TierScalingTests
    {
        [Test] public void TierZero_KeepsBaseValue()
        {
            Assert.AreEqual(300, TierScaling.Scale(300, 0.5f, 0));
            Assert.AreEqual(2f, TierScaling.Scale(2f, 0.5f, 0), 1e-5f);
        }

        [Test] public void EachTier_AddsGrowthOfBase()
        {
            Assert.AreEqual(450, TierScaling.Scale(300, 0.5f, 1));
            Assert.AreEqual(600, TierScaling.Scale(300, 0.5f, 2));
            Assert.AreEqual(3f, TierScaling.Scale(2f, 0.25f, 2), 1e-5f);
        }

        [Test] public void IntResult_IsRounded()
        {
            Assert.AreEqual(13, TierScaling.Scale(10, 0.25f, 1));   // 12.5 → 13（0 から遠い側）
        }

        [Test] public void NegativeTier_Throws()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() => TierScaling.Scale(10, 0.5f, -1));
        }

        [Test] public void Count_AddsWholeExtraPerTier()
        {
            Assert.AreEqual(2, TierScaling.AddPerTier(2, 1, 0));
            Assert.AreEqual(4, TierScaling.AddPerTier(2, 1, 2));
        }
    }
}

using System;

namespace PictoArcher.Core
{
    /// <summary>
    /// 中ボスやボスが「出るたびに強くなる」ための計算。tier は何回目の登場か（0 が最初）。
    /// 値は元の値に、1回ごとに「元の値 × 伸びの割合」を足す。
    /// </summary>
    public static class TierScaling
    {
        public static float Scale(float baseValue, float growthPerTier, int tier)
        {
            if (tier < 0) throw new ArgumentOutOfRangeException(nameof(tier), "Tier must not be negative.");
            return baseValue * (1f + growthPerTier * tier);
        }

        /// <summary>整数版（HP など）。四捨五入（.5 は 0 から遠い側）。</summary>
        public static int Scale(int baseValue, float growthPerTier, int tier)
        {
            return (int)Math.Round(Scale((float)baseValue, growthPerTier, tier), MidpointRounding.AwayFromZero);
        }

        /// <summary>1回ごとに決まった数を足す（分裂の数など）。</summary>
        public static int AddPerTier(int baseCount, int extraPerTier, int tier)
        {
            if (tier < 0) throw new ArgumentOutOfRangeException(nameof(tier), "Tier must not be negative.");
            return baseCount + extraPerTier * tier;
        }
    }
}

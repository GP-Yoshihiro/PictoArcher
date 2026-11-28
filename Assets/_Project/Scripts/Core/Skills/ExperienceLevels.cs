using System;

namespace PictoArcher.Core
{
    /// <summary>
    /// 経験値とレベル（設計書 §6「レベルアップ」）。レベルは 1 から始まる。
    /// 次のレベルまでに要る量は「最初の量 + 1レベルごとに増える量 ×（今のレベル − 1）」。どちらも調整値。
    /// </summary>
    public sealed class ExperienceLevels
    {
        readonly int _firstRequirement;
        readonly int _growthPerLevel;

        public ExperienceLevels(int firstRequirement, int growthPerLevel)
        {
            if (firstRequirement < 1) throw new ArgumentOutOfRangeException(nameof(firstRequirement), "最初に要る量は 1 以上");
            if (growthPerLevel < 0) throw new ArgumentOutOfRangeException(nameof(growthPerLevel), "増える量は 0 以上");
            _firstRequirement = firstRequirement;
            _growthPerLevel = growthPerLevel;
            Level = 1;
        }

        public int Level { get; private set; }
        /// <summary>今のレベルの中でたまった量。</summary>
        public int Experience { get; private set; }
        public int ToNextLevel => _firstRequirement + _growthPerLevel * (Level - 1);

        /// <summary>経験値を足し、上がったレベルの数を返す（一度に2つ以上上がることもある）。</summary>
        public int Add(int amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount), "足す量は 0 以上");
            Experience += amount;
            int gained = 0;
            while (Experience >= ToNextLevel)
            {
                Experience -= ToNextLevel;
                Level++;
                gained++;
            }
            return gained;
        }
    }
}

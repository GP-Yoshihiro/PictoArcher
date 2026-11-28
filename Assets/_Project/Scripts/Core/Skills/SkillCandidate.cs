using System;

namespace PictoArcher.Core
{
    /// <summary>
    /// 抽選に入る強化カード1枚の決まり（設計書 §9）。
    /// MaxStacks が 0 なら上限なし（何度でも取れる）。Weight が 0 なら抽選に出ない。
    /// </summary>
    public readonly struct SkillCandidate
    {
        public SkillCandidate(int id, int maxStacks, int weight)
        {
            if (maxStacks < 0) throw new ArgumentOutOfRangeException(nameof(maxStacks), "重ね取りの上限は 0 以上（0 は上限なし）");
            if (weight < 0) throw new ArgumentOutOfRangeException(nameof(weight), "重みは 0 以上");
            Id = id;
            MaxStacks = maxStacks;
            Weight = weight;
        }

        public int Id { get; }
        public int MaxStacks { get; }
        public int Weight { get; }
        public bool IsUnlimited => MaxStacks == 0;
    }
}

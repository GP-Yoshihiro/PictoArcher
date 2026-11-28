namespace PictoArcher.Core
{
    /// <summary>
    /// レベルアップで出るカード1枚。強化のカードか、強化が尽きたときのコインのカード。
    /// NextStack は選んだら届く段階、MaxStacks はその上限（0 は上限なしで ★ を出さない）。
    /// </summary>
    public readonly struct LevelUpCard
    {
        LevelUpCard(bool isCoin, int skillId, int nextStack, int maxStacks)
        {
            IsCoin = isCoin;
            SkillId = skillId;
            NextStack = nextStack;
            MaxStacks = maxStacks;
        }

        public static LevelUpCard Skill(int skillId, int nextStack, int maxStacks) => new LevelUpCard(false, skillId, nextStack, maxStacks);
        public static LevelUpCard Coin() => new LevelUpCard(true, -1, 0, 0);

        public bool IsCoin { get; }
        /// <summary>強化の id。コインのカードでは -1。</summary>
        public int SkillId { get; }
        public int NextStack { get; }
        public int MaxStacks { get; }
    }
}

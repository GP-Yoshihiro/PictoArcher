namespace PictoArcher.Core
{
    /// <summary>攻撃の周期のどこにいるか。</summary>
    public enum AttackPhase
    {
        /// <summary>次の予兆までの間。</summary>
        Waiting,
        /// <summary>赤い線・赤い円で予告している間。終わると攻撃する。</summary>
        Telegraphing,
    }
}

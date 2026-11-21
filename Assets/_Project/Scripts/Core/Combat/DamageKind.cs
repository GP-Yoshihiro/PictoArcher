namespace PictoArcher.Core
{
    /// <summary>ダメージの出どころ。ダメージ数字の色や、強化の効き方を分けるのに使う。</summary>
    public enum DamageKind
    {
        /// <summary>弾・レーザー・体当たりなど、直接当たったもの。</summary>
        Direct,
        /// <summary>ランチャーや「敵の死亡時に爆発」の範囲。</summary>
        Explosion,
        /// <summary>雷の連鎖。</summary>
        Lightning,
        /// <summary>炎の継続ダメージ。</summary>
        Burn,
        /// <summary>毒の継続ダメージ。</summary>
        Poison,
    }
}

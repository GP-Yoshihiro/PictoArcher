namespace PictoArcher.Game
{
    /// <summary>HUD が読む1回のプレイの育ち（上の帯の経験値バー、★n、コイン数）。経験値とコインの System（A さんの側）が実装し、HUD は毎フレーム読むだけ。</summary>
    public interface IRunStatus
    {
        /// <summary>今のレベル（★n の n）。1 から始まる。</summary>
        int Level { get; }
        /// <summary>今のレベルの中でたまった経験値。</summary>
        int Experience { get; }
        /// <summary>次のレベルまでに要る経験値（バーの長さの分母）。</summary>
        int ExperienceToNextLevel { get; }
        int Coins { get; }
    }
}

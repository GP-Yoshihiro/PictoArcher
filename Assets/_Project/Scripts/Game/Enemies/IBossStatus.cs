namespace PictoArcher.Game
{
    /// <summary>画面の上のボスの HP バーが読む値。EnemySystem が実装する。</summary>
    public interface IBossStatus
    {
        /// <summary>HP バーを出す敵（EnemyDefinition.ShowsBossBar）が生きて出ているか。</summary>
        bool HasBoss { get; }
        int BossHp { get; }
        int BossMaxHp { get; }
    }
}

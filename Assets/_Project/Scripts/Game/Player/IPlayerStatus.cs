namespace PictoArcher.Game
{
    /// <summary>HUD が読むプレイヤーの体の状態（頭上の HP バーと数字）。プレイヤーの System（A さんの側）が実装し、HUD は毎フレーム読むだけ。</summary>
    public interface IPlayerStatus
    {
        int Hp { get; }
        int MaxHp { get; }
        bool IsDead { get; }
        /// <summary>被弾のあとの無敵時間の間は true（点滅に使う）。</summary>
        bool IsInvulnerable { get; }
    }
}

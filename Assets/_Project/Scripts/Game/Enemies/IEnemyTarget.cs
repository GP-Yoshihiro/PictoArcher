namespace PictoArcher.Game
{
    /// <summary>EnemyRegistry に登録する敵1体。狙われ、当たり、状態異常を受ける。敵の動く部品の持ち主（B さんの側）が実装する。</summary>
    public interface IEnemyTarget : ITarget, IHittable, IStatusReceiver
    {
    }
}

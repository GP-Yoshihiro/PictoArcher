namespace PictoArcher.Game
{
    /// <summary>敵の動きと攻撃の種類の定義の親。新しい動きは、これを継承した定義と動く部品（IEnemyBehavior）を1組足す。</summary>
    public abstract class EnemyBehaviorDefinition : DefinitionAsset<IEnemyBehavior>
    {
    }
}

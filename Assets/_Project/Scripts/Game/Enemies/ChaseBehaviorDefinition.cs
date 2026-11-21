using UnityEngine;

namespace PictoArcher.Game
{
    /// <summary>プレイヤーを追って体当たりする動き。速さは EnemyDefinition.MoveSpeed。</summary>
    [CreateAssetMenu(menuName = "PictoArcher/Enemy/ChaseBehavior")]
    public sealed class ChaseBehaviorDefinition : EnemyBehaviorDefinition
    {
        public override IEnemyBehavior Create() => new ChaseBehavior();
    }
}

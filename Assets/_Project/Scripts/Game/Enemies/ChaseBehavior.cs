using PictoArcher.Core;
using UnityEngine;

namespace PictoArcher.Game
{
    /// <summary>プレイヤーへまっすぐ向かう。</summary>
    public sealed class ChaseBehavior : IEnemyBehavior
    {
        ChaseMovement _movement;

        public Vector2 Tick(float deltaTime, in EnemyContext context, IEnemyAttackPort attacks)
        {
            if (_movement == null) _movement = new ChaseMovement(context.MoveSpeed);
            return _movement.Velocity(context.Position, context.TargetPosition);
        }
    }
}

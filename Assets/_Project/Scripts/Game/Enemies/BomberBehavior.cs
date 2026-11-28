using PictoArcher.Core;
using UnityEngine;

namespace PictoArcher.Game
{
    /// <summary>突進 → 近づいたら止まる → 赤い円が広がる → 自爆。円が広がり始めたら、あとは必ず爆発する。</summary>
    public sealed class BomberBehavior : IEnemyBehavior
    {
        readonly BomberBehaviorDefinition _definition;
        readonly AttackCycle _fuse;
        ChaseMovement _movement;
        bool _triggered;

        public BomberBehavior(BomberBehaviorDefinition definition)
        {
            _definition = definition;
            _fuse = new AttackCycle(0f, definition.FuseSeconds);
        }

        public Vector2 Tick(float deltaTime, in EnemyContext context, IEnemyAttackPort attacks)
        {
            if (!_triggered && (context.TargetPosition - context.Position).magnitude <= _definition.TriggerRange) _triggered = true;
            if (!_triggered)
            {
                if (_movement == null) _movement = new ChaseMovement(context.MoveSpeed);
                return _movement.Velocity(context.Position, context.ChasePoint);
            }

            if (_fuse.Tick(deltaTime)) attacks.SelfDestruct(_definition.BlastRadius, _definition.BlastDamage);
            else attacks.ShowCircle(context.Position, _definition.BlastRadius, _fuse.Progress);
            return Vector2.zero;
        }
    }
}

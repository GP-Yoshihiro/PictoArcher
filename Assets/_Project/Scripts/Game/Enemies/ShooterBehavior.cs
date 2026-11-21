using PictoArcher.Core;
using UnityEngine;

namespace PictoArcher.Game
{
    /// <summary>
    /// 射程まで近づく → 止まる → 待つ → 赤い線（向きは予兆が始まった時点で決まる）→ 撃つ、を繰り返す。
    /// 予兆の間は動かない。予兆のあとに必ず撃つ。
    /// </summary>
    public sealed class ShooterBehavior : IEnemyBehavior
    {
        readonly ShooterBehaviorDefinition _definition;
        readonly AttackCycle _cycle;
        ChaseMovement _movement;
        Vector2 _aim = Vector2.right;

        public ShooterBehavior(ShooterBehaviorDefinition definition)
        {
            _definition = definition;
            _cycle = new AttackCycle(definition.Interval, definition.WindupSeconds);
        }

        public Vector2 Tick(float deltaTime, in EnemyContext context, IEnemyAttackPort attacks)
        {
            var toTarget = context.TargetPosition - context.Position;
            if (_cycle.Phase == AttackPhase.Waiting)
            {
                if (toTarget.magnitude > _definition.AttackRange)
                {
                    _cycle.Reset();
                    if (_movement == null) _movement = new ChaseMovement(context.MoveSpeed);
                    return _movement.Velocity(context.Position, context.TargetPosition);
                }
                if (toTarget.sqrMagnitude > 1e-8f) _aim = toTarget.normalized;   // 予兆が始まる瞬間まで狙い直し、始まったら固定する
            }

            if (_cycle.Tick(deltaTime)) Fire(attacks);
            else if (_cycle.Phase == AttackPhase.Telegraphing) ShowLines(context.Position, attacks);
            return Vector2.zero;
        }

        void ShowLines(Vector2 origin, IEnemyAttackPort attacks)
        {
            for (int i = 0; i < _definition.ProjectileCount; i++)
            {
                var direction = FanPattern.Direction(_aim, i, _definition.ProjectileCount, _definition.SpreadDegrees);
                attacks.ShowLine(i, origin, direction, _definition.LineLength, _definition.LineWidth, _cycle.Progress);
            }
        }

        void Fire(IEnemyAttackPort attacks)
        {
            for (int i = 0; i < _definition.ProjectileCount; i++)
            {
                var direction = FanPattern.Direction(_aim, i, _definition.ProjectileCount, _definition.SpreadDegrees);
                attacks.Shoot(_definition.Projectile, direction);
            }
        }
    }
}

using PictoArcher.Core;
using UnityEngine;

namespace PictoArcher.Game
{
    /// <summary>
    /// 近づく → 突進の予告（向きを決めて止まり、赤い線を満たす）→ 突進 → 近づく → 雑魚を呼ぶ（予兆の間は止まる）→ はじめに戻る。
    /// 突進の向きは予告の始めに決め、そのあと相手が動いても変えない（よけられるように）。
    /// </summary>
    public sealed class ChargeSummonBehavior : IEnemyBehavior
    {
        // 秒数を足していくと誤差で少し足りなくなるので、この分は終わったとみなす
        const float TimeEpsilon = 1e-4f;

        enum Phase { Approach, ChargeWindup, Charge, Summon }

        readonly ChargeSummonBehaviorDefinition _definition;
        ChaseMovement _movement;
        Phase _phase = Phase.Approach;
        bool _summonNext;
        float _elapsed;
        Vector2 _chargeDirection;

        public ChargeSummonBehavior(ChargeSummonBehaviorDefinition definition) => _definition = definition;

        public Vector2 Tick(float deltaTime, in EnemyContext context, IEnemyAttackPort attacks)
        {
            if (_elapsed >= DurationOf(_phase) - TimeEpsilon) Advance(context, attacks);
            _elapsed += deltaTime;
            switch (_phase)
            {
                case Phase.Approach:
                    if (_movement == null) _movement = new ChaseMovement(context.MoveSpeed);
                    return _movement.Velocity(context.Position, context.TargetPosition);
                case Phase.ChargeWindup:
                    float progress = Mathf.Clamp01(_elapsed / _definition.ChargeWindupSeconds);
                    attacks.ShowLine(0, context.Position, _chargeDirection, _definition.ChargeDistance, _definition.ChargeLineWidth, progress);
                    return Vector2.zero;
                case Phase.Charge:
                    return _chargeDirection * _definition.ChargeSpeed;
                default:
                    return Vector2.zero;
            }
        }

        float DurationOf(Phase phase)
        {
            switch (phase)
            {
                case Phase.Approach: return _definition.ApproachSeconds;
                case Phase.ChargeWindup: return _definition.ChargeWindupSeconds;
                case Phase.Charge: return _definition.ChargeDistance / _definition.ChargeSpeed;
                default: return _definition.SummonWindupSeconds;
            }
        }

        void Advance(in EnemyContext context, IEnemyAttackPort attacks)
        {
            _elapsed = 0f;
            switch (_phase)
            {
                case Phase.Approach:
                    if (_summonNext) BeginSummon(context, attacks);
                    else BeginChargeWindup(context);
                    _summonNext = !_summonNext;
                    break;
                case Phase.ChargeWindup:
                    _phase = Phase.Charge;
                    break;
                default:
                    _phase = Phase.Approach;
                    break;
            }
        }

        void BeginChargeWindup(in EnemyContext context)
        {
            _phase = Phase.ChargeWindup;
            var toTarget = context.TargetPosition - context.Position;
            _chargeDirection = toTarget.sqrMagnitude > 0f ? toTarget.normalized : Vector2.down;
        }

        // ボスを囲むように等間隔で出す。始めの向きは相手への向きの横にして、相手との間をふさがない
        void BeginSummon(in EnemyContext context, IEnemyAttackPort attacks)
        {
            _phase = Phase.Summon;
            int count = _definition.SummonCount;
            if (_definition.SummonEnemy == null || count <= 0) return;
            var toTarget = context.TargetPosition - context.Position;
            float baseAngle = Mathf.Atan2(toTarget.y, toTarget.x) + Mathf.PI * 0.5f;
            var area = _definition.SummonArea;
            for (int i = 0; i < count; i++)
            {
                float angle = baseAngle + Mathf.PI * 2f * i / count;
                var position = context.Position + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * _definition.SummonDistance;
                position.x = Mathf.Clamp(position.x, area.xMin, area.xMax);
                position.y = Mathf.Clamp(position.y, area.yMin, area.yMax);
                attacks.Summon(_definition.SummonEnemy, position, _definition.SummonWindupSeconds);
            }
        }
    }
}

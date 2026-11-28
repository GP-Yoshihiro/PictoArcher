using PictoArcher.Core;
using UnityEngine;

namespace PictoArcher.Game
{
    /// <summary>
    /// 近づく → 突進の予告（向きを決めて止まり、赤い線を満たす）→ 突進 → 近づく → 弾幕の予告（光りながら全方向に赤い線）→ 撃つ
    /// → 近づく → 雑魚を呼ぶ（予兆の間は止まる）→ はじめに戻る。
    /// 突進と弾幕の向きは予告の始めに決め、そのあと相手が動いても変えない（よけられるように）。
    /// HP が半分を切ったら、そのあとずっと「変わったあと」の値を使う。
    /// </summary>
    public sealed class FinalBossBehavior : IEnemyBehavior
    {
        // 秒数を足していくと誤差で少し足りなくなるので、この分は終わったとみなす
        const float TimeEpsilon = 1e-4f;

        enum Phase { Approach, ChargeWindup, Charge, BarrageWindup, Summon }
        enum Attack { Charge, Barrage, Summon }

        readonly FinalBossBehaviorDefinition _definition;
        ChaseMovement _movement;
        Phase _phase = Phase.Approach;
        Attack _next = Attack.Charge;
        float _elapsed;
        Vector2 _aim;
        int _barrageCount;
        bool _enraged;
        bool _tintApplied;

        public FinalBossBehavior(FinalBossBehaviorDefinition definition) => _definition = definition;

        public Vector2 Tick(float deltaTime, in EnemyContext context, IEnemyAttackPort attacks)
        {
            UpdateEnrage(context, attacks);
            if (_elapsed >= DurationOf(_phase) - TimeEpsilon) Advance(context, attacks);
            _elapsed += deltaTime;
            switch (_phase)
            {
                case Phase.Approach:
                    if (_movement == null) _movement = new ChaseMovement(context.MoveSpeed);
                    return _movement.Velocity(context.Position, context.ChasePoint);
                case Phase.ChargeWindup:
                    attacks.ShowLine(0, context.Position, _aim, _definition.ChargeDistance, _definition.ChargeLineWidth, Progress(_definition.ChargeWindupSeconds));
                    return Vector2.zero;
                case Phase.Charge:
                    return _aim * _definition.ChargeSpeed;
                case Phase.BarrageWindup:
                    ShowBarrageTelegraph(context.Position, attacks);
                    return Vector2.zero;
                default:
                    return Vector2.zero;
            }
        }

        // 半分を切ったら1回だけ切り替える。色は変わるときだけ入れる
        void UpdateEnrage(in EnemyContext context, IEnemyAttackPort attacks)
        {
            if (!_enraged && context.HpRatio < _definition.EnrageHpRatio)
            {
                _enraged = true;
                attacks.SetTint(_definition.EnragedTint);
                _tintApplied = true;
            }
            if (_tintApplied) return;
            attacks.SetTint(_definition.Tint);
            _tintApplied = true;
        }

        float Progress(float seconds) => Mathf.Clamp01(_elapsed / seconds);

        float DurationOf(Phase phase)
        {
            switch (phase)
            {
                case Phase.Approach: return _definition.ApproachSeconds * (_enraged ? _definition.EnragedIntervalScale : 1f);
                case Phase.ChargeWindup: return _definition.ChargeWindupSeconds;
                case Phase.Charge: return _definition.ChargeDistance / Mathf.Max(_definition.ChargeSpeed, 0.01f);
                case Phase.BarrageWindup: return _definition.BarrageWindupSeconds;
                default: return _definition.SummonWindupSeconds;
            }
        }

        void Advance(in EnemyContext context, IEnemyAttackPort attacks)
        {
            _elapsed = 0f;
            switch (_phase)
            {
                case Phase.Approach:
                    BeginNextAttack(context, attacks);
                    break;
                case Phase.ChargeWindup:
                    _phase = Phase.Charge;
                    break;
                case Phase.BarrageWindup:
                    FireBarrage(attacks);
                    _phase = Phase.Approach;
                    break;
                default:
                    _phase = Phase.Approach;
                    break;
            }
        }

        void BeginNextAttack(in EnemyContext context, IEnemyAttackPort attacks)
        {
            var toTarget = context.TargetPosition - context.Position;
            _aim = toTarget.sqrMagnitude > 0f ? toTarget.normalized : Vector2.down;
            switch (_next)
            {
                case Attack.Charge:
                    _phase = Phase.ChargeWindup;
                    _next = Attack.Barrage;
                    break;
                case Attack.Barrage:
                    _phase = Phase.BarrageWindup;
                    _barrageCount = _enraged ? _definition.EnragedBarrageCount : _definition.BarrageCount;
                    _next = Attack.Summon;
                    break;
                default:
                    _phase = Phase.Summon;
                    Summon(context, attacks);
                    _next = Attack.Charge;
                    break;
            }
        }

        void ShowBarrageTelegraph(Vector2 origin, IEnemyAttackPort attacks)
        {
            float flashes = _enraged ? _definition.EnragedGlowFlashesPerSecond : _definition.GlowFlashesPerSecond;
            var glow = _enraged ? _definition.EnragedGlowColor : _definition.GlowColor;
            attacks.ShowGlow(glow, Mathf.PingPong(_elapsed * flashes * 2f, 1f));
            float progress = Progress(_definition.BarrageWindupSeconds);
            for (int i = 0; i < _barrageCount; i++)
            {
                attacks.ShowLine(i, origin, RingPattern.Direction(_aim, i, _barrageCount), _definition.BarrageLineLength, _definition.BarrageLineWidth, progress);
            }
        }

        void FireBarrage(IEnemyAttackPort attacks)
        {
            if (_definition.Projectile == null) return;
            for (int i = 0; i < _barrageCount; i++) attacks.Shoot(_definition.Projectile, RingPattern.Direction(_aim, i, _barrageCount));
        }

        // ボスを囲むように等間隔で出す。始めの向きは相手への向きの横にして、相手との間をふさがない
        void Summon(in EnemyContext context, IEnemyAttackPort attacks)
        {
            int count = _definition.SummonCount;
            if (_definition.SummonEnemy == null || count <= 0) return;
            var start = new Vector2(-_aim.y, _aim.x);
            for (int i = 0; i < count; i++)
            {
                var position = RingPattern.Point(context.Position, start, i, count, _definition.SummonDistance, _definition.SummonArea);
                attacks.Summon(_definition.SummonEnemy, position, _definition.SummonWindupSeconds);
            }
        }
    }
}

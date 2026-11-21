using System;

namespace PictoArcher.Core
{
    /// <summary>
    /// 敵の攻撃の周期：待つ → 予兆（赤い線・円）→ 攻撃 → 待つ。Tick で進め、攻撃する瞬間に true を返す。
    /// 攻撃は必ず予兆のあとに起こる。1回の Tick で攻撃するのは多くても1回。
    /// </summary>
    public sealed class AttackCycle
    {
        readonly float _interval;
        readonly float _windup;
        float _elapsed;

        /// <param name="interval">攻撃から次の予兆が始まるまでの秒数（0 以上）。</param>
        /// <param name="windup">予兆の秒数（0 より大きい）。</param>
        public AttackCycle(float interval, float windup)
        {
            if (interval < 0f) throw new ArgumentOutOfRangeException(nameof(interval), "Interval must not be negative.");
            if (windup <= 0f) throw new ArgumentOutOfRangeException(nameof(windup), "Windup must be positive.");
            _interval = interval;
            _windup = windup;
        }

        public AttackPhase Phase { get; private set; }

        /// <summary>予兆の進み具合（0〜1）。待っている間は 0。</summary>
        public float Progress => Phase == AttackPhase.Telegraphing ? Math.Min(_elapsed / _windup, 1f) : 0f;

        /// <summary>時間を進める。この Tick で予兆が終わって攻撃する瞬間なら true。</summary>
        public bool Tick(float deltaTime)
        {
            if (deltaTime < 0f) throw new ArgumentOutOfRangeException(nameof(deltaTime), "Delta time must not be negative.");
            _elapsed += deltaTime;
            if (Phase == AttackPhase.Waiting && _elapsed >= _interval)
            {
                _elapsed -= _interval;
                Phase = AttackPhase.Telegraphing;
            }
            if (Phase == AttackPhase.Telegraphing && _elapsed >= _windup)
            {
                _elapsed = 0f;
                Phase = AttackPhase.Waiting;
                return true;
            }
            return false;
        }

        /// <summary>待ちの最初に戻す（止まっていた敵が動き出すときなど）。</summary>
        public void Reset()
        {
            Phase = AttackPhase.Waiting;
            _elapsed = 0f;
        }
    }
}

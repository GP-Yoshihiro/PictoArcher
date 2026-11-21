using System;

namespace PictoArcher.Core
{
    /// <summary>
    /// 状態異常を1回かける依頼。かける側が作り、IStatusReceiver.ApplyStatus に渡す。
    /// Power の意味は種類で変わる：炎・毒は1秒あたりのダメージ、氷は遅くする割合（0〜1。0.3 なら速さが 70% になる）。
    /// 重ねがけ（上書き・延長・毒の重なり）は受ける側が決める。
    /// </summary>
    public readonly struct StatusEffect
    {
        public StatusEffect(StatusKind kind, float duration, float power)
        {
            if (duration <= 0f) throw new ArgumentOutOfRangeException(nameof(duration), "Duration must be positive.");
            if (power < 0f) throw new ArgumentOutOfRangeException(nameof(power), "Power must not be negative.");
            if (kind == StatusKind.Freeze && power > 1f) throw new ArgumentOutOfRangeException(nameof(power), "Freeze power is a slow ratio from 0 to 1.");
            Kind = kind;
            Duration = duration;
            Power = power;
        }

        public StatusKind Kind { get; }
        /// <summary>続く秒数。</summary>
        public float Duration { get; }
        public float Power { get; }
    }
}

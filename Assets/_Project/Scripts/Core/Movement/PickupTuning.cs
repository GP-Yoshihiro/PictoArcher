using System;

namespace PictoArcher.Core
{
    /// <summary>拾うもの（経験値・コイン）の吸い寄せの調整値。長さはユニット、速さはユニット毎秒。</summary>
    public readonly struct PickupTuning
    {
        public PickupTuning(float magnetRadius, float collectRadius, float startSpeed, float acceleration, float maxSpeed)
        {
            if (magnetRadius < 0f) throw new ArgumentOutOfRangeException(nameof(magnetRadius));
            if (collectRadius < 0f) throw new ArgumentOutOfRangeException(nameof(collectRadius));
            if (startSpeed < 0f) throw new ArgumentOutOfRangeException(nameof(startSpeed));
            if (acceleration < 0f) throw new ArgumentOutOfRangeException(nameof(acceleration));
            if (maxSpeed < startSpeed) throw new ArgumentOutOfRangeException(nameof(maxSpeed));
            MagnetRadius = magnetRadius;
            CollectRadius = collectRadius;
            StartSpeed = startSpeed;
            Acceleration = acceleration;
            MaxSpeed = maxSpeed;
        }

        /// <summary>この距離まで近づくと吸い寄せ始める。</summary>
        public float MagnetRadius { get; }
        /// <summary>この距離まで来たら拾う。</summary>
        public float CollectRadius { get; }
        /// <summary>吸い寄せ始めの速さ。</summary>
        public float StartSpeed { get; }
        /// <summary>吸い寄せの間、毎秒これだけ速くなる。</summary>
        public float Acceleration { get; }
        public float MaxSpeed { get; }
    }
}

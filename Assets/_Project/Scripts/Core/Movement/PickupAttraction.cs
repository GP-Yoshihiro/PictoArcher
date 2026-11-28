using System;
using UnityEngine;

namespace PictoArcher.Core
{
    /// <summary>
    /// 拾うもの1つの動き。近づくか Attract されると、だんだん速くなりながら相手へ向かい、届くと拾われる。
    /// 一度吸い寄せ始めたら、離れても止まらない。
    /// </summary>
    public sealed class PickupAttraction
    {
        readonly PickupTuning _tuning;
        float _speed;

        public PickupAttraction(Vector2 position, PickupTuning tuning)
        {
            Position = position;
            _tuning = tuning;
        }

        public Vector2 Position { get; private set; }
        public bool IsAttracted { get; private set; }
        public bool IsCollected { get; private set; }

        /// <summary>距離に関係なく吸い寄せ始める（ウェーブの全滅のとき）。</summary>
        public void Attract()
        {
            if (IsAttracted) return;
            IsAttracted = true;
            _speed = _tuning.StartSpeed;
        }

        /// <summary>1フレーム進める。拾われたら true を返す（そのあとも true のまま）。</summary>
        public bool Tick(float deltaTime, Vector2 target)
        {
            if (deltaTime < 0f) throw new ArgumentOutOfRangeException(nameof(deltaTime));
            if (IsCollected) return true;
            float distance = Vector2.Distance(Position, target);
            if (!IsAttracted && distance <= _tuning.MagnetRadius) Attract();
            if (IsAttracted)
            {
                _speed = Mathf.Min(_speed + _tuning.Acceleration * deltaTime, _tuning.MaxSpeed);
                Position = Vector2.MoveTowards(Position, target, _speed * deltaTime);
                distance = Vector2.Distance(Position, target);
            }
            if (distance <= _tuning.CollectRadius) IsCollected = true;
            return IsCollected;
        }
    }
}

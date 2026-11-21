using System;
using UnityEngine;

namespace PictoArcher.Core
{
    /// <summary>目標へまっすぐ向かう動き。障害物を避ける道探しはしない（ぶつかったら物理で滑る）。</summary>
    public sealed class ChaseMovement
    {
        readonly float _speed;

        public ChaseMovement(float speed)
        {
            if (speed < 0f) throw new ArgumentOutOfRangeException(nameof(speed), "Speed must not be negative.");
            _speed = speed;
        }

        /// <summary>self から target へ向かう速度（長さは speed）。同じ位置なら 0。</summary>
        public Vector2 Velocity(Vector2 self, Vector2 target)
        {
            var toTarget = target - self;
            if (toTarget.sqrMagnitude < 1e-8f) return Vector2.zero;
            return toTarget.normalized * _speed;
        }
    }
}

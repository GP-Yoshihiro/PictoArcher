using System;
using UnityEngine;

namespace PictoArcher.Core
{
    /// <summary>扇形に弾を撃つときの、弾ごとの向き。</summary>
    public static class FanPattern
    {
        /// <summary>
        /// center を真ん中に、全体で totalSpreadDegrees の開きに count 本を均等に並べたときの index 番目の向き（長さ 1）。
        /// 1本のときは center そのまま。
        /// </summary>
        public static Vector2 Direction(Vector2 center, int index, int count, float totalSpreadDegrees)
        {
            if (count < 1) throw new ArgumentOutOfRangeException(nameof(count), "Count must be at least 1.");
            if (index < 0 || index >= count) throw new ArgumentOutOfRangeException(nameof(index), "Index must be within count.");
            var unit = center.normalized;
            if (count == 1) return unit;
            float offset = -totalSpreadDegrees * 0.5f + totalSpreadDegrees * index / (count - 1);
            float radians = offset * Mathf.Deg2Rad;
            float cos = Mathf.Cos(radians);
            float sin = Mathf.Sin(radians);
            return new Vector2(unit.x * cos - unit.y * sin, unit.x * sin + unit.y * cos);
        }
    }
}

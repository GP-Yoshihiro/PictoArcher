using System;
using UnityEngine;

namespace PictoArcher.Core
{
    /// <summary>障害物（長方形）とプレイヤーの体（円）が重なるかの判定。重なる場所には障害物を出さない（閉じ込めない）ために使う。</summary>
    public static class ObstacleOverlap
    {
        /// <summary>長方形の中で円の中心に一番近い点までの距離が radius より小さいとき true。ちょうど radius は重ならない。</summary>
        public static bool Overlaps(Rect obstacle, Vector2 center, float radius)
        {
            if (radius < 0f) throw new ArgumentOutOfRangeException(nameof(radius), radius, "半径は 0 以上");
            float x = Mathf.Clamp(center.x, obstacle.xMin, obstacle.xMax);
            float y = Mathf.Clamp(center.y, obstacle.yMin, obstacle.yMax);
            float dx = center.x - x;
            float dy = center.y - y;
            return dx * dx + dy * dy < radius * radius;
        }
    }
}

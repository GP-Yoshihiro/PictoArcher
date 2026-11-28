using System;
using UnityEngine;

namespace PictoArcher.Core
{
    /// <summary>全方向に等間隔に並べるときの向きと位置（ボスの弾幕、雑魚を呼ぶ位置）。</summary>
    public static class RingPattern
    {
        /// <summary>
        /// 全方向に count 本を等間隔に並べたときの index 番目の向き（長さ 1）。0 番目が aim で、反時計回りに並ぶ。
        /// aim が長さ 0 なら下向きから始める。
        /// </summary>
        public static Vector2 Direction(Vector2 aim, int index, int count)
        {
            if (count < 1) throw new ArgumentOutOfRangeException(nameof(count), "Count must be at least 1.");
            if (index < 0 || index >= count) throw new ArgumentOutOfRangeException(nameof(index), "Index must be within count.");
            var unit = aim.sqrMagnitude > 0f ? aim.normalized : Vector2.down;
            float radians = Mathf.PI * 2f * index / count;
            float cos = Mathf.Cos(radians);
            float sin = Mathf.Sin(radians);
            return new Vector2(unit.x * cos - unit.y * sin, unit.x * sin + unit.y * cos);
        }

        /// <summary>center から Direction(start, index, count) の向きへ radius 進んだ位置を、area の中（端を含む）に収めて返す。</summary>
        public static Vector2 Point(Vector2 center, Vector2 start, int index, int count, float radius, Rect area)
        {
            var p = center + Direction(start, index, count) * radius;
            return new Vector2(Mathf.Clamp(p.x, area.xMin, area.xMax), Mathf.Clamp(p.y, area.yMin, area.yMax));
        }
    }
}

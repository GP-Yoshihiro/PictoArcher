using UnityEngine;

namespace PictoArcher.Core
{
    /// <summary>まっすぐの光線（レーザー）が点に当たるか。光線は origin から direction の向きへ length の線分で、太さの半分が radius。</summary>
    public static class BeamHit
    {
        /// <summary>
        /// point と線分の距離が radius 以下なら true。direction は向きだけを使う（長さ 0 なら origin のまわりだけ）。
        /// </summary>
        public static bool Hits(Vector2 origin, Vector2 direction, float length, float radius, Vector2 point)
        {
            var toPoint = point - origin;
            if (direction.sqrMagnitude <= 0f) return toPoint.sqrMagnitude <= radius * radius;
            var unit = direction.normalized;
            float along = Mathf.Clamp(Vector2.Dot(toPoint, unit), 0f, Mathf.Max(length, 0f));
            return (toPoint - unit * along).sqrMagnitude <= radius * radius;
        }
    }
}

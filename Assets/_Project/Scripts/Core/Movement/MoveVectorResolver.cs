using UnityEngine;

namespace PictoArcher.Core
{
    public static class MoveVectorResolver
    {
        public const float DragDeadZonePixels = 10f;

        public static Vector2 Resolve(Vector2 keyboard, bool dragActive, Vector2 dragOrigin, Vector2 dragCurrent, float dragRadiusPixels)
        {
            if (keyboard != Vector2.zero)
                return Vector2.ClampMagnitude(keyboard, 1f);
            if (!dragActive)
                return Vector2.zero;
            var delta = dragCurrent - dragOrigin;
            if (delta.magnitude < DragDeadZonePixels)
                return Vector2.zero;
            return Vector2.ClampMagnitude(delta / dragRadiusPixels, 1f);
        }
    }
}

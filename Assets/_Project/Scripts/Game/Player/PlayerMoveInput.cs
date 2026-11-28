using PictoArcher.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PictoArcher.Game
{
    /// <summary>
    /// キーボード（WASD と矢印）と、マウス・タッチのドラッグ（押した位置からの向き）を読む。キーボードを優先する。
    /// 入力は PlayerSystem の Tick の中で読む（action.performed は使わない）。
    /// </summary>
    public sealed class PlayerMoveInput : IMoveInput
    {
        bool _dragging;
        Vector2 _dragOrigin;

        public Vector2 ReadMove(float dragRadiusPixels)
        {
            var pointer = Pointer.current;
            bool pressed = pointer != null && pointer.press.isPressed;
            Vector2 current = pressed ? pointer.position.ReadValue() : _dragOrigin;
            if (pressed && !_dragging) _dragOrigin = current;
            _dragging = pressed;
            return MoveVectorResolver.Resolve(ReadKeyboard(), _dragging, _dragOrigin, current, dragRadiusPixels);
        }

        static Vector2 ReadKeyboard()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return Vector2.zero;
            float x = (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed ? 1f : 0f)
                - (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed ? 1f : 0f);
            float y = (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed ? 1f : 0f)
                - (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed ? 1f : 0f);
            return new Vector2(x, y);
        }
    }
}

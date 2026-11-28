using UnityEngine.InputSystem;

namespace PictoArcher.Game
{
    /// <summary>
    /// 一時停止のキー（Esc）を読む。Battle では ⏸ を開き、Pause では閉じて戻る。
    /// 入力はシーンの Tick の中で読む（action.performed は使わない）。キーボードがなければ押されていない。
    /// </summary>
    public sealed class PauseKeyInput
    {
        public bool WasPressed()
        {
            var keyboard = Keyboard.current;
            return keyboard != null && keyboard.escapeKey.wasPressedThisFrame;
        }
    }
}

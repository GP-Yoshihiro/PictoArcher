using UnityEngine;
using UnityEngine.UI;

namespace PictoArcher.Game
{
    /// <summary>
    /// uGUI のボタンが押されたことを記録しておくだけの部品。使う側は Tick の中で ConsumeClicked を見る。
    /// インスペクターの onClick には何も登録しない（Init が実行時に登録する）。
    /// </summary>
    [RequireComponent(typeof(Button))]
    public sealed class UiButtonRelay : MonoBehaviour
    {
        Button _button;
        bool _clicked;

        public void Init()
        {
            _button = GetComponent<Button>();
            _button.onClick.AddListener(HandleClicked);
        }

        /// <summary>前に呼んでから押されていたら true を返し、記録を消す。</summary>
        public bool ConsumeClicked()
        {
            bool clicked = _clicked;
            _clicked = false;
            return clicked;
        }

        public void Dispose()
        {
            if (_button != null) _button.onClick.RemoveListener(HandleClicked);
            _button = null;
            _clicked = false;
        }

        void HandleClicked() => _clicked = true;
    }
}

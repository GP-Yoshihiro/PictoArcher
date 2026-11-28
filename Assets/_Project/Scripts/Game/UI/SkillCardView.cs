using UnityEngine;
using UnityEngine.UI;

namespace PictoArcher.Game
{
    /// <summary>
    /// 縦長のカード1枚（設計書 §9）。上に強化のアイコン、真ん中に ★ の段階。下の実演アニメの枠は 25 で入れる。
    /// ★ は上限の数だけ並べ、選んだら届く段階までを明るくする。文字は出さない。
    /// </summary>
    public sealed class SkillCardView : MonoBehaviour
    {
        [SerializeField] UiButtonRelay _button;
        [SerializeField] Image _icon;
        [SerializeField, Tooltip("左から順に使う。上限の最大（5）だけ置く")] Image[] _stars = new Image[0];
        [SerializeField] Color _starOn = Color.white;
        [SerializeField] Color _starOff = new Color(1f, 1f, 1f, 0.25f);

        public void Init()
        {
            _button.Init();
            Hide();
        }

        public void Show(Sprite icon, int filledStars, int maxStars)
        {
            gameObject.SetActive(true);
            _icon.sprite = icon;
            _icon.enabled = icon != null;
            for (int i = 0; i < _stars.Length; i++)
            {
                _stars[i].gameObject.SetActive(i < maxStars);
                _stars[i].color = i < filledStars ? _starOn : _starOff;
            }
            // 前に出ていたときの押した記録を捨てる
            _button.ConsumeClicked();
        }

        public void Hide() => gameObject.SetActive(false);

        public bool ConsumeClicked() => _button.ConsumeClicked();

        public void Dispose() => _button.Dispose();
    }
}

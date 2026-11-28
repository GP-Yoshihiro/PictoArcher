using PictoArcher.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PictoArcher.Game
{
    /// <summary>クリアの板。🏆、⏱ クリアタイム、獲得コインを出す。</summary>
    public sealed class ClearView : MonoBehaviour
    {
        [SerializeField] Image _trophy;
        [SerializeField] Image _timerIcon;
        [SerializeField] TMP_Text _timeText;
        [SerializeField] Image _coinIcon;
        [SerializeField] TMP_Text _coinText;

        /// <summary>画面を開いたときに1回だけ呼ぶ。</summary>
        public void Show(RunResult result, UiIconVisual icons)
        {
            _trophy.sprite = icons.Trophy;
            _timerIcon.sprite = icons.Timer;
            _coinIcon.sprite = icons.Coin;
            _timeText.text = DigitText.Duration(result.ClearSeconds);
            _coinText.text = DigitText.Count(result.Coins);
            gameObject.SetActive(true);
        }

        public void Hide() => gameObject.SetActive(false);
    }
}

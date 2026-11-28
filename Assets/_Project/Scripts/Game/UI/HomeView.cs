using PictoArcher.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PictoArcher.Game
{
    /// <summary>ホームの画面。▶ の絵と、これまでの最高記録（クリア済みなら ⏱ 最短タイム、まだなら 23/50）を出す。</summary>
    public sealed class HomeView : MonoBehaviour
    {
        [SerializeField] Image _startIcon;
        [SerializeField] GameObject _recordRow;
        [SerializeField] TMP_Text _bestWaveText;
        [SerializeField] GameObject _bestTimeGroup;
        [SerializeField] Image _timerIcon;
        [SerializeField] TMP_Text _bestTimeText;

        /// <summary>画面を開いたときに1回だけ呼ぶ。</summary>
        public void Show(BestRecord record, UiIconVisual icons)
        {
            _startIcon.sprite = icons.Play;
            _timerIcon.sprite = icons.Timer;
            _recordRow.SetActive(record.HasPlayed);
            if (!record.HasPlayed) return;
            _bestTimeGroup.SetActive(record.HasCleared);
            _bestWaveText.gameObject.SetActive(!record.HasCleared);
            if (record.HasCleared) _bestTimeText.text = DigitText.Duration(record.BestClearSeconds);
            else _bestWaveText.text = DigitText.Progress(record.BestWave, WaveLayout.TotalWaves);
        }
    }
}

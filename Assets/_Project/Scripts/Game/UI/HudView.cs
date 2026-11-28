using TMPro;
using UnityEngine;

namespace PictoArcher.Game
{
    /// <summary>画面の上の帯（今はウェーブの n/50 だけ）。数字が変わったときだけ文字列を作り直す。</summary>
    public sealed class HudView : MonoBehaviour
    {
        [SerializeField] TMP_Text _wave;

        int _shownWave = -1;
        int _shownTotal = -1;

        public void Init()
        {
            _shownWave = -1;
            _shownTotal = -1;
            _wave.text = string.Empty;
        }

        public void SetWave(int current, int total)
        {
            if (current == _shownWave && total == _shownTotal) return;
            _shownWave = current;
            _shownTotal = total;
            _wave.SetText("{0}/{1}", current, total);
        }
    }
}

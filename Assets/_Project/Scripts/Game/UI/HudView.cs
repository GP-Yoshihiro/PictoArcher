using TMPro;
using UnityEngine;

namespace PictoArcher.Game
{
    /// <summary>画面の上の帯（ウェーブの n/50 と、ボスが出ている間のボスの HP バー）。値が変わったときだけ作り直す。</summary>
    public sealed class HudView : MonoBehaviour
    {
        [SerializeField] TMP_Text _wave;
        [SerializeField, Tooltip("ボスの HP バー全体（どくろの絵と枠）。ボスがいないときは隠す")] GameObject _bossBar;
        [SerializeField, Tooltip("ボスの HP バーの中身（赤）。anchorMax.x を残りの割合にする")] RectTransform _bossFill;

        int _shownWave = -1;
        int _shownTotal = -1;
        bool _shownBoss;
        float _shownBossRatio = -1f;

        public void Init()
        {
            _shownWave = -1;
            _shownTotal = -1;
            _wave.text = string.Empty;
            _shownBoss = false;
            _shownBossRatio = -1f;
            _bossBar.SetActive(false);
        }

        public void SetWave(int current, int total)
        {
            if (current == _shownWave && total == _shownTotal) return;
            _shownWave = current;
            _shownTotal = total;
            _wave.SetText("{0}/{1}", current, total);
        }

        public void SetBoss(bool show, int hp, int maxHp)
        {
            if (show != _shownBoss)
            {
                _shownBoss = show;
                _bossBar.SetActive(show);
            }
            if (!show) return;
            float ratio = maxHp > 0 ? Mathf.Clamp01((float)hp / maxHp) : 0f;
            if (Mathf.Approximately(ratio, _shownBossRatio)) return;
            _shownBossRatio = ratio;
            var max = _bossFill.anchorMax;
            max.x = ratio;
            _bossFill.anchorMax = max;
        }
    }
}

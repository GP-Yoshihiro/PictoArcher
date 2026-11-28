using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PictoArcher.Game
{
    /// <summary>
    /// HUD の育ちの表示（経験値バー、★n、コイン、武器と強化の絵の一覧）。自分では動かず、HudSystem が値を渡す。
    /// 文字は数字と記号だけ（★とコインと経験値は絵）。値が変わったときだけ作り直す。
    /// </summary>
    public sealed class HudStatusView : MonoBehaviour
    {
        [SerializeField] Image _levelIcon;
        [SerializeField] TMP_Text _level;
        [SerializeField] Image _coinIcon;
        [SerializeField] TMP_Text _coins;
        [SerializeField] Image _experienceIcon;
        [SerializeField, Tooltip("経験値バーの中身。anchorMax.x を次のレベルまでの割合にする")] RectTransform _experienceFill;
        [SerializeField, Tooltip("武器と強化の絵のマスを並べる親")] Transform _slotRoot;
        [SerializeField] HudSkillSlotView _slotPrefab;

        readonly List<HudSkillSlotView> _slots = new List<HudSkillSlotView>();
        int _shownLevel = -1;
        int _shownCoins = -1;
        float _shownRatio = -1f;

        public int SlotCount => _slots.Count;

        public void Init(UiIconVisual icons, Sprite weaponIcon)
        {
            _levelIcon.sprite = icons.Level;
            _coinIcon.sprite = icons.Coin;
            _experienceIcon.sprite = icons.Experience;
            _shownLevel = -1;
            _shownCoins = -1;
            _shownRatio = -1f;
            _level.text = string.Empty;
            _coins.text = string.Empty;
            SetExperience(0, 1);
            SlotAt(0).Set(-1, weaponIcon, 0);
        }

        public void SetLevel(int level)
        {
            if (level == _shownLevel) return;
            _shownLevel = level;
            _level.SetText("{0}", level);
        }

        public void SetCoins(int coins)
        {
            if (coins == _shownCoins) return;
            _shownCoins = coins;
            _coins.SetText("{0}", coins);
        }

        public void SetExperience(int experience, int toNext)
        {
            float ratio = toNext > 0 ? Mathf.Clamp01((float)experience / toNext) : 0f;
            if (Mathf.Approximately(ratio, _shownRatio)) return;
            _shownRatio = ratio;
            var max = _experienceFill.anchorMax;
            max.x = ratio;
            _experienceFill.anchorMax = max;
        }

        /// <summary>強化の絵を n 番目（0 から）に出す。武器の絵が 0 番のマスなので、マスの番号は n + 1。</summary>
        public void SetSkill(int n, SkillId id, Sprite icon, int count) => SlotAt(n + 1).Set((int)id, icon, count);

        HudSkillSlotView SlotAt(int slot)
        {
            while (_slots.Count <= slot) _slots.Add(Instantiate(_slotPrefab, _slotRoot));
            return _slots[slot];
        }
    }
}

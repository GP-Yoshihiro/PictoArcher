using System.Collections.Generic;
using PictoArcher.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PictoArcher.Game
{
    /// <summary>死亡の板。💀、到達ウェーブ（23/50）、使った武器、取った強化のアイコンを出す。</summary>
    public sealed class DefeatView : MonoBehaviour
    {
        [SerializeField] Image _skull;
        [SerializeField] TMP_Text _waveText;
        [SerializeField] Image _weaponIcon;
        [SerializeField] RectTransform _skillIconRoot;
        [SerializeField] Image _skillIconPrefab;
        [SerializeField, Tooltip("強化のアイコンを探す一覧（Data/Skills の SkillDefinition）")] SkillDefinition[] _skills;

        readonly List<Image> _skillIcons = new List<Image>();

        /// <summary>画面を開いたときに1回だけ呼ぶ。</summary>
        public void Show(RunResult result, UiIconVisual icons)
        {
            _skull.sprite = icons.Defeat;
            _waveText.text = DigitText.Progress(result.ReachedWave, WaveLayout.TotalWaves);
            _weaponIcon.sprite = WeaponIcon(result.Weapon, icons);
            ClearSkillIcons();
            for (int i = 0; i < result.Skills.Count; i++)
            {
                var sprite = FindSkillIcon(_skills, result.Skills[i]);
                if (sprite == null) continue;   // 絵のない強化は飛ばす
                var icon = Instantiate(_skillIconPrefab, _skillIconRoot);
                icon.sprite = sprite;
                _skillIcons.Add(icon);
            }
            gameObject.SetActive(true);
        }

        public void Hide() => gameObject.SetActive(false);

        public void Dispose() => ClearSkillIcons();

        /// <summary>強化の絵を一覧から探す。ないか、絵が入っていなければ null。</summary>
        public static Sprite FindSkillIcon(SkillDefinition[] skills, SkillId id)
        {
            if (skills == null) return null;
            for (int i = 0; i < skills.Length; i++)
            {
                if (skills[i] != null && skills[i].Id == id) return skills[i].Icon;
            }
            return null;
        }

        static Sprite WeaponIcon(WeaponId weapon, UiIconVisual icons)
        {
            switch (weapon)
            {
                case WeaponId.Laser: return icons.WeaponLaser;
                case WeaponId.Launcher: return icons.WeaponLauncher;
                default: return icons.WeaponGun;
            }
        }

        void ClearSkillIcons()
        {
            for (int i = 0; i < _skillIcons.Count; i++)
            {
                if (_skillIcons[i] != null) Destroy(_skillIcons[i].gameObject);
            }
            _skillIcons.Clear();
        }
    }
}

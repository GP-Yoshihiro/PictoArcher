using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PictoArcher.Game
{
    /// <summary>HUD の絵の一覧の1マス（武器か強化の絵と、重ね取りの <c>×2</c>）。値が変わったときだけ作り直す。</summary>
    public sealed class HudSkillSlotView : MonoBehaviour
    {
        [SerializeField] Image _icon;
        [SerializeField, Tooltip("重ね取りの数（×2 の形。数字と記号だけ）。1 のときは隠す")] TMP_Text _count;

        int _shownId = -1;
        int _shownCount = -1;

        /// <summary>id は強化の SkillId の番号（武器のマスは -1）。count が 2 以上のときだけ <c>×n</c> を出す。</summary>
        public void Set(int id, Sprite icon, int count)
        {
            if (id != _shownId)
            {
                _shownId = id;
                _icon.sprite = icon;
                _shownCount = -1;
            }
            if (count == _shownCount) return;
            _shownCount = count;
            _count.gameObject.SetActive(count >= 2);
            if (count >= 2) _count.SetText("×{0}", count);
        }
    }
}

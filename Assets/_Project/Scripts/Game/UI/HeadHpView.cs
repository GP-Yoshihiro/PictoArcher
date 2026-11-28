using TMPro;
using UnityEngine;

namespace PictoArcher.Game
{
    /// <summary>プレイヤーの頭上の HP バーと数字（ワールドに置く）。バーの中身は左端を基準に縮める。</summary>
    public sealed class HeadHpView : MonoBehaviour
    {
        [SerializeField] Transform _fill;
        [SerializeField] TextMeshPro _number;
        [SerializeField, Tooltip("プレイヤーの中心からの位置")] Vector2 _offset = new Vector2(0f, 1.4f);

        Transform _transform;
        Vector3 _fillScale;
        Vector3 _fillPosition;
        int _shownHp = -1;

        public void Init()
        {
            _transform = transform;
            _fillScale = _fill.localScale;
            _fillPosition = _fill.localPosition;
            _shownHp = -1;
            _number.text = string.Empty;
        }

        public void Show(Vector2 position, int hp, int maxHp)
        {
            _transform.position = position + _offset;
            if (hp == _shownHp) return;
            _shownHp = hp;
            float ratio = maxHp > 0 ? Mathf.Clamp01((float)hp / maxHp) : 0f;
            // 中心が基準の絵なので、縮めた分だけ左へ寄せて左端をそろえる
            _fill.localScale = new Vector3(_fillScale.x * ratio, _fillScale.y, _fillScale.z);
            _fill.localPosition = _fillPosition - new Vector3(_fillScale.x * (1f - ratio) * 0.5f, 0f, 0f);
            _number.SetText("{0}", hp);
        }
    }
}

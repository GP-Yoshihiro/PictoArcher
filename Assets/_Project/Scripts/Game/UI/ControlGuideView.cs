using UnityEngine;
using UnityEngine.UI;

namespace PictoArcher.Game
{
    /// <summary>
    /// 操作案内の最小版（矢印キー4つ、マウス、手の絵）。自分では動かず、ControlGuideSystem が Show／Hide を呼ぶ。
    /// 文字は出さない。キーの絵は上向きの矢印1枚を回して4方向にする（プレハブで回してある）。
    /// </summary>
    public sealed class ControlGuideView : MonoBehaviour
    {
        [SerializeField, Tooltip("矢印キーの絵。上・右・下・左の順")] Image[] _keys;
        [SerializeField] Image _mouse;
        [SerializeField] Image _hand;
        [SerializeField, Tooltip("手が真ん中から動く距離（Canvas の基準 1080×1920 での長さ）")] float _handTravel = 60f;
        [SerializeField, Tooltip("矢印キー1つを光らせる長さ（秒）")] float _stepSeconds = 0.6f;
        [SerializeField, Tooltip("プレイヤーが始めの位置からこれだけ動いたら消す（ユニット）")] float _dismissDistance = 0.5f;
        [SerializeField, Range(0f, 1f), Tooltip("光っていないキーの不透明度")] float _dimAlpha = 0.35f;

        RectTransform _handTransform;
        Vector2 _handHome;
        int _shownKey = -1;

        public float StepSeconds => _stepSeconds;
        public float DismissDistance => _dismissDistance;

        public void Init(UiIconVisual icons)
        {
            for (int i = 0; i < _keys.Length; i++) _keys[i].sprite = icons.GuideArrowKey;
            _mouse.sprite = icons.GuideMouse;
            _hand.sprite = icons.GuideHand;
            _handTransform = _hand.rectTransform;
            _handHome = _handTransform.anchoredPosition;
            _shownKey = -1;
            gameObject.SetActive(true);
        }

        public void Show(int highlightedKey, Vector2 dragOffset)
        {
            if (highlightedKey != _shownKey)
            {
                _shownKey = highlightedKey;
                for (int i = 0; i < _keys.Length; i++)
                {
                    var color = _keys[i].color;
                    color.a = i == highlightedKey ? 1f : _dimAlpha;
                    _keys[i].color = color;
                }
            }
            _handTransform.anchoredPosition = _handHome + dragOffset * _handTravel;
        }

        public void Hide() => gameObject.SetActive(false);
    }
}

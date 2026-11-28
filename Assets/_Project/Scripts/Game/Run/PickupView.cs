using UnityEngine;

namespace PictoArcher.Game
{
    /// <summary>拾うもの1つの見た目。自分では動かず、PickupSystem が位置を写す。当たり判定は持たない（距離で拾う）。</summary>
    public sealed class PickupView : MonoBehaviour
    {
        [SerializeField] SpriteRenderer _sprite;
        Transform _transform;

        public void Init(SpriteVisual visual, Vector2 position)
        {
            _transform = transform;
            float scale = visual != null ? visual.Scale : 1f;
            _transform.localScale = new Vector3(scale, scale, 1f);
            _sprite.sprite = visual != null && visual.Clip.Frames.Count > 0 ? visual.Clip.Frames[0] : null;
            SetPosition(position);
        }

        public void SetPosition(Vector2 position) => _transform.position = position;
    }
}

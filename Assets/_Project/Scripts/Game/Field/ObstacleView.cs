using UnityEngine;

namespace PictoArcher.Game
{
    /// <summary>障害物1つの見た目と当たり判定。自分では動かず、ObstacleSystem が呼ぶ。</summary>
    public sealed class ObstacleView : MonoBehaviour
    {
        [SerializeField] SpriteRenderer _sprite;
        [SerializeField] BoxCollider2D _collider;

        /// <summary>位置・絵・大きさ・当たりの範囲をパターンの1つに合わせる。</summary>
        public void Init(ObstaclePlacement placement)
        {
            var sprite = placement.Sprite;
            float scale = placement.Visual != null ? placement.Visual.Scale : 1f;
            var self = transform;
            self.localPosition = placement.Position;
            self.localScale = new Vector3(scale, scale, 1f);
            _sprite.sprite = sprite;
            if (sprite == null) return;
            var bounds = sprite.bounds;
            _collider.size = bounds.size;
            _collider.offset = bounds.center;
        }

        /// <summary>薄い影にする（当たり判定なし）。</summary>
        public void ShowShadow(Color color)
        {
            _collider.enabled = false;
            _sprite.color = color;
        }

        /// <summary>現れた状態にする（当たり判定あり）。</summary>
        public void ShowSolid()
        {
            _collider.enabled = true;
            _sprite.color = Color.white;
        }
    }
}

using UnityEngine;

namespace PictoArcher.Game
{
    /// <summary>
    /// プレイヤーの弾のプレハブに付く部品。自分では動かない（PlayerProjectileSystem が Tick で見る）。
    /// プレハブの形：根に Rigidbody2D（Kinematic、Gravity Scale 0）・Collider2D（Is Trigger）・CollisionRelay2D・この部品（レイヤー PlayerShot）。子に SpriteRenderer を1つ置く。
    /// </summary>
    public sealed class PlayerProjectileView : MonoBehaviour
    {
        [SerializeField] SpriteRenderer _sprite;
        [SerializeField] Rigidbody2D _rigidbody;
        [SerializeField] CollisionRelay2D _relay;

        public CollisionRelay2D Relay => _relay;
        public Vector2 Position => _rigidbody.position;

        /// <summary>position から velocity の向きと速さで飛ばす。絵は見た目の表の最初のコマ。</summary>
        public void Launch(Vector2 position, Vector2 velocity, SpriteVisual visual)
        {
            var self = transform;
            self.position = position;
            self.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg);
            _rigidbody.position = position;
            _rigidbody.linearVelocity = velocity;
            if (visual == null) return;
            var frames = visual.Clip.Frames;
            if (frames.Count > 0) _sprite.sprite = frames[0];
            _sprite.transform.localScale = new Vector3(visual.Scale, visual.Scale, 1f);
        }
    }
}

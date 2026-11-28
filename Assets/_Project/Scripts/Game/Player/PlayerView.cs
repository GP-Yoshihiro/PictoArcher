using UnityEngine;

namespace PictoArcher.Game
{
    /// <summary>
    /// プレイヤーのプレハブに付く部品。自分では動かない（PlayerSystem が Tick で動かす）。
    /// プレハブの形：根に Rigidbody2D（Dynamic、Gravity Scale 0、回転を固定、Interpolate）・CircleCollider2D・この部品（レイヤー Player）。子に SpriteRenderer を1つ置く。
    /// </summary>
    public sealed class PlayerView : MonoBehaviour
    {
        [SerializeField] Rigidbody2D _rigidbody;
        [SerializeField] SpriteRenderer _sprite;
        [SerializeField, Tooltip("無敵時間の点滅の、消える・出るの1回の長さ（秒）")] float _blinkSeconds = 0.08f;

        public SpriteRenderer Sprite => _sprite;
        public float BlinkSeconds => _blinkSeconds;
        public Vector2 Position => _rigidbody.position;

        public void Init(Vector2 position)
        {
            transform.position = position;
            _rigidbody.position = position;
            _rigidbody.linearVelocity = Vector2.zero;
        }

        public void SetVelocity(Vector2 velocity) => _rigidbody.linearVelocity = velocity;

        public void SetVisible(bool visible) => _sprite.enabled = visible;
    }
}

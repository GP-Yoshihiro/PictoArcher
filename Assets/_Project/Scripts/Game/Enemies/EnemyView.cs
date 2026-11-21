using UnityEngine;

namespace PictoArcher.Game
{
    /// <summary>
    /// 敵のプレハブに付く部品。自分では動かない（動かすのは EnemySystem が Tick で呼ぶ EnemyUnit）。
    /// プレハブの形：根（足元、レイヤー Enemy）に Rigidbody2D（Dynamic、Gravity Scale 0、回転固定）・Collider2D・CollisionRelay2D・この部品。
    /// 子に SpriteRenderer を1つ置く（CharacterSpriteAnimator が倍率と足元のずれを入れる）。
    /// </summary>
    public sealed class EnemyView : MonoBehaviour
    {
        [SerializeField] SpriteRenderer _sprite;
        [SerializeField] Rigidbody2D _rigidbody;
        [SerializeField] Collider2D _body;
        [SerializeField] CollisionRelay2D _relay;

        Transform _transform;

        public SpriteRenderer Sprite => _sprite;
        /// <summary>弾が当たる Collider2D。EnemyRegistry に登録する。</summary>
        public Collider2D Body => _body;
        public CollisionRelay2D Relay => _relay;
        public Vector2 Position => _transform.position;

        public void Init(Vector2 position)
        {
            _transform = transform;
            gameObject.layer = Layers.Enemy;
            _transform.position = position;
        }

        public void SetVelocity(Vector2 velocity) => _rigidbody.linearVelocity = velocity;

        /// <summary>死んだあと、当たらず動かない状態にする。</summary>
        public void Freeze()
        {
            _rigidbody.linearVelocity = Vector2.zero;
            _body.enabled = false;
        }
    }
}

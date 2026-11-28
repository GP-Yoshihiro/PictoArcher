using UnityEngine;

namespace PictoArcher.Game
{
    /// <summary>
    /// 光線と爆発の見た目だけのプレハブに付く部品（当たりは持たない）。自分では動かない（PlayerProjectileSystem が Tick でコマを進め、片付ける）。
    /// プレハブの形：根にこの部品、子に SpriteRenderer を1つ置く。
    /// </summary>
    public sealed class ShotEffectView : MonoBehaviour
    {
        [SerializeField] SpriteRenderer _sprite;

        /// <summary>position に、z 軸まわりに degrees 回して、絵が size（ユニット）の大きさになるように置く。</summary>
        public void Show(Vector2 position, float degrees, Vector2 size, Sprite sprite)
        {
            var self = transform;
            self.position = position;
            self.rotation = Quaternion.Euler(0f, 0f, degrees);
            SetFrame(sprite);
            if (sprite == null) return;
            var bounds = sprite.bounds.size;
            _sprite.transform.localScale = new Vector3(
                bounds.x > 0f ? size.x / bounds.x : 1f,
                bounds.y > 0f ? size.y / bounds.y : 1f,
                1f);
        }

        public void SetFrame(Sprite sprite) => _sprite.sprite = sprite;
    }
}

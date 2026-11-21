using UnityEngine;

namespace PictoArcher.Game
{
    /// <summary>敵の弾1種類の調整値。撃つ敵の動きの定義（ShooterBehaviorDefinition など）が指す。</summary>
    [CreateAssetMenu(menuName = "PictoArcher/EnemyProjectileDefinition")]
    public sealed class EnemyProjectileDefinition : ScriptableObject
    {
        [SerializeField] EnemyProjectileView _prefab;
        [SerializeField, Tooltip("見た目の表（Data/Visuals/Projectiles）")] SpriteVisual _visual;
        [SerializeField, Tooltip("1秒に進む距離（ユニット）")] float _speed = 8f;
        [SerializeField] int _damage = 5;
        [SerializeField, Tooltip("消えるまでの秒数（壁やプレイヤーに当たらなかったとき）")] float _lifetime = 4f;

        public EnemyProjectileView Prefab => _prefab;
        public SpriteVisual Visual => _visual;
        public float Speed => _speed;
        public int Damage => _damage;
        public float Lifetime => _lifetime;
    }
}

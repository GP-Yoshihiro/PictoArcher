using UnityEngine;

namespace PictoArcher.Game
{
    /// <summary>銃：1発ずつまっすぐ飛ぶ弾。速く撃つ（基準の連射速度）。</summary>
    [CreateAssetMenu(menuName = "PictoArcher/Weapon/Gun")]
    public sealed class GunWeaponDefinition : WeaponDefinition
    {
        [SerializeField, Tooltip("弾の速さ（ユニット/秒）")] float _projectileSpeed = 14f;
        [SerializeField, Tooltip("弾が消えるまでの秒数")] float _projectileLifetime = 3f;
        [SerializeField] SpriteVisual _projectileVisual;

        public float ProjectileSpeed => _projectileSpeed;
        public float ProjectileLifetime => _projectileLifetime;
        public SpriteVisual ProjectileVisual => _projectileVisual;

        public override IWeapon Create() => new GunWeapon(this);
    }
}

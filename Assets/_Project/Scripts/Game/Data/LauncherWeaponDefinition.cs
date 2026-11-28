using UnityEngine;

namespace PictoArcher.Game
{
    /// <summary>ランチャー：小さな弾が敵か壁に当たると爆発し、まわりの敵にも当たる。撃つのは遅い。</summary>
    [CreateAssetMenu(menuName = "PictoArcher/Weapon/Launcher")]
    public sealed class LauncherWeaponDefinition : WeaponDefinition
    {
        [SerializeField, Tooltip("弾の速さ（ユニット/秒）")] float _projectileSpeed = 9f;
        [SerializeField, Tooltip("弾が消えるまでの秒数（当たらずに消えたら爆発しない）")] float _projectileLifetime = 3f;
        [SerializeField] SpriteVisual _projectileVisual;
        [SerializeField, Tooltip("爆発の半径。敵の中心がこの距離の内なら当たる")] float _blastRadius = 2f;
        [SerializeField, Tooltip("爆発を見せる秒数")] float _blastShowSeconds = 0.25f;
        [SerializeField] SpriteVisual _blastVisual;

        public float ProjectileSpeed => _projectileSpeed;
        public float ProjectileLifetime => _projectileLifetime;
        public SpriteVisual ProjectileVisual => _projectileVisual;
        public float BlastRadius => _blastRadius;
        public float BlastShowSeconds => _blastShowSeconds;
        public SpriteVisual BlastVisual => _blastVisual;

        public override IWeapon Create() => new LauncherWeapon(this);
    }
}

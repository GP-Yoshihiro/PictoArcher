using UnityEngine;

namespace PictoArcher.Game
{
    /// <summary>レーザー：一瞬でまっすぐ伸び、線の上の敵すべてに当たる（壁で止まる）。撃つのは遅め。</summary>
    [CreateAssetMenu(menuName = "PictoArcher/Weapon/Laser")]
    public sealed class LaserWeaponDefinition : WeaponDefinition
    {
        [SerializeField, Tooltip("光線の長さ（壁があればそこまで）")] float _length = 24f;
        [SerializeField, Tooltip("当たる太さの半分。敵の中心がこの距離の内なら当たる")] float _hitRadius = 0.6f;
        [SerializeField, Tooltip("見た目の太さ（ユニット）")] float _visualWidth = 0.25f;
        [SerializeField, Tooltip("光線を見せる秒数")] float _showSeconds = 0.12f;
        [SerializeField] SpriteVisual _beamVisual;

        public float Length => _length;
        public float HitRadius => _hitRadius;
        public float VisualWidth => _visualWidth;
        public float ShowSeconds => _showSeconds;
        public SpriteVisual BeamVisual => _beamVisual;

        public override IWeapon Create() => new LaserWeapon(this);
    }
}

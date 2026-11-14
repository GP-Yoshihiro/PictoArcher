using UnityEngine;
using UnityEngine.Serialization;

namespace PictoArcher.Game
{
    /// <summary>プレイヤーの調整値。</summary>
    [CreateAssetMenu(menuName = "PictoArcher/PlayerStats")]
    public sealed class PlayerStats : ScriptableObject
    {
        [SerializeField, FormerlySerializedAs("maxHp")] int _maxHp = 100;
        [SerializeField, FormerlySerializedAs("moveSpeed")] float _moveSpeed = 6f;
        [SerializeField, FormerlySerializedAs("fireInterval")] float _fireInterval = 0.5f;
        [SerializeField, FormerlySerializedAs("moveFireRateMultiplier")] float _moveFireRateMultiplier = 1f;
        [SerializeField, FormerlySerializedAs("projectileDamage")] int _projectileDamage = 10;
        [SerializeField, FormerlySerializedAs("projectileSpeed")] float _projectileSpeed = 14f;
        [SerializeField, FormerlySerializedAs("projectileLifetime")] float _projectileLifetime = 3f;
        [SerializeField, FormerlySerializedAs("invulnerabilitySeconds")] float _invulnerabilitySeconds = 0.6f;
        [SerializeField, FormerlySerializedAs("dragRadiusPixels")] float _dragRadiusPixels = 80f;

        public int MaxHp => _maxHp;
        public float MoveSpeed => _moveSpeed;
        public float FireInterval => _fireInterval;
        public float MoveFireRateMultiplier => _moveFireRateMultiplier;
        public int ProjectileDamage => _projectileDamage;
        public float ProjectileSpeed => _projectileSpeed;
        public float ProjectileLifetime => _projectileLifetime;
        public float InvulnerabilitySeconds => _invulnerabilitySeconds;
        public float DragRadiusPixels => _dragRadiusPixels;
    }
}

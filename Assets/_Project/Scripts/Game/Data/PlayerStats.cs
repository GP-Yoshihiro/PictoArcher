using UnityEngine;

namespace PictoArcher.Game
{
    [CreateAssetMenu(menuName = "PictoArcher/PlayerStats")]
    public class PlayerStats : ScriptableObject
    {
        public int maxHp = 100;
        public float moveSpeed = 6f;
        public float fireInterval = 0.5f;
        public float moveFireRateMultiplier = 1f;
        public int projectileDamage = 10;
        public float projectileSpeed = 14f;
        public float projectileLifetime = 3f;
        public float invulnerabilitySeconds = 0.6f;
        public float dragRadiusPixels = 80f;
    }
}

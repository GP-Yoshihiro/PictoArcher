using UnityEngine;

namespace PictoArcher.Game
{
    /// <summary>銃の撃ち方：まっすぐ飛ぶ弾を1発。</summary>
    public sealed class GunWeapon : IWeapon
    {
        readonly GunWeaponDefinition _definition;

        public GunWeapon(GunWeaponDefinition definition) => _definition = definition;

        public float FireInterval => _definition.FireInterval;

        public void Fire(Vector2 origin, Vector2 direction, IPlayerShotPort shots)
            => shots.Fire(origin, direction, _definition.ProjectileSpeed, _definition.Damage, _definition.ProjectileLifetime, _definition.ProjectileVisual);
    }
}

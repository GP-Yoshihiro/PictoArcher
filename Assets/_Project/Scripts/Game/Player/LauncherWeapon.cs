using UnityEngine;

namespace PictoArcher.Game
{
    /// <summary>ランチャーの撃ち方：当たると爆発する弾を1発。</summary>
    public sealed class LauncherWeapon : IWeapon
    {
        readonly LauncherWeaponDefinition _definition;

        public LauncherWeapon(LauncherWeaponDefinition definition) => _definition = definition;

        public float FireInterval => _definition.FireInterval;

        public void Fire(Vector2 origin, Vector2 direction, IPlayerShotPort shots)
            => shots.FireShell(origin, direction, _definition.ProjectileSpeed, _definition.Damage, _definition.ProjectileLifetime, _definition.ProjectileVisual,
                _definition.BlastRadius, _definition.BlastVisual, _definition.BlastShowSeconds);
    }
}

using UnityEngine;

namespace PictoArcher.Game
{
    /// <summary>レーザーの撃ち方：一瞬で伸びる光線を1本。</summary>
    public sealed class LaserWeapon : IWeapon
    {
        readonly LaserWeaponDefinition _definition;

        public LaserWeapon(LaserWeaponDefinition definition) => _definition = definition;

        public float FireInterval => _definition.FireInterval;

        public void Fire(Vector2 origin, Vector2 direction, IPlayerShotPort shots)
            => shots.FireBeam(origin, direction, _definition.Length, _definition.HitRadius, _definition.Damage,
                _definition.BeamVisual, _definition.VisualWidth, _definition.ShowSeconds);
    }
}

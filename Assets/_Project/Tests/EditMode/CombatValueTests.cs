using System;
using NUnit.Framework;
using PictoArcher.Core;

namespace PictoArcher.Tests.EditMode
{
    public class CombatValueTests
    {
        [Test] public void Damage_KeepsAmountAndKind()
        {
            var damage = new Damage(12, DamageKind.Burn);
            Assert.AreEqual(12, damage.Amount);
            Assert.AreEqual(DamageKind.Burn, damage.Kind);
        }

        [Test] public void Damage_DefaultKindIsDirect() =>
            Assert.AreEqual(DamageKind.Direct, new Damage(5).Kind);

        [Test] public void Damage_NegativeAmountThrows() =>
            Assert.Throws<ArgumentOutOfRangeException>(() => new Damage(-1));

        [Test] public void StatusEffect_KeepsValues()
        {
            var effect = new StatusEffect(StatusKind.Freeze, 2f, 0.5f);
            Assert.AreEqual(StatusKind.Freeze, effect.Kind);
            Assert.AreEqual(2f, effect.Duration);
            Assert.AreEqual(0.5f, effect.Power);
        }

        [Test] public void StatusEffect_NonPositiveDurationThrows() =>
            Assert.Throws<ArgumentOutOfRangeException>(() => new StatusEffect(StatusKind.Burn, 0f, 3f));

        [Test] public void StatusEffect_NegativePowerThrows() =>
            Assert.Throws<ArgumentOutOfRangeException>(() => new StatusEffect(StatusKind.Poison, 1f, -1f));

        [Test] public void StatusEffect_FreezeAboveOneThrows() =>
            Assert.Throws<ArgumentOutOfRangeException>(() => new StatusEffect(StatusKind.Freeze, 1f, 1.5f));
    }
}

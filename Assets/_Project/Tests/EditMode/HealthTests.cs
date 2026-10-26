using System.Collections.Generic;
using NUnit.Framework;
using PictoArcher.Core;
using UnityEngine;

namespace PictoArcher.Tests.EditMode
{
    public class HealthTests
    {
        [Test] public void Damage_ReducesAndClampsAtZero()
        {
            var h = new Health(30);
            Assert.AreEqual(10, h.ApplyDamage(10));
            Assert.AreEqual(20, h.ApplyDamage(50));
            Assert.AreEqual(0, h.Current);
            Assert.IsTrue(h.IsDead);
        }

        [Test] public void NonPositiveDamage_IsIgnored()
        {
            var h = new Health(30);
            Assert.AreEqual(0, h.ApplyDamage(0));
            Assert.AreEqual(0, h.ApplyDamage(-5));
            Assert.AreEqual(30, h.Current);
        }

        [Test] public void Died_FiresOnce_EvenWithExtraHits()
        {
            var h = new Health(10);
            int count = 0;
            h.Died += () => count++;
            h.ApplyDamage(10);
            Assert.AreEqual(0, h.ApplyDamage(10));
            Assert.AreEqual(1, count);
        }

        [Test] public void Heal_ClampsAtMax_AndDoesNothingWhenDead()
        {
            var h = new Health(30);
            h.ApplyDamage(5);
            Assert.AreEqual(5, h.Heal(100));
            h.ApplyDamage(30);
            Assert.AreEqual(0, h.Heal(10));
        }
    }
}

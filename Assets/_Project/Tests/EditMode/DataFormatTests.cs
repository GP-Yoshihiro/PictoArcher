using NUnit.Framework;
using UnityEngine;
using PictoArcher.Game;

namespace PictoArcher.Tests.EditMode
{
    public class DataFormatTests
    {
        [Test]
        public void PlayerStats_MoveFireRateDefaultsToFull()
        {
            var stats = ScriptableObject.CreateInstance<PlayerStats>();
            Assert.AreEqual(1f, stats.moveFireRateMultiplier);
            Object.DestroyImmediate(stats);
        }

        [Test]
        public void SkillId_HasTwelveSkills() =>
            Assert.AreEqual(12, System.Enum.GetValues(typeof(SkillId)).Length);

        [Test]
        public void WaveDefinition_DefaultsToTelegraph08()
        {
            var wave = ScriptableObject.CreateInstance<WaveDefinition>();
            Assert.AreEqual(0.8f, wave.telegraphSeconds);
            Object.DestroyImmediate(wave);
        }

        [Test]
        public void EnemyRegistry_IgnoresDuplicateRegisterAndUnknownUnregister()
        {
            EnemyRegistry.Clear();
            var a = new GameObject("a").transform;
            var b = new GameObject("b").transform;
            EnemyRegistry.Register(a);
            EnemyRegistry.Register(a);
            EnemyRegistry.Unregister(b);
            Assert.AreEqual(1, EnemyRegistry.Active.Count);
            EnemyRegistry.Clear();
            Object.DestroyImmediate(a.gameObject);
            Object.DestroyImmediate(b.gameObject);
        }
    }
}

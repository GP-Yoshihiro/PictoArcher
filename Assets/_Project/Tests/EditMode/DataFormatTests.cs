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

        [TearDown]
        public void TearDown()
        {
            EnemyRegistry.Clear();
            foreach (var go in created) if (go != null) Object.DestroyImmediate(go);
            created.Clear();
        }

        readonly System.Collections.Generic.List<GameObject> created = new System.Collections.Generic.List<GameObject>();

        [Test]
        public void EnemyRegistry_IgnoresDuplicateRegisterAndUnknownUnregister()
        {
            EnemyRegistry.Clear();
            var a = new GameObject("a"); created.Add(a);
            var b = new GameObject("b"); created.Add(b);
            EnemyRegistry.Register(a.transform);
            EnemyRegistry.Register(a.transform);
            EnemyRegistry.Unregister(b.transform);
            Assert.AreEqual(1, EnemyRegistry.Active.Count);
        }

        [Test]
        public void EnemyRegistry_HasSubsystemRegistrationResetMethod()
        {
            var m = typeof(EnemyRegistry).GetMethod("ResetOnLoad",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            Assert.IsNotNull(m);
            var attr = (RuntimeInitializeOnLoadMethodAttribute)System.Attribute.GetCustomAttribute(
                m, typeof(RuntimeInitializeOnLoadMethodAttribute));
            Assert.IsNotNull(attr);
            Assert.AreEqual(RuntimeInitializeLoadType.SubsystemRegistration, attr.loadType);
            var go = new GameObject("r"); created.Add(go);
            EnemyRegistry.Register(go.transform);
            m.Invoke(null, null);
            Assert.AreEqual(0, EnemyRegistry.Active.Count);
        }
    }
}

using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
using PictoArcher.Game;

namespace PictoArcher.Tests.EditMode
{
    public class DataFormatTests
    {
        [Test]
        public void PlayerStats_MoveFireRateDefaultsToFull()
        {
            var stats = ScriptableObject.CreateInstance<PlayerStats>();
            Assert.AreEqual(1f, stats.MoveFireRateMultiplier);
            Object.DestroyImmediate(stats);
        }

        [Test]
        public void SkillId_HasTwelveSkills() =>
            Assert.AreEqual(12, System.Enum.GetValues(typeof(SkillId)).Length);

        [Test]
        public void WaveDefinition_DefaultsToTelegraph08()
        {
            var wave = ScriptableObject.CreateInstance<WaveDefinition>();
            Assert.AreEqual(0.8f, wave.TelegraphSeconds);
            Object.DestroyImmediate(wave);
        }

        [Test]
        public void PlayerStatsAsset_KeepsValuesAfterRename()
        {
            var stats = AssetDatabase.LoadAssetAtPath<PlayerStats>("Assets/_Project/Data/PlayerStats.asset");
            Assert.IsNotNull(stats);
            Assert.AreEqual(100, stats.MaxHp);
            Assert.AreEqual(6f, stats.MoveSpeed);
            Assert.AreEqual(0.5f, stats.FireInterval);
            Assert.AreEqual(1f, stats.MoveFireRateMultiplier);
            Assert.AreEqual(10, stats.ProjectileDamage);
            Assert.AreEqual(14f, stats.ProjectileSpeed);
            Assert.AreEqual(3f, stats.ProjectileLifetime);
            Assert.AreEqual(0.6f, stats.InvulnerabilitySeconds);
            Assert.AreEqual(80f, stats.DragRadiusPixels);
        }

        // 保存された値は既定値と同じなので、上のテストだけでは名前を変えて値が消えても気づけない。
        // 古い名前を FormerlySerializedAs で残していることも確かめる。
        [Test]
        public void PlayerStats_PrivateFieldsKeepOldNames()
        {
            var fields = typeof(PlayerStats).GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            Assert.AreEqual(9, fields.Length);
            foreach (var field in fields)
            {
                var old = field.GetCustomAttribute<FormerlySerializedAsAttribute>();
                Assert.IsNotNull(old, field.Name);
                Assert.AreEqual("_" + old.oldName, field.Name);
            }
        }

        [Test]
        public void SpawnEntry_KeepsConstructorValues()
        {
            var entry = new SpawnEntry(null, new Vector2(1, 2));
            Assert.AreEqual(new Vector2(1, 2), entry.Position);
            Assert.IsNull(entry.Enemy);
        }
    }
}

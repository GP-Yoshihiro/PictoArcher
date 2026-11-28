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
        public void SkillId_MatchesDesignSection9() =>
            CollectionAssert.AreEqual(new[]
            {
                "Heal", "MaxHpUp", "AttackUp", "MoveSpeedUp", "AttackSpeedUp",
                "FrontShot", "DiagonalShot", "SideShot", "RearShot", "MultiShot",
                "WallBounce", "EnemyBounce", "Pierce",
                "Fire", "Ice", "Lightning", "Poison",
                "AreaUp", "Shrink",
                "DeathBlastVertical", "DeathBlastHorizontal", "DeathBlastRing",
                "DeathPull", "ExpGainUp", "Orbiter", "Wildcard",
            }, System.Enum.GetNames(typeof(SkillId)));

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
            var stats = AssetDatabase.LoadAssetAtPath<PlayerStats>("Assets/_Project/Data/Player/PlayerStats.asset");
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

        [Test]
        public void WaveDefinition_HasNoObstacleLayoutByDefault()
        {
            var wave = ScriptableObject.CreateInstance<WaveDefinition>();
            Assert.IsNull(wave.ObstacleLayout);
            Object.DestroyImmediate(wave);
        }

        [Test]
        public void ObstacleSwitchSettings_DefaultsMatchSpec()
        {
            var settings = ScriptableObject.CreateInstance<ObstacleSwitchSettings>();
            Assert.AreEqual(0.5f, settings.ShadowSeconds);
            Assert.AreEqual(new Color(0f, 0f, 0f, 0.35f), settings.ShadowColor);
            Assert.AreEqual(0.6f, settings.PlayerRadius);
            Object.DestroyImmediate(settings);
        }

        [Test]
        public void ObstaclePlacement_BoundsFollowSpriteAndScale()
        {
            var texture = new Texture2D(32, 16);
            var sprite = Sprite.Create(texture, new Rect(0f, 0f, 32f, 16f), new Vector2(0.5f, 0.5f), 16f);
            var visual = ScriptableObject.CreateInstance<SpriteVisual>();
            var so = new SerializedObject(visual);
            var frames = so.FindProperty("_clip._frames");
            frames.arraySize = 1;
            frames.GetArrayElementAtIndex(0).objectReferenceValue = sprite;
            so.FindProperty("_scale").floatValue = 2f;
            so.ApplyModifiedPropertiesWithoutUndo();

            var bounds = new ObstaclePlacement(visual, new Vector2(3f, 4f)).Bounds;
            Assert.AreEqual(1f, bounds.x, 1e-4f);
            Assert.AreEqual(3f, bounds.y, 1e-4f);
            Assert.AreEqual(4f, bounds.width, 1e-4f);
            Assert.AreEqual(2f, bounds.height, 1e-4f);

            Object.DestroyImmediate(visual);
            Object.DestroyImmediate(sprite);
            Object.DestroyImmediate(texture);
        }

        [Test]
        public void ObstaclePlacement_WithoutSprite_HasEmptyBounds()
            => Assert.AreEqual(Vector2.zero, new ObstaclePlacement(null, new Vector2(3f, 4f)).Bounds.size);
    }
}

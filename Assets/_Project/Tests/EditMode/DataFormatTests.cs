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
        public void PickupSettings_DefaultsBuildAValidTuning()
        {
            var settings = ScriptableObject.CreateInstance<PickupSettings>();
            var tuning = settings.Tuning;
            Assert.AreEqual(2.5f, tuning.MagnetRadius);
            Assert.AreEqual(0.5f, tuning.CollectRadius);
            Assert.AreEqual(4f, tuning.StartSpeed);
            Assert.AreEqual(30f, tuning.Acceleration);
            Assert.AreEqual(20f, tuning.MaxSpeed);
            Assert.AreEqual(0.3f, settings.DropSpread);
            Object.DestroyImmediate(settings);
        }

        [Test]
        public void PickupSettingsAsset_HasBothVisuals()
        {
            var settings = AssetDatabase.LoadAssetAtPath<PickupSettings>("Assets/_Project/Data/Player/PickupSettings.asset");
            Assert.IsNotNull(settings);
            Assert.IsNotNull(settings.VisualFor(PickupKind.Experience));
            Assert.IsNotNull(settings.VisualFor(PickupKind.Coin));
            Assert.AreNotSame(settings.VisualFor(PickupKind.Experience), settings.VisualFor(PickupKind.Coin), "経験値とコインは形で見分ける");
        }

        [Test]
        public void SkillDefinition_DefaultsToWeight10()
        {
            var def = ScriptableObject.CreateInstance<SkillDefinition>();
            Assert.AreEqual(10, def.Weight);
            Object.DestroyImmediate(def);
        }

        [Test]
        public void SkillCatalogAsset_HasAll26CardsInOrder_WithDesignStackLimits()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<SkillCatalog>("Assets/_Project/Data/Skills/SkillCatalog.asset");
            Assert.IsNotNull(catalog);
            // 設計書 §9 の重ね取りの上限（0 は上限なし＝HP回復）
            var max = new[] { 0, 5, 5, 3, 5, 3, 2, 1, 1, 2, 2, 2, 1, 3, 3, 3, 3, 3, 2, 2, 2, 2, 2, 3, 3, 1 };
            var ids = (SkillId[])System.Enum.GetValues(typeof(SkillId));
            Assert.AreEqual(ids.Length, catalog.Skills.Count);
            for (int i = 0; i < ids.Length; i++)
            {
                var def = catalog.Skills[i];
                Assert.IsNotNull(def, ids[i].ToString());
                Assert.AreEqual(ids[i], def.Id);
                Assert.AreEqual(max[i], def.MaxStacks, ids[i].ToString());
                Assert.AreSame(def, catalog.DefinitionOf(ids[i]));
                // ネタ系は中身が【仮】なので抽選に出さない
                if (ids[i] == SkillId.Wildcard) Assert.AreEqual(0, def.Weight);
                else Assert.Greater(def.Weight, 0, ids[i].ToString());
            }
            Assert.AreEqual(26, catalog.BuildCandidates().Length);
        }

        [Test]
        public void LevelUpSettings_DefaultsMatchSpec()
        {
            var settings = ScriptableObject.CreateInstance<LevelUpSettings>();
            Assert.AreEqual(3, settings.FirstLevelExperience);
            Assert.AreEqual(4, settings.ExperiencePerLevel);
            Assert.AreEqual(3, settings.CardCount);
            Assert.AreEqual(20, settings.CoinCardAmount);
            Object.DestroyImmediate(settings);
        }

        [Test]
        public void LevelUpSettingsAsset_Exists()
            => Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<LevelUpSettings>("Assets/_Project/Data/Player/LevelUpSettings.asset"));

        [Test]
        public void SkillCatalogAsset_EveryCardHasAnIcon()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<SkillCatalog>("Assets/_Project/Data/Skills/SkillCatalog.asset");
            foreach (var def in catalog.Skills) Assert.IsNotNull(def.Icon, def.Id.ToString());
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

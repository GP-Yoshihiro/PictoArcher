using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using PictoArcher.Game;
using UnityEditor;
using UnityEngine;

namespace PictoArcher.Tests.EditMode
{
    /// <summary>仮の敵の定義13種（敵10種・中ボス1種・ボス2種）と、分裂で出る敵が揃っていて、WaveDefinition から指せる形になっているか。</summary>
    public class EnemyDefinitionAssetTests
    {
        static readonly string[] ExpectedNames =
        {
            "Shooter", "Lobber", "SpreadShooter", "BurstShooter", "Chaser", "Bomber", "Burner", "Striker", "Hammerer", "TimeLimiter",
            "Splitter", "BossA", "FinalBoss",
            "SplitterMini",   // 中ボスの分裂で出る敵（ウェーブには入れない）
        };

        static List<EnemyDefinition> LoadAll()
        {
            return AssetDatabase.FindAssets("t:EnemyDefinition", new[] { "Assets/_Project/Data/Enemies" })
                .Select(guid => AssetDatabase.LoadAssetAtPath<EnemyDefinition>(AssetDatabase.GUIDToAssetPath(guid)))
                .ToList();
        }

        [Test] public void ThirteenWaveDefinitionsAndSplitChildExist_WithExpectedNames()
        {
            var names = LoadAll().Select(d => d.name).OrderBy(n => n).ToArray();
            CollectionAssert.AreEqual(ExpectedNames.OrderBy(n => n).ToArray(), names);
        }

        [Test] public void EveryDefinition_HasPositiveValues_Prefab_AndVisual()
        {
            foreach (var definition in LoadAll())
            {
                Assert.Greater(definition.MaxHp, 0, definition.name + " の HP");
                Assert.Greater(definition.MoveSpeed, 0f, definition.name + " の速度");
                Assert.GreaterOrEqual(definition.ContactDamage, 0, definition.name + " の接触ダメージ");
                Assert.GreaterOrEqual(definition.XpReward, 0, definition.name + " の経験値");
                Assert.GreaterOrEqual(definition.CoinReward, 0, definition.name + " のコイン");
                Assert.IsNotNull(definition.Visual, definition.name + " の見た目の表");
                Assert.IsNotNull(definition.Behavior, definition.name + " の動きと攻撃（Behavior）");
                Assert.IsNotNull(definition.Prefab, definition.name + " のプレハブ");
                Assert.IsNotNull(definition.Prefab.GetComponent<EnemyView>(), definition.name + " のプレハブに EnemyView がない");
            }
        }

        [Test] public void BossesAreTougherThanMidBoss_AndMidBossThanNormalEnemies()
        {
            var byName = LoadAll().ToDictionary(d => d.name);
            int strongestNormal = ExpectedNames.Take(10).Max(n => byName[n].MaxHp);
            Assert.Greater(byName["Splitter"].MaxHp, strongestNormal);
            Assert.Greater(byName["BossA"].MaxHp, byName["Splitter"].MaxHp);
            Assert.Greater(byName["FinalBoss"].MaxHp, byName["BossA"].MaxHp);
        }
    }
}

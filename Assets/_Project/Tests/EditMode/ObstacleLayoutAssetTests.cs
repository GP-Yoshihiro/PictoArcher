using System.Collections.Generic;
using NUnit.Framework;
using PictoArcher.Core;
using PictoArcher.Game;
using UnityEditor;
using UnityEngine;

namespace PictoArcher.Tests.EditMode
{
    /// <summary>障害物のパターン（Data/Obstacles）を手で直したときに、壁の外・重なり・閉じ込め・ボス向けの数を確かめる。</summary>
    public class ObstacleLayoutAssetTests
    {
        const string Folder = "Assets/_Project/Data/Obstacles";
        const float BodySize = 1.5f;      // プレイヤーの体より少し大きめ
        const float CellSize = 0.25f;
        const int MaxBossObstacles = 2;

        static List<ObstacleLayout> LoadLayouts()
        {
            var layouts = new List<ObstacleLayout>();
            foreach (var guid in AssetDatabase.FindAssets("t:ObstacleLayout", new[] { Folder }))
                layouts.Add(AssetDatabase.LoadAssetAtPath<ObstacleLayout>(AssetDatabase.GUIDToAssetPath(guid)));
            return layouts;
        }

        [Test] public void SixLayoutsExist() => Assert.AreEqual(6, LoadLayouts().Count);

        [Test]
        public void BothBossAndNormalLayoutsExist()
        {
            var layouts = LoadLayouts();
            Assert.IsTrue(layouts.Exists(l => l.ForBossWaves), "ボス向けのパターンがない");
            Assert.IsTrue(layouts.Exists(l => !l.ForBossWaves), "通常のウェーブ向けのパターンがない");
        }

        [Test]
        public void BossLayouts_HaveFewObstacles()
        {
            var layouts = LoadLayouts();
            int fewestNormal = int.MaxValue;
            foreach (var l in layouts) if (!l.ForBossWaves) fewestNormal = Mathf.Min(fewestNormal, l.Placements.Count);
            var problems = new List<string>();
            foreach (var l in layouts)
            {
                if (!l.ForBossWaves) continue;
                if (l.Placements.Count > MaxBossObstacles) problems.Add($"{l.name} は {l.Placements.Count} 個（{MaxBossObstacles} 個まで）");
                if (l.Placements.Count >= fewestNormal) problems.Add($"{l.name} が通常のパターンより少なくない");
            }
            Assert.IsEmpty(problems, string.Join("、", problems));
        }

        [Test]
        public void Placements_HaveSprites()
        {
            var problems = new List<string>();
            foreach (var l in LoadLayouts())
                for (int i = 0; i < l.Placements.Count; i++)
                    if (l.Placements[i].Sprite == null) problems.Add($"{l.name} の {i} 番目に絵がない");
            Assert.IsEmpty(problems, string.Join("、", problems));
        }

        [Test]
        public void Placements_AreInsideTheInnerArea()
        {
            var area = FieldTestPrefabTests.InnerArea;
            var problems = new List<string>();
            foreach (var l in LoadLayouts())
                for (int i = 0; i < l.Placements.Count; i++)
                {
                    var b = l.Placements[i].Bounds;
                    if (!area.Contains(b.min) || !area.Contains(b.max)) problems.Add($"{l.name} の {i} 番目が壁の内側からはみ出す");
                }
            Assert.IsEmpty(problems, string.Join("、", problems));
        }

        [Test]
        public void Placements_DoNotOverlapEachOther()
        {
            var problems = new List<string>();
            foreach (var l in LoadLayouts())
                for (int i = 0; i < l.Placements.Count; i++)
                    for (int j = i + 1; j < l.Placements.Count; j++)
                        if (l.Placements[i].Bounds.Overlaps(l.Placements[j].Bounds)) problems.Add($"{l.name} の {i} と {j} が重なる");
            Assert.IsEmpty(problems, string.Join("、", problems));
        }

        [Test]
        public void Layouts_NeverTrapThePlayer()
        {
            var problems = new List<string>();
            foreach (var l in LoadLayouts())
            {
                var rects = new List<Rect>();
                for (int i = 0; i < l.Placements.Count; i++) rects.Add(l.Placements[i].Bounds);
                if (!ObstacleReachability.IsAllReachable(FieldTestPrefabTests.InnerArea, rects, BodySize, CellSize)) problems.Add($"{l.name} に閉じ込められる場所がある");
            }
            Assert.IsEmpty(problems, string.Join("、", problems));
        }

        [Test]
        public void SwitchSettings_ShadowFitsInTheRest()
        {
            var settings = AssetDatabase.LoadAssetAtPath<ObstacleSwitchSettings>(Folder + "/ObstacleSwitchSettings.asset");
            Assert.IsNotNull(settings, "ObstacleSwitchSettings.asset がない");
            Assert.That(settings.ShadowSeconds, Is.GreaterThan(0f).And.LessThan(WaveSequence.DefaultRestSeconds));
        }
    }
}

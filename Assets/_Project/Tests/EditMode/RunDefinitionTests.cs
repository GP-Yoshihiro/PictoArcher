using System.Collections.Generic;
using NUnit.Framework;
using PictoArcher.Game;
using UnityEditor;
using UnityEngine;

namespace PictoArcher.Tests.EditMode
{
    /// <summary>50ウェーブの仮の構成（設計書 §6・§8）が RunDefinition のアセットに入っているか。</summary>
    public class RunDefinitionTests
    {
        const string Enemies = "Assets/_Project/Data/Enemies/";

        // 設計書 §8：通常の敵と、初めて出るウェーブ
        static readonly (string name, int firstWave)[] Introduced =
        {
            ("Shooter", 1), ("Chaser", 1), ("SpreadShooter", 6), ("Bomber", 6), ("Lobber", 11),
            ("Hammerer", 11), ("BurstShooter", 16), ("Burner", 16), ("Striker", 21), ("TimeLimiter", 26),
        };

        RunDefinition _run;
        EnemyDefinition _splitter, _bossA, _finalBoss;

        [SetUp]
        public void SetUp()
        {
            _run = AssetDatabase.LoadAssetAtPath<RunDefinition>("Assets/_Project/Data/Waves/RunDefinition.asset");
            _splitter = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(Enemies + "Splitter.asset");
            _bossA = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(Enemies + "BossA.asset");
            _finalBoss = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(Enemies + "FinalBoss.asset");
        }

        static int FirstWaveOf(EnemyDefinition enemy)
        {
            foreach (var (name, first) in Introduced) if (enemy.name == name) return first;
            return -1;
        }

        [Test]
        public void WaveDefinition_DefaultsToTier0()
        {
            var wave = ScriptableObject.CreateInstance<WaveDefinition>();
            Assert.AreEqual(0, wave.Tier);
            Object.DestroyImmediate(wave);
        }

        [Test]
        public void HasFiftyDistinctWaves_StartingAtWave1()
        {
            Assert.AreEqual(50, _run.Waves.Count);
            Assert.AreEqual(1, _run.StartWave);
            var seen = new HashSet<WaveDefinition>();
            for (int n = 1; n <= 50; n++)
            {
                Assert.IsNotNull(_run.WaveFor(n), "ウェーブ " + n);
                Assert.IsTrue(seen.Add(_run.WaveFor(n)), "ウェーブ " + n + " が別のウェーブと同じアセット");
            }
        }

        [Test]
        public void BossWaves_AreAtTheDesignedPlaces_AndAlone()
        {
            for (int n = 1; n <= 50; n++)
            {
                var spawns = _run.WaveFor(n).Spawns;
                EnemyDefinition boss = n == 50 ? _finalBoss : n % 10 == 0 ? _bossA : n % 10 == 5 ? _splitter : null;
                if (boss == null)
                {
                    foreach (var s in spawns)
                    {
                        Assert.AreNotSame(_splitter, s.Enemy, "ウェーブ " + n);
                        Assert.AreNotSame(_bossA, s.Enemy, "ウェーブ " + n);
                        Assert.AreNotSame(_finalBoss, s.Enemy, "ウェーブ " + n);
                    }
                    continue;
                }
                Assert.AreEqual(1, spawns.Count, "ウェーブ " + n + " は中ボス・ボスだけ");
                Assert.AreSame(boss, spawns[0].Enemy, "ウェーブ " + n);
            }
        }

        [Test]
        public void Tier_RisesEveryTenWaves()
        {
            // 中ボスは 5→0、15→1 … 45→4。ボスA は 10→0、20→1 … 40→3（出るたびに強くなる）
            for (int n = 1; n <= 50; n++) Assert.AreEqual((n - 1) / 10, _run.WaveFor(n).Tier, "ウェーブ " + n);
        }

        [Test]
        public void NormalWaves_UseOnlyIntroducedEnemies_AndEachNewEnemyAppearsOnItsFirstWave()
        {
            for (int n = 1; n <= 50; n++)
            {
                if (n % 5 == 0) continue;
                var spawns = _run.WaveFor(n).Spawns;
                Assert.That(spawns.Count, Is.InRange(3, 8), "ウェーブ " + n + " の敵の数");
                foreach (var s in spawns)
                {
                    int first = FirstWaveOf(s.Enemy);
                    Assert.AreNotEqual(-1, first, "ウェーブ " + n + " に通常の敵でない " + s.Enemy.name);
                    Assert.LessOrEqual(first, n, s.Enemy.name + " は " + first + " ウェーブから");
                }
            }
            foreach (var (name, first) in Introduced)
            {
                bool found = false;
                foreach (var s in _run.WaveFor(first).Spawns) found |= s.Enemy.name == name;
                Assert.IsTrue(found, name + " がウェーブ " + first + " に出ない");
            }
        }

        [Test]
        public void EveryWave_HasALayout_BossWavesUseTheBossLayouts()
        {
            Assert.AreEqual("Layout1_Corners", _run.WaveFor(1).ObstacleLayout.name);
            for (int n = 1; n <= 50; n++)
            {
                var layout = _run.WaveFor(n).ObstacleLayout;
                Assert.IsNotNull(layout, "ウェーブ " + n);
                Assert.AreEqual(n % 5 == 0, layout.ForBossWaves, "ウェーブ " + n + " の障害物の並び");
            }
        }

        [Test]
        public void Spawns_AreInsideTheFloor_AndNotOnObstacles()
        {
            for (int n = 1; n <= 50; n++)
            {
                var wave = _run.WaveFor(n);
                foreach (var s in wave.Spawns)
                {
                    Assert.That(Mathf.Abs(s.Position.x), Is.LessThanOrEqualTo(6.5f), "ウェーブ " + n);
                    Assert.That(Mathf.Abs(s.Position.y), Is.LessThanOrEqualTo(11f), "ウェーブ " + n);
                    foreach (var p in wave.ObstacleLayout.Placements)
                    {
                        var r = p.Bounds;
                        var grown = new Rect(r.x - 1f, r.y - 1f, r.width + 2f, r.height + 2f);
                        Assert.IsFalse(grown.Contains(s.Position), "ウェーブ " + n + " の " + s.Position + " が障害物に近い");
                    }
                }
            }
        }
    }
}

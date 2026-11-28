using System.Collections.Generic;
using NUnit.Framework;
using PictoArcher.Game;
using UnityEditor;
using UnityEngine;

namespace PictoArcher.Tests.EditMode
{
    /// <summary>
    /// 最後のボス（ウェーブ50）の動き：近づく → 突進（赤い線）→ 近づく → 弾幕（光ってから、赤い線）→ 近づく → 雑魚を呼ぶ → くり返す。
    /// HP が半分を切ると色と光り方が変わり、攻撃の間が短くなる。
    /// </summary>
    public class FinalBossBehaviorTests
    {
        sealed class FakePort : IEnemyAttackPort
        {
            public readonly List<(int slot, Vector2 direction, float length)> Lines = new List<(int, Vector2, float)>();
            public readonly List<Vector2> Shots = new List<Vector2>();
            public readonly List<Vector2> Summons = new List<Vector2>();
            public readonly List<(Color color, float amount)> Glows = new List<(Color, float)>();
            public readonly List<Color> Tints = new List<Color>();
            public void ShowLine(int slot, Vector2 origin, Vector2 direction, float length, float width, float progress) => Lines.Add((slot, direction, length));
            public void ShowCircle(Vector2 center, float radius, float progress) { }
            public void Shoot(EnemyProjectileDefinition projectile, Vector2 direction) => Shots.Add(direction);
            public void SelfDestruct(float radius, int damage) { }
            public void Summon(EnemyDefinition enemy, Vector2 position, float seconds) => Summons.Add(position);
            public void SetTint(Color color) => Tints.Add(color);
            public void ShowGlow(Color color, float amount) => Glows.Add((color, amount));
        }

        const float Step = 0.05f;
        static readonly Color Tint = new Color(0.8f, 0.8f, 0.8f, 1f);
        static readonly Color EnragedTint = new Color(0.5f, 0.4f, 0.4f, 1f);
        static readonly Color Glow = Color.white;
        static readonly Color EnragedGlow = new Color(1f, 0.6f, 0.8f, 1f);

        FinalBossBehaviorDefinition _definition;
        EnemyDefinition _minion;
        EnemyProjectileDefinition _projectile;
        FakePort _port;

        [SetUp]
        public void SetUp()
        {
            _minion = ScriptableObject.CreateInstance<EnemyDefinition>();
            _projectile = ScriptableObject.CreateInstance<EnemyProjectileDefinition>();
            _definition = ScriptableObject.CreateInstance<FinalBossBehaviorDefinition>();
            var so = new SerializedObject(_definition);
            so.FindProperty("_approachSeconds").floatValue = 1f;
            so.FindProperty("_enrageHpRatio").floatValue = 0.5f;
            so.FindProperty("_enragedIntervalScale").floatValue = 0.5f;
            so.FindProperty("_chargeWindupSeconds").floatValue = 0.5f;
            so.FindProperty("_chargeSpeed").floatValue = 10f;
            so.FindProperty("_chargeDistance").floatValue = 5f;
            so.FindProperty("_projectile").objectReferenceValue = _projectile;
            so.FindProperty("_barrageWindupSeconds").floatValue = 0.5f;
            so.FindProperty("_barrageCount").intValue = 8;
            so.FindProperty("_enragedBarrageCount").intValue = 12;
            so.FindProperty("_barrageLineLength").floatValue = 4f;
            so.FindProperty("_summonWindupSeconds").floatValue = 0.5f;
            so.FindProperty("_summonEnemy").objectReferenceValue = _minion;
            so.FindProperty("_summonCount").intValue = 3;
            so.FindProperty("_summonDistance").floatValue = 3f;
            so.FindProperty("_summonArea").rectValue = new Rect(-6f, -10f, 12f, 20f);
            so.FindProperty("_tint").colorValue = Tint;
            so.FindProperty("_enragedTint").colorValue = EnragedTint;
            so.FindProperty("_glowColor").colorValue = Glow;
            so.FindProperty("_enragedGlowColor").colorValue = EnragedGlow;
            so.FindProperty("_glowFlashesPerSecond").floatValue = 2f;
            so.FindProperty("_enragedGlowFlashesPerSecond").floatValue = 6f;
            so.ApplyModifiedPropertiesWithoutUndo();
            _port = new FakePort();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_definition);
            Object.DestroyImmediate(_projectile);
            Object.DestroyImmediate(_minion);
        }

        static readonly Vector2 Self = Vector2.zero;
        static readonly Vector2 Target = new Vector2(0f, -8f);

        /// <summary>seconds 秒ぶん Tick し、最後の速度を返す。</summary>
        Vector2 Run(IEnemyBehavior behavior, float seconds, float hpRatio = 1f)
        {
            var velocity = Vector2.zero;
            for (float t = 0f; t < seconds - 1e-4f; t += Step) velocity = behavior.Tick(Step, new EnemyContext(Self, Target, 2f, hpRatio), _port);
            return velocity;
        }

        [Test]
        public void ThreePatterns_InOrder_EachWithATelegraph()
        {
            var behavior = _definition.Create();
            Assert.Greater(Run(behavior, 1f).magnitude, 0f, "はじめは近づく");
            Assert.AreEqual(0, _port.Lines.Count);

            // 1. 突進：赤い線で予告して止まり、線の向きへ進む
            Assert.AreEqual(Vector2.zero, Run(behavior, 0.5f), "突進の予告の間は止まる");
            Assert.Greater(_port.Lines.Count, 0, "突進の前に赤い線");
            Assert.AreEqual(5f, _port.Lines[0].length, 1e-4f);
            Assert.AreEqual(0f, Vector2.Distance(Vector2.down * 10f, Run(behavior, 0.1f)), 1e-3f, "線の向きへ突進する");
            Run(behavior, 0.4f);
            Assert.AreEqual(0, _port.Shots.Count, "突進では撃たない");

            // 2. 弾幕：近づく → 光りながら赤い線を全方向に出す → 撃つ
            Run(behavior, 1f);
            _port.Lines.Clear();
            Assert.AreEqual(Vector2.zero, Run(behavior, 0.5f), "弾幕の予告の間は止まる");
            Assert.AreEqual(0, _port.Shots.Count, "予告が終わるまで撃たない");
            Assert.Greater(_port.Glows.Count, 0, "撃つ前に光る");
            foreach (var g in _port.Glows) Assert.AreEqual(Glow, g.color);
            var slots = new HashSet<int>();
            foreach (var line in _port.Lines) slots.Add(line.slot);
            Assert.AreEqual(8, slots.Count, "弾の数だけ赤い線");
            Run(behavior, 0.05f);
            Assert.AreEqual(8, _port.Shots.Count, "予告のあとに全方向へ撃つ");
            AssertEvenRing(_port.Shots);
            Assert.AreEqual(0, _port.Summons.Count, "弾幕では呼ばない");

            // 3. 雑魚を呼ぶ：近づく → 予兆つきで呼ぶ
            Run(behavior, 1f);
            Assert.AreEqual(Vector2.zero, Run(behavior, 0.1f), "呼んでいる間は止まる");
            Assert.AreEqual(3, _port.Summons.Count);
            var area = new Rect(-6f, -10f, 12f, 20f);
            foreach (var p in _port.Summons)
            {
                Assert.That(p.x, Is.InRange(area.xMin, area.xMax));
                Assert.That(p.y, Is.InRange(area.yMin, area.yMax));
            }

            // はじめ（突進）に戻る
            _port.Lines.Clear();
            Run(behavior, 0.4f + 1f + 0.2f);
            Assert.Greater(_port.Lines.Count, 0, "また突進の予告");
            Assert.AreEqual(8, _port.Shots.Count, "続けて弾幕は撃たない");
        }

        [Test]
        public void BelowHalfHp_ChangesColorAndGlow_AndShortensTheInterval()
        {
            var behavior = _definition.Create();
            Run(behavior, 0.05f);
            Assert.AreEqual(Tint, _port.Tints[_port.Tints.Count - 1], "はじめの色");

            Run(behavior, 0.05f, 0.5f);
            Assert.AreEqual(Tint, _port.Tints[_port.Tints.Count - 1], "ちょうど半分はまだ変わらない");
            Run(behavior, 0.05f, 0.49f);
            Assert.AreEqual(EnragedTint, _port.Tints[_port.Tints.Count - 1], "半分を切ると色が変わる");
            int tints = _port.Tints.Count;
            Run(behavior, 0.05f, 0.49f);
            Assert.AreEqual(tints, _port.Tints.Count, "色は変わるときだけ入れる");

            // 近づく秒数が半分（1 → 0.5）になる。今の近づく（0.2 秒経過）も 0.5 秒で終わる
            Run(behavior, 0.3f, 0.49f);
            _port.Lines.Clear();
            Run(behavior, 0.5f + 0.5f, 0.49f);   // 予告 → 突進
            Run(behavior, 0.5f, 0.49f);         // 近づく（短くなった）
            _port.Lines.Clear();
            _port.Glows.Clear();
            Run(behavior, 0.5f, 0.49f);
            Assert.Greater(_port.Glows.Count, 0, "短い間のあとに弾幕の予告が始まる");
            foreach (var g in _port.Glows) Assert.AreEqual(EnragedGlow, g.color, "光る色も変わる");
            Run(behavior, 0.05f, 0.49f);
            Assert.AreEqual(12, _port.Shots.Count, "弾の数が増える");
            AssertEvenRing(_port.Shots);
        }

        [Test]
        public void BelowHalfHp_GlowFlashesFaster()
        {
            int Flips(float hpRatio)
            {
                var behavior = _definition.Create();
                _port.Glows.Clear();
                Run(behavior, 1f + 0.5f + 0.5f, hpRatio);
                Run(behavior, hpRatio < 0.5f ? 0f : 1f, hpRatio);   // 半分を切ると、近づく秒数が半分なので、ここで弾幕の予告の手前
                _port.Glows.Clear();
                Run(behavior, 0.5f, hpRatio);
                int flips = 0;
                for (int i = 2; i < _port.Glows.Count; i++)
                {
                    float before = _port.Glows[i - 1].amount - _port.Glows[i - 2].amount;
                    float now = _port.Glows[i].amount - _port.Glows[i - 1].amount;
                    if (before * now < 0f) flips++;
                }
                return flips;
            }
            Assert.Greater(Flips(0.4f), Flips(1f), "半分を切ると速く点滅する");
        }

        [Test]
        public void Definition_DefaultsBuildAValidPattern()
        {
            var def = ScriptableObject.CreateInstance<FinalBossBehaviorDefinition>();
            Assert.Greater(def.ApproachSeconds, 0f);
            Assert.That(def.EnragedIntervalScale, Is.InRange(0.01f, 0.99f), "半分を切ったら短くなる");
            Assert.AreEqual(0.5f, def.EnrageHpRatio);
            Assert.Greater(def.ChargeWindupSeconds, 0f);
            Assert.Greater(def.BarrageWindupSeconds, 0f);
            Assert.Greater(def.EnragedBarrageCount, def.BarrageCount);
            Assert.Greater(def.EnragedGlowFlashesPerSecond, def.GlowFlashesPerSecond);
            Assert.AreNotEqual(def.Tint, def.EnragedTint);
            Assert.AreNotEqual(def.GlowColor, def.EnragedGlowColor);
            Object.DestroyImmediate(def);
        }

        [Test]
        public void FinalBossAsset_UsesThisBehavior_WithAProjectileAndMinion()
        {
            var boss = AssetDatabase.LoadAssetAtPath<EnemyDefinition>("Assets/_Project/Data/Enemies/FinalBoss.asset");
            var behavior = boss.Behavior as FinalBossBehaviorDefinition;
            Assert.IsNotNull(behavior, "最後のボスは FinalBossBehavior");
            Assert.IsNotNull(behavior.Projectile);
            Assert.IsNotNull(behavior.SummonEnemy);
            Assert.IsTrue(boss.ShowsBossBar);
        }

        static void AssertEvenRing(List<Vector2> shots)
        {
            int n = shots.Count;
            var sum = Vector2.zero;
            foreach (var s in shots)
            {
                Assert.AreEqual(1f, s.magnitude, 1e-4f);
                sum += s;
            }
            Assert.Less(sum.magnitude, 1e-3f, "全方向に等間隔");
            float step = 360f / n;
            for (int i = 1; i < n; i++) Assert.AreEqual(step, Vector2.Angle(shots[i - 1], shots[i]), 1e-2f, "隣どうしの角度");
        }
    }
}

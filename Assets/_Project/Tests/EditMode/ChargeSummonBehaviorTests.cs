using System.Collections.Generic;
using NUnit.Framework;
using PictoArcher.Game;
using UnityEditor;
using UnityEngine;

namespace PictoArcher.Tests.EditMode
{
    /// <summary>ボスA（ウェーブ10の形）の動き：近づく → 赤い線で予告して突進 → 近づく → 赤い予兆つきで雑魚を呼ぶ → くり返す。</summary>
    public class ChargeSummonBehaviorTests
    {
        sealed class FakePort : IEnemyAttackPort
        {
            public readonly List<(Vector2 origin, Vector2 direction, float length, float progress)> Lines = new List<(Vector2, Vector2, float, float)>();
            public readonly List<(EnemyDefinition enemy, Vector2 position, float seconds)> Summons = new List<(EnemyDefinition, Vector2, float)>();
            public void ShowLine(int slot, Vector2 origin, Vector2 direction, float length, float width, float progress) => Lines.Add((origin, direction, length, progress));
            public void ShowCircle(Vector2 center, float radius, float progress) { }
            public void Shoot(EnemyProjectileDefinition projectile, Vector2 direction) { }
            public void SelfDestruct(float radius, int damage) { }
            public void Summon(EnemyDefinition enemy, Vector2 position, float seconds) => Summons.Add((enemy, position, seconds));
            public void SetTint(Color color) { }
            public void ShowGlow(Color color, float amount) { }
        }

        const float Step = 0.05f;
        ChargeSummonBehaviorDefinition _definition;
        EnemyDefinition _minion;
        FakePort _port;

        [SetUp]
        public void SetUp()
        {
            _minion = ScriptableObject.CreateInstance<EnemyDefinition>();
            _definition = ScriptableObject.CreateInstance<ChargeSummonBehaviorDefinition>();
            var so = new SerializedObject(_definition);
            so.FindProperty("_approachSeconds").floatValue = 1f;
            so.FindProperty("_chargeWindupSeconds").floatValue = 0.5f;
            so.FindProperty("_chargeSpeed").floatValue = 10f;
            so.FindProperty("_chargeDistance").floatValue = 5f;
            so.FindProperty("_summonWindupSeconds").floatValue = 0.8f;
            so.FindProperty("_summonEnemy").objectReferenceValue = _minion;
            so.FindProperty("_summonCount").intValue = 2;
            so.FindProperty("_summonDistance").floatValue = 3f;
            so.FindProperty("_summonArea").rectValue = new Rect(-6f, -10f, 12f, 20f);
            so.ApplyModifiedPropertiesWithoutUndo();
            _port = new FakePort();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_definition);
            Object.DestroyImmediate(_minion);
        }

        static EnemyContext At(Vector2 self, Vector2 target) => new EnemyContext(self, target, 2f);

        /// <summary>seconds 秒ぶん Tick し、最後の速度を返す。</summary>
        Vector2 Run(IEnemyBehavior behavior, float seconds, Vector2 self, Vector2 target)
        {
            var velocity = Vector2.zero;
            for (float t = 0f; t < seconds - 1e-4f; t += Step) velocity = behavior.Tick(Step, At(self, target), _port);
            return velocity;
        }

        [Test]
        public void Approaches_ThenShowsRedLine_ThenChargesAlongTheLockedLine()
        {
            var behavior = _definition.Create();
            var velocity = Run(behavior, 1f, Vector2.zero, new Vector2(0f, -8f));
            Assert.Greater(velocity.magnitude, 0f, "はじめは近づく");
            Assert.AreEqual(0, _port.Lines.Count, "近づいている間は予告しない");

            // 予告の間に相手が動いても、線の向きは予告の始めに決めたまま
            velocity = behavior.Tick(Step, At(Vector2.zero, new Vector2(0f, -8f)), _port);
            Assert.AreEqual(Vector2.zero, velocity, "予告の間は止まる");
            velocity = Run(behavior, 0.4f, Vector2.zero, new Vector2(8f, 0f));
            Assert.AreEqual(Vector2.zero, velocity);
            Assert.Greater(_port.Lines.Count, 1);
            foreach (var line in _port.Lines)
            {
                Assert.AreEqual(0f, Vector2.Distance(Vector2.down, line.direction), 1e-4f, "線は相手へ向く");
                Assert.AreEqual(5f, line.length, 1e-4f, "線の長さは突進の距離");
            }
            Assert.Greater(_port.Lines[_port.Lines.Count - 1].progress, _port.Lines[0].progress, "線が満ちていく");

            int linesBefore = _port.Lines.Count;
            velocity = Run(behavior, 0.1f, Vector2.zero, new Vector2(8f, 0f));
            Assert.AreEqual(0f, Vector2.Distance(Vector2.down * 10f, velocity), 1e-3f, "予告した線の向きへ突進する");
            int linesDuringCharge = _port.Lines.Count - linesBefore;
            Assert.LessOrEqual(linesDuringCharge, 1, "突進が始まったら線は出さない");
        }

        [Test]
        public void AfterTheCharge_SummonsMinionsWithTelegraph_InsideTheArea_ThenChargesAgain()
        {
            var behavior = _definition.Create();
            var self = new Vector2(5f, 9f);   // 端の近く。呼ぶ位置は場の中に収める
            Run(behavior, 1f + 0.5f + 0.5f, self, Vector2.zero);   // 近づく → 予告 → 突進（5 / 10 = 0.5 秒）
            Assert.AreEqual(0, _port.Summons.Count, "突進までは呼ばない");

            Run(behavior, 1f, self, Vector2.zero);   // もう一度近づく
            var velocity = Run(behavior, 0.1f, self, Vector2.zero);
            Assert.AreEqual(2, _port.Summons.Count, "決めた数だけ1回で呼ぶ");
            Assert.AreEqual(Vector2.zero, velocity, "呼んでいる間は止まる");
            var area = new Rect(-6f, -10f, 12f, 20f);
            foreach (var s in _port.Summons)
            {
                Assert.AreSame(_minion, s.enemy);
                Assert.AreEqual(0.8f, s.seconds, 1e-4f, "出る前に予兆（赤い ! 円）を出す");
                // Rect.Contains は右と上の端を含まないので、端も含めて比べる
                Assert.That(s.position.x, Is.InRange(area.xMin, area.xMax), s.position + " は場の外");
                Assert.That(s.position.y, Is.InRange(area.yMin, area.yMax), s.position + " は場の外");
            }
            Assert.AreNotEqual(_port.Summons[0].position, _port.Summons[1].position);

            Run(behavior, 0.7f + 1f, self, Vector2.zero);   // 呼び終わる → 近づく
            int lines = _port.Lines.Count;
            Run(behavior, 0.2f, self, Vector2.zero);
            Assert.Greater(_port.Lines.Count, lines, "また突進の予告に戻る");
            Assert.AreEqual(2, _port.Summons.Count, "続けては呼ばない");
        }

        [Test]
        public void Definition_DefaultsBuildAValidPattern()
        {
            var def = ScriptableObject.CreateInstance<ChargeSummonBehaviorDefinition>();
            Assert.Greater(def.ApproachSeconds, 0f);
            Assert.Greater(def.ChargeWindupSeconds, 0f);
            Assert.Greater(def.ChargeSpeed, 0f);
            Assert.Greater(def.ChargeDistance, 0f);
            Assert.Greater(def.SummonWindupSeconds, 0f);
            Assert.Greater(def.SummonCount, 0);
            Object.DestroyImmediate(def);
        }
    }
}

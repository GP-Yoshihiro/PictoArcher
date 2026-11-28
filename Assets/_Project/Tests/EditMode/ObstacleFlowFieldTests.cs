using System;
using System.Collections.Generic;
using NUnit.Framework;
using PictoArcher.Core;
using UnityEngine;

namespace PictoArcher.Tests.EditMode
{
    public class ObstacleFlowFieldTests
    {
        // 戦える広さ（壁の内側）と同じ 15×25 ユニット
        static readonly Rect Area = new Rect(-7.5f, -14f, 15f, 25f);
        const float CellSize = 0.5f;
        const float Clearance = 0.6f;
        const int LookAhead = 16;

        static ObstacleFlowField NewField(float clearance = Clearance) => new ObstacleFlowField(Area, CellSize, clearance, LookAhead);

        // 敵が NextPoint へ少しずつ歩く。目標に着くか、歩数を使い切ったら止まる
        static List<Vector2> Walk(ObstacleFlowField field, Vector2 start, Vector2 target, int maxSteps = 2000, float step = 0.05f)
        {
            var path = new List<Vector2> { start };
            var position = start;
            for (int i = 0; i < maxSteps; i++)
            {
                if ((target - position).magnitude <= step) break;
                var toNext = field.NextPoint(position) - position;
                if (toNext.sqrMagnitude < 1e-10f) break;
                position += toNext.normalized * Mathf.Min(step, toNext.magnitude);
                path.Add(position);
            }
            return path;
        }

        static Rect Inflate(Rect rect, float amount) => Rect.MinMaxRect(rect.xMin - amount, rect.yMin - amount, rect.xMax + amount, rect.yMax + amount);

        // 敵の中心が、半径ぶん広げた障害物の中へ入っていない（縁に沿って歩くのはよい）
        static void AssertStaysOutside(List<Vector2> path, IReadOnlyList<Rect> obstacles, float clearance)
        {
            for (int i = 0; i < obstacles.Count; i++)
            {
                var inner = Inflate(obstacles[i], clearance - 0.01f);
                for (int p = 0; p < path.Count; p++)
                {
                    Assert.IsFalse(inner.Contains(path[p]), $"{p} 歩目 {path[p]} が障害物 {obstacles[i]} にめり込んでいます");
                }
            }
        }

        [Test] public void NextPoint_NothingInTheWay_IsTheTarget()
        {
            var field = NewField();
            field.SetObstacles(new[] { new Rect(4f, 4f, 2f, 2f) });
            field.SetTarget(new Vector2(0f, 5f));
            Assert.AreEqual(new Vector2(0f, 5f), field.NextPoint(new Vector2(0f, -5f)));
        }

        [Test] public void NextPoint_WallInTheWay_IsNotTheTarget_AndCanBeWalkedStraightTo()
        {
            var wall = new Rect(-3f, -0.5f, 6f, 1f);
            var field = NewField();
            field.SetObstacles(new[] { wall });
            field.SetTarget(new Vector2(0f, 5f));
            var self = new Vector2(0f, -5f);

            var next = field.NextPoint(self);

            Assert.AreNotEqual(new Vector2(0f, 5f), next);
            AssertStaysOutside(new List<Vector2> { self, Vector2.Lerp(self, next, 0.5f), next }, new[] { wall }, Clearance);
        }

        [Test] public void Walk_AroundAWall_ReachesTheTarget()
        {
            var obstacles = new[] { new Rect(-3f, -0.5f, 6f, 1f) };
            var field = NewField();
            field.SetObstacles(obstacles);
            var target = new Vector2(0f, 5f);
            field.SetTarget(target);

            var path = Walk(field, new Vector2(0f, -5f), target);

            Assert.Less((path[path.Count - 1] - target).magnitude, 0.1f, "目標に着いていません");
            AssertStaysOutside(path, obstacles, Clearance);
        }

        [Test] public void Walk_OutOfACup_ReachesTheTargetBehindIt()
        {
            // 敵は上が開いたコの字の中、目標はコの字の底の向こう（下）
            var obstacles = new[]
            {
                new Rect(-3f, -2f, 6f, 1f),    // 底
                new Rect(-3f, -1f, 1f, 5f),    // 左の壁
                new Rect(2f, -1f, 1f, 5f),     // 右の壁
            };
            var field = NewField();
            field.SetObstacles(obstacles);
            var target = new Vector2(0f, -8f);
            field.SetTarget(target);

            var path = Walk(field, new Vector2(0f, 1f), target, maxSteps: 4000);

            Assert.Less((path[path.Count - 1] - target).magnitude, 0.1f, "コの字から出られていません");
            AssertStaysOutside(path, obstacles, Clearance);
        }

        [Test] public void Walk_GapNarrowerThanTheBody_GoesAroundInstead()
        {
            // 2つの障害物の間の隙間は 0.8。半径 0.6 の体（幅 1.2）は通れない
            var obstacles = new[]
            {
                new Rect(-5f, -0.5f, 4.6f, 1f),
                new Rect(0.4f, -0.5f, 4.6f, 1f),
            };
            var field = NewField();
            field.SetObstacles(obstacles);
            var target = new Vector2(0f, 5f);
            field.SetTarget(target);

            var path = Walk(field, new Vector2(0f, -5f), target, maxSteps: 4000);

            Assert.Less((path[path.Count - 1] - target).magnitude, 0.1f, "外を回って着いていません");
            AssertStaysOutside(path, obstacles, Clearance);
        }

        [Test] public void Walk_GapWideEnough_GoesThrough()
        {
            // 隙間 2.0 は半径 0.6 の体が通れる。外を回るより短いので隙間を通る
            var obstacles = new[]
            {
                new Rect(-7.5f, -0.5f, 6.5f, 1f),
                new Rect(1f, -0.5f, 6.5f, 1f),
            };
            var field = NewField();
            field.SetObstacles(obstacles);
            var target = new Vector2(0f, 5f);
            field.SetTarget(target);

            var path = Walk(field, new Vector2(0f, -5f), target);

            Assert.Less((path[path.Count - 1] - target).magnitude, 0.1f);
            AssertStaysOutside(path, obstacles, Clearance);
        }

        [Test] public void Walk_TargetPressedAgainstAnObstacle_GetsClose()
        {
            // プレイヤーが障害物に背中をつけている（広げた範囲の中にいる）
            var obstacles = new[] { new Rect(-3f, 0f, 6f, 1f) };
            var field = NewField();
            field.SetObstacles(obstacles);
            var target = new Vector2(0f, 1.3f);
            field.SetTarget(target);

            var path = Walk(field, new Vector2(0f, -5f), target, maxSteps: 4000);

            // 最後は目標へまっすぐ向かう。実際の体は障害物の面で止まり、プレイヤーに触れる（この歩きは物理がないので通り抜ける）
            Assert.Less((path[path.Count - 1] - target).magnitude, Clearance + CellSize, "目標のそばまで来ていません");
        }

        [Test] public void Walk_TargetAtTheAreaEdge_GoesAroundAPillarInsteadOfThroughIt()
        {
            // プレイヤーが右の壁ぎわ（縁の帯の中）にいて、敵は柱の反対側の左の壁ぎわにいる。
            // 縁の帯は外周をぐるっとつながっているので、帯をたどって始まりを決めると、左の壁ぎわまで「まっすぐ」になり柱へ歩き続けた
            var pillar = new[] { new Rect(4f, -1f, 1f, 2f) };
            var field = NewField();
            field.SetObstacles(pillar);
            var target = new Vector2(7.2f, 0f);
            field.SetTarget(target);
            var start = new Vector2(-6.7f, 0.1f);

            Assert.AreNotEqual(target, field.NextPoint(start), "柱の向こうから目標へまっすぐ向かっています");
            var path = Walk(field, start, target, maxSteps: 6000);
            AssertStaysOutside(path, pillar, Clearance);
            Assert.Less((path[path.Count - 1] - target).magnitude, Clearance + CellSize, "目標のそばまで来ていません");
        }

        [Test] public void NextPoint_TargetEnclosed_FallsBackToTheTarget()
        {
            // 目標が囲まれていて道がない。そのときは今までどおりまっすぐ向かう
            var obstacles = new[]
            {
                new Rect(-3f, 2f, 6f, 1f),
                new Rect(-3f, 7f, 6f, 1f),
                new Rect(-3f, 3f, 1f, 4f),
                new Rect(2f, 3f, 1f, 4f),
            };
            var field = NewField();
            field.SetObstacles(obstacles);
            var target = new Vector2(0f, 5f);
            field.SetTarget(target);

            Assert.AreEqual(target, field.NextPoint(new Vector2(0f, -5f)));
        }

        [Test] public void NextPoint_SelfOutsideTheArea_IsTheTarget()
        {
            var field = NewField();
            field.SetObstacles(new[] { new Rect(-3f, -0.5f, 6f, 1f) });
            field.SetTarget(new Vector2(0f, 5f));
            Assert.AreEqual(new Vector2(0f, 5f), field.NextPoint(new Vector2(0f, -20f)));
        }

        [Test] public void NextPoint_SelfTouchingTheObstacle_StillGetsOut()
        {
            // 体が障害物の下の面に触れている（中心は広げた範囲の縁）
            var obstacles = new[] { new Rect(-3f, -0.5f, 6f, 1f) };
            var field = NewField();
            field.SetObstacles(obstacles);
            var target = new Vector2(0f, 5f);
            field.SetTarget(target);

            var path = Walk(field, new Vector2(0f, -0.5f - Clearance), target);

            Assert.Less((path[path.Count - 1] - target).magnitude, 0.1f);
            AssertStaysOutside(path, obstacles, Clearance);
        }

        [Test] public void Walk_BiggerBody_KeepsBiggerDistance()
        {
            const float big = 1.2f;
            var obstacles = new[] { new Rect(-3f, -0.5f, 6f, 1f) };
            var field = NewField(big);
            field.SetObstacles(obstacles);
            var target = new Vector2(0f, 5f);
            field.SetTarget(target);

            var path = Walk(field, new Vector2(0f, -5f), target);

            Assert.Less((path[path.Count - 1] - target).magnitude, 0.1f);
            AssertStaysOutside(path, obstacles, big);
        }

        [Test] public void NextPoint_NoObstacles_NearTheAreaEdge_IsStillTheTarget()
        {
            // 障害物がなければ、壁の近くでもまっすぐ向かう（今までと同じ動き）
            var field = NewField();
            field.SetObstacles(Array.Empty<Rect>());
            var target = new Vector2(7f, 10f);
            field.SetTarget(target);
            Assert.AreEqual(target, field.NextPoint(new Vector2(-7f, -13f)));
        }

        [Test] public void SetTarget_WithinTheSameCell_DoesNotRebuild()
        {
            var field = NewField();
            field.SetObstacles(new[] { new Rect(-3f, -0.5f, 6f, 1f) });
            field.SetTarget(new Vector2(0.1f, 5.1f));
            int built = field.BuildCount;

            field.SetTarget(new Vector2(0.2f, 5.2f));

            Assert.AreEqual(built, field.BuildCount);
        }

        [Test] public void SetTarget_ToAnotherCell_Rebuilds()
        {
            var field = NewField();
            field.SetObstacles(new[] { new Rect(-3f, -0.5f, 6f, 1f) });
            field.SetTarget(new Vector2(0.1f, 5.1f));
            int built = field.BuildCount;

            field.SetTarget(new Vector2(1.1f, 5.1f));

            Assert.AreEqual(built + 1, field.BuildCount);
        }

        [Test] public void SetObstacles_ThenSameTarget_Rebuilds()
        {
            var field = NewField();
            field.SetObstacles(new[] { new Rect(-3f, -0.5f, 6f, 1f) });
            field.SetTarget(new Vector2(0f, 5f));
            int built = field.BuildCount;

            field.SetObstacles(new[] { new Rect(-5f, -0.5f, 6f, 1f) });
            field.SetTarget(new Vector2(0f, 5f));

            Assert.AreEqual(built + 1, field.BuildCount);
        }

        [Test] public void NextPoint_AfterObstaclesChange_UsesTheNewObstacles()
        {
            var field = NewField();
            field.SetObstacles(new[] { new Rect(-3f, -0.5f, 6f, 1f) });
            field.SetTarget(new Vector2(0f, 5f));
            Assume.That(field.NextPoint(new Vector2(0f, -5f)), Is.Not.EqualTo(new Vector2(0f, 5f)));

            field.SetObstacles(Array.Empty<Rect>());
            field.SetTarget(new Vector2(0f, 5f));

            Assert.AreEqual(new Vector2(0f, 5f), field.NextPoint(new Vector2(0f, -5f)));
        }

        [Test] public void NextPoint_BeforeAnyTarget_IsTheSelfPosition()
        {
            var field = NewField();
            Assert.AreEqual(new Vector2(1f, 2f), field.NextPoint(new Vector2(1f, 2f)));
        }

        [Test] public void Constructor_BadValues_Throw()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new ObstacleFlowField(Area, 0f, Clearance, LookAhead));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ObstacleFlowField(Area, CellSize, -0.1f, LookAhead));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ObstacleFlowField(Area, CellSize, Clearance, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ObstacleFlowField(new Rect(0f, 0f, 0f, 5f), CellSize, Clearance, LookAhead));
        }

        [Test] public void SetObstacles_Null_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => NewField().SetObstacles(null));
        }
    }
}

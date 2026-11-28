using System;
using NUnit.Framework;
using PictoArcher.Core;
using UnityEngine;

namespace PictoArcher.Tests.EditMode
{
    public class ObstacleReachabilityTests
    {
        static readonly Rect Area = new Rect(0f, 0f, 10f, 10f);
        const float Body = 1f;
        const float Cell = 0.25f;

        static bool Reachable(params Rect[] obstacles) => ObstacleReachability.IsAllReachable(Area, obstacles, Body, Cell);

        [Test] public void EmptyArea_IsReachable() => Assert.IsTrue(Reachable());
        [Test] public void WallAcrossArea_IsNotReachable() => Assert.IsFalse(Reachable(new Rect(0f, 4f, 10f, 2f)));

        [Test]
        public void ClosedRoomWithNarrowDoor_IsNotReachable()
            => Assert.IsFalse(Reachable(new Rect(6f, 6f, 1.5f, 0.5f), new Rect(8f, 6f, 2f, 0.5f), new Rect(6f, 6f, 0.5f, 4f)));

        [Test]
        public void RoomWithWideDoor_IsReachable()
            => Assert.IsTrue(Reachable(new Rect(6f, 6f, 0.5f, 0.5f), new Rect(8.5f, 6f, 1.5f, 0.5f), new Rect(6f, 6f, 0.5f, 4f)));

        [Test]
        public void ScatteredBoxes_AreReachable()
            => Assert.IsTrue(Reachable(new Rect(2f, 2f, 2f, 2f), new Rect(6f, 2f, 2f, 2f), new Rect(2f, 6f, 2f, 2f)));

        [Test] public void AreaFullyBlocked_IsNotReachable() => Assert.IsFalse(Reachable(new Rect(0f, 0f, 10f, 10f)));

        [Test]
        public void NonPositiveSizes_Throw()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => ObstacleReachability.IsAllReachable(Area, Array.Empty<Rect>(), 0f, Cell));
            Assert.Throws<ArgumentOutOfRangeException>(() => ObstacleReachability.IsAllReachable(Area, Array.Empty<Rect>(), Body, 0f));
        }
    }
}

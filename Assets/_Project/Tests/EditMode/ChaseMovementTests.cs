using NUnit.Framework;
using PictoArcher.Core;
using UnityEngine;

namespace PictoArcher.Tests.EditMode
{
    public class ChaseMovementTests
    {
        [Test] public void Velocity_PointsAtTarget_WithSpeedAsLength()
        {
            var chase = new ChaseMovement(2.5f);
            var velocity = chase.Velocity(new Vector2(1f, 1f), new Vector2(1f, 11f));
            Assert.AreEqual(0f, velocity.x, 1e-5f);
            Assert.AreEqual(2.5f, velocity.y, 1e-5f);
        }

        [Test] public void Velocity_Diagonal_KeepsSpeed()
        {
            var chase = new ChaseMovement(3f);
            Assert.AreEqual(3f, chase.Velocity(Vector2.zero, new Vector2(4f, -7f)).magnitude, 1e-4f);
        }

        [Test] public void Velocity_OnTarget_IsZero()
        {
            var chase = new ChaseMovement(2.5f);
            Assert.AreEqual(Vector2.zero, chase.Velocity(new Vector2(2f, 3f), new Vector2(2f, 3f)));
        }

        [Test] public void Constructor_NegativeSpeed_Throws()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() => new ChaseMovement(-1f));
        }
    }
}

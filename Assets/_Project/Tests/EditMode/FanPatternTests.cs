using NUnit.Framework;
using PictoArcher.Core;
using UnityEngine;

namespace PictoArcher.Tests.EditMode
{
    public class FanPatternTests
    {
        static float AngleBetween(Vector2 a, Vector2 b) => Vector2.Angle(a, b);

        [Test] public void Single_PointsStraightAtCenter()
        {
            var direction = FanPattern.Direction(Vector2.right, 0, 1, 40f);
            Assert.AreEqual(1f, direction.x, 1e-5f);
            Assert.AreEqual(0f, direction.y, 1e-5f);
        }

        [Test] public void Three_MiddleIsCenter_OthersSpreadEvenly()
        {
            var center = Vector2.up;
            var left = FanPattern.Direction(center, 0, 3, 40f);
            var middle = FanPattern.Direction(center, 1, 3, 40f);
            var right = FanPattern.Direction(center, 2, 3, 40f);
            Assert.AreEqual(0f, AngleBetween(middle, center), 1e-3f);
            Assert.AreEqual(20f, AngleBetween(left, center), 1e-3f);
            Assert.AreEqual(20f, AngleBetween(right, center), 1e-3f);
            Assert.AreEqual(40f, AngleBetween(left, right), 1e-3f);
        }

        [Test] public void Directions_AreUnitLength_AndWorkForAnyCenter()
        {
            var direction = FanPattern.Direction(new Vector2(3f, -4f), 0, 3, 60f);
            Assert.AreEqual(1f, direction.magnitude, 1e-4f);
        }

        [Test] public void InvalidIndexOrCount_Throws()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() => FanPattern.Direction(Vector2.right, 0, 0, 40f));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => FanPattern.Direction(Vector2.right, 3, 3, 40f));
        }
    }
}

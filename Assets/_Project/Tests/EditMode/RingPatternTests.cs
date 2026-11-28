using NUnit.Framework;
using PictoArcher.Core;
using UnityEngine;

namespace PictoArcher.Tests.EditMode
{
    public class RingPatternTests
    {
        [Test]
        public void Direction_StartsAtAim_AndGoesAroundEvenly()
        {
            var aim = new Vector2(0f, -2f);
            Assert.AreEqual(0f, Vector2.Distance(Vector2.down, RingPattern.Direction(aim, 0, 4)), 1e-5f, "0 番目は狙う向き");
            Assert.AreEqual(0f, Vector2.Distance(Vector2.right, RingPattern.Direction(aim, 1, 4)), 1e-5f, "反時計回りに 90 度");
            Assert.AreEqual(0f, Vector2.Distance(Vector2.up, RingPattern.Direction(aim, 2, 4)), 1e-5f);
            Assert.AreEqual(0f, Vector2.Distance(Vector2.left, RingPattern.Direction(aim, 3, 4)), 1e-5f);
        }

        [Test]
        public void Direction_WithZeroAim_PointsDown()
            => Assert.AreEqual(0f, Vector2.Distance(Vector2.down, RingPattern.Direction(Vector2.zero, 0, 3)), 1e-5f);

        [Test]
        public void Direction_RejectsBadIndex()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() => RingPattern.Direction(Vector2.up, 0, 0));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => RingPattern.Direction(Vector2.up, 3, 3));
        }

        [Test]
        public void Point_IsOnTheCircle_AndClampedIntoTheArea()
        {
            var area = new Rect(-5f, -5f, 10f, 10f);
            var p = RingPattern.Point(Vector2.zero, Vector2.right, 0, 2, 3f, area);
            Assert.AreEqual(0f, Vector2.Distance(new Vector2(3f, 0f), p), 1e-5f);
            p = RingPattern.Point(new Vector2(4f, 0f), Vector2.right, 0, 2, 3f, area);
            Assert.AreEqual(0f, Vector2.Distance(new Vector2(5f, 0f), p), 1e-5f, "場の端（右の端も含む）に収める");
        }
    }
}

using NUnit.Framework;
using PictoArcher.Core;
using UnityEngine;

namespace PictoArcher.Tests.EditMode
{
    public class BeamHitTests
    {
        [Test]
        public void Hits_PointsOnTheBeam_WithinRadius()
        {
            Assert.IsTrue(BeamHit.Hits(Vector2.zero, Vector2.right, 10f, 0.5f, new Vector2(3f, 0f)), "線の上");
            Assert.IsTrue(BeamHit.Hits(Vector2.zero, Vector2.right, 10f, 0.5f, new Vector2(7f, 0.4f)), "線から半径の内");
            Assert.IsFalse(BeamHit.Hits(Vector2.zero, Vector2.right, 10f, 0.5f, new Vector2(7f, 0.6f)), "線から半径の外");
        }

        [Test]
        public void Hits_NotBehindTheOrigin_NorPastTheEnd()
        {
            Assert.IsFalse(BeamHit.Hits(Vector2.zero, Vector2.right, 10f, 0.5f, new Vector2(-1f, 0f)), "後ろ");
            Assert.IsFalse(BeamHit.Hits(Vector2.zero, Vector2.right, 10f, 0.5f, new Vector2(11f, 0f)), "先の外");
            Assert.IsTrue(BeamHit.Hits(Vector2.zero, Vector2.right, 10f, 0.5f, new Vector2(10.3f, 0f)), "端から半径の内");
        }

        [Test]
        public void Hits_UsesTheDirectionOnly_NotItsLength()
            => Assert.IsTrue(BeamHit.Hits(new Vector2(1f, 1f), new Vector2(0f, 5f), 4f, 0.25f, new Vector2(1f, 4.5f)));

        [Test]
        public void Hits_WithZeroDirection_OnlyAroundTheOrigin()
        {
            Assert.IsTrue(BeamHit.Hits(Vector2.zero, Vector2.zero, 10f, 0.5f, new Vector2(0.3f, 0f)));
            Assert.IsFalse(BeamHit.Hits(Vector2.zero, Vector2.zero, 10f, 0.5f, new Vector2(3f, 0f)));
        }
    }
}

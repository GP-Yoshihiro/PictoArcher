using System;
using NUnit.Framework;
using PictoArcher.Core;
using UnityEngine;

namespace PictoArcher.Tests.EditMode
{
    public class PickupAttractionTests
    {
        // 吸い寄せ 2、拾う 0.5、始め 4、加速 10、上限 6
        static readonly PickupTuning Tuning = new PickupTuning(2f, 0.5f, 4f, 10f, 6f);

        [Test]
        public void Far_DoesNotMove()
        {
            var p = new PickupAttraction(Vector2.zero, Tuning);
            Assert.IsFalse(p.Tick(0.1f, new Vector2(5f, 0f)));
            Assert.AreEqual(Vector2.zero, p.Position);
            Assert.IsFalse(p.IsAttracted);
        }

        [Test]
        public void InsideMagnetRadius_MovesTowardTarget_WithAcceleratedSpeed()
        {
            var p = new PickupAttraction(Vector2.zero, Tuning);
            Assert.IsFalse(p.Tick(0.1f, new Vector2(2f, 0f)));
            Assert.IsTrue(p.IsAttracted);
            // 速さ 4 + 10×0.1 = 5、0.1 秒で 0.5 進む
            Assert.AreEqual(0.5f, p.Position.x, 1e-4f);
            Assert.AreEqual(0f, p.Position.y, 1e-4f);
        }

        [Test]
        public void Speed_DoesNotExceedMax()
        {
            var p = new PickupAttraction(Vector2.zero, Tuning);
            p.Attract();
            p.Tick(1f, new Vector2(100f, 0f));
            Assert.AreEqual(6f, p.Position.x, 1e-4f);
        }

        [Test]
        public void Attract_MovesEvenWhenFar_AndKeepsGoingAfterTargetMovesAway()
        {
            var p = new PickupAttraction(Vector2.zero, Tuning);
            p.Attract();
            p.Tick(0.1f, new Vector2(10f, 0f));
            Assert.Greater(p.Position.x, 0f);
            p.Tick(0.1f, new Vector2(-10f, 0f));
            Assert.IsTrue(p.IsAttracted, "一度吸い寄せ始めたら離れても止まらない");
        }

        [Test]
        public void ReachingCollectRadius_Collects_AndStaysCollected()
        {
            var p = new PickupAttraction(Vector2.zero, Tuning);
            var target = new Vector2(1f, 0f);
            bool collected = false;
            for (int i = 0; i < 20 && !collected; i++) collected = p.Tick(0.05f, target);
            Assert.IsTrue(collected);
            Assert.IsTrue(p.IsCollected);
            Assert.IsTrue(p.Tick(0.05f, new Vector2(50f, 0f)));
        }

        [Test]
        public void DoesNotOvershootTheTarget()
        {
            var p = new PickupAttraction(Vector2.zero, new PickupTuning(2f, 0f, 100f, 0f, 100f));
            Assert.IsTrue(p.Tick(1f, new Vector2(1f, 1f)));
            Assert.AreEqual(new Vector2(1f, 1f), p.Position);
        }

        [Test]
        public void WithinCollectRadius_CollectsAtOnce()
        {
            var p = new PickupAttraction(Vector2.zero, Tuning);
            Assert.IsTrue(p.Tick(0f, new Vector2(0.3f, 0f)));
        }

        [Test]
        public void NegativeDeltaTime_Throws()
            => Assert.Throws<ArgumentOutOfRangeException>(() => new PickupAttraction(Vector2.zero, Tuning).Tick(-0.1f, Vector2.zero));

        [Test]
        public void Tuning_RejectsNegativeValuesAndMaxBelowStart()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new PickupTuning(-1f, 0.5f, 4f, 10f, 6f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new PickupTuning(2f, -0.5f, 4f, 10f, 6f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new PickupTuning(2f, 0.5f, -4f, 10f, 6f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new PickupTuning(2f, 0.5f, 4f, -10f, 6f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new PickupTuning(2f, 0.5f, 4f, 10f, 3f));
        }
    }
}

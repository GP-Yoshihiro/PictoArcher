using NUnit.Framework;
using PictoArcher.Core;

namespace PictoArcher.Tests.EditMode
{
    public class AttackCycleTests
    {
        [Test] public void StartsWaiting_WithNoProgress()
        {
            var cycle = new AttackCycle(2f, 0.8f);
            Assert.AreEqual(AttackPhase.Waiting, cycle.Phase);
            Assert.AreEqual(0f, cycle.Progress, 1e-5f);
        }

        [Test] public void AfterInterval_StartsTelegraphing_ThenProgressGrows()
        {
            var cycle = new AttackCycle(2f, 0.8f);
            Assert.IsFalse(cycle.Tick(2f));
            Assert.AreEqual(AttackPhase.Telegraphing, cycle.Phase);
            Assert.IsFalse(cycle.Tick(0.4f));
            Assert.AreEqual(0.5f, cycle.Progress, 1e-4f);
        }

        [Test] public void FiresOnce_WhenTelegraphEnds_ThenWaitsAgain()
        {
            var cycle = new AttackCycle(2f, 0.8f);
            cycle.Tick(2f);
            Assert.IsTrue(cycle.Tick(0.8f), "予兆が終わったフレームで撃つ");
            Assert.AreEqual(AttackPhase.Waiting, cycle.Phase);
            Assert.AreEqual(0f, cycle.Progress, 1e-5f);
            Assert.IsFalse(cycle.Tick(0.1f), "続けて撃たない");
        }

        [Test] public void NextCycle_StartsAfterAnotherInterval()
        {
            var cycle = new AttackCycle(2f, 0.8f);
            cycle.Tick(2f);
            cycle.Tick(0.8f);
            cycle.Tick(1.9f);
            Assert.AreEqual(AttackPhase.Waiting, cycle.Phase);
            cycle.Tick(0.1f);
            Assert.AreEqual(AttackPhase.Telegraphing, cycle.Phase);
        }

        [Test] public void LargeDelta_FiresAtMostOncePerTick()
        {
            var cycle = new AttackCycle(1f, 1f);
            Assert.IsTrue(cycle.Tick(10f));
            Assert.AreEqual(AttackPhase.Waiting, cycle.Phase);
        }

        [Test] public void ZeroInterval_TelegraphsAtOnce()
        {
            var cycle = new AttackCycle(0f, 0.5f);
            cycle.Tick(0.1f);
            Assert.AreEqual(AttackPhase.Telegraphing, cycle.Phase);
        }

        [Test] public void Reset_GoesBackToWaiting()
        {
            var cycle = new AttackCycle(1f, 1f);
            cycle.Tick(1.5f);
            cycle.Reset();
            Assert.AreEqual(AttackPhase.Waiting, cycle.Phase);
            Assert.AreEqual(0f, cycle.Progress, 1e-5f);
            cycle.Tick(0.9f);
            Assert.AreEqual(AttackPhase.Waiting, cycle.Phase);
        }

        [Test] public void Constructor_InvalidValues_Throw()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() => new AttackCycle(-1f, 1f));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => new AttackCycle(1f, 0f));
        }

        [Test] public void Tick_NegativeDelta_Throws()
        {
            var cycle = new AttackCycle(1f, 1f);
            Assert.Throws<System.ArgumentOutOfRangeException>(() => cycle.Tick(-0.1f));
        }
    }
}

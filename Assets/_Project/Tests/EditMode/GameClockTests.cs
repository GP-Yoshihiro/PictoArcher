using System;
using NUnit.Framework;
using PictoArcher.Game;

namespace PictoArcher.Tests.EditMode
{
    public class GameClockTests
    {
        [Test] public void NewClock_RunsAtScaleOne()
        {
            var clock = new GameClock();
            Assert.IsFalse(clock.Paused);
            Assert.AreEqual(1f, clock.Scale);
            Assert.AreEqual(1f, clock.EffectiveTimeScale);
        }

        [Test] public void Paused_MakesEffectiveScaleZero()
        {
            var clock = new GameClock { Scale = 2f, Paused = true };
            Assert.AreEqual(0f, clock.EffectiveTimeScale);
            clock.Paused = false;
            Assert.AreEqual(2f, clock.EffectiveTimeScale);
        }

        [Test] public void Advance_StoresUnscaledDeltaEvenWhenPaused()
        {
            var clock = new GameClock { Paused = true };
            clock.Advance(0.25f);
            Assert.AreEqual(0.25f, clock.UnscaledDeltaTime);
        }

        [Test] public void NegativeScale_Throws() =>
            Assert.Throws<ArgumentOutOfRangeException>(() => new GameClock().Scale = -1f);

        [Test] public void HitStop_StopsTimeUntilItsSecondsPassInUnscaledTime()
        {
            var clock = new GameClock { Scale = 2f };
            clock.RequestHitStop(0.1f);
            Assert.IsTrue(clock.IsHitStopped);
            Assert.AreEqual(0f, clock.EffectiveTimeScale);

            clock.Advance(0.06f);
            Assert.IsTrue(clock.IsHitStopped, "0.1 秒のうち 0.06 秒しかたっていません");

            clock.Advance(0.06f);
            Assert.IsFalse(clock.IsHitStopped);
            Assert.AreEqual(2f, clock.EffectiveTimeScale);
        }

        [Test] public void HitStop_ShorterRequestDoesNotCutLongerOne()
        {
            var clock = new GameClock();
            clock.RequestHitStop(0.2f);
            clock.RequestHitStop(0.05f);
            clock.Advance(0.1f);
            Assert.IsTrue(clock.IsHitStopped, "短い依頼で長いほうが縮まないはずです");
        }

        [Test] public void HitStop_LongerRequestExtends()
        {
            var clock = new GameClock();
            clock.RequestHitStop(0.05f);
            clock.RequestHitStop(0.2f);
            clock.Advance(0.1f);
            Assert.IsTrue(clock.IsHitStopped);
        }

        [Test] public void HitStop_DoesNotCountDownWhilePaused()
        {
            var clock = new GameClock();
            clock.RequestHitStop(0.1f);
            clock.Paused = true;
            clock.Advance(0.5f);
            clock.Paused = false;
            Assert.IsTrue(clock.IsHitStopped, "一時停止の間にヒットストップを使い切らないはずです");
        }

        [Test] public void HitStop_NegativeSecondsThrows() =>
            Assert.Throws<ArgumentOutOfRangeException>(() => new GameClock().RequestHitStop(-0.1f));
    }
}

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
    }
}

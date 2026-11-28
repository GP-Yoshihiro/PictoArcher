using System;
using NUnit.Framework;
using PictoArcher.Core;
using UnityEngine;

namespace PictoArcher.Tests.EditMode
{
    public class ObstacleSwapTests
    {
        static readonly Rect[] Two = { new Rect(0f, 0f, 2f, 2f), new Rect(5f, 0f, 2f, 2f) };
        static readonly Vector2 Far = new Vector2(20f, 20f);
        static readonly Vector2 OnA = new Vector2(1f, 1f);
        const float Radius = 0.6f;

        static ObstacleSwap NewSwap() => new ObstacleSwap(Two, 0.5f);

        [Test]
        public void NewSwap_AllSlotsAreShadow()
        {
            var swap = NewSwap();
            Assert.AreEqual(2, swap.Count);
            Assert.AreEqual(ObstacleSlotState.Shadow, swap.StateAt(0));
            Assert.AreEqual(ObstacleSlotState.Shadow, swap.StateAt(1));
            Assert.IsFalse(swap.IsFinished);
        }

        [Test]
        public void BeforeShadowSeconds_StaysShadow()
        {
            var swap = NewSwap();
            swap.Tick(0.49f, Far, Radius);
            Assert.AreEqual(ObstacleSlotState.Shadow, swap.StateAt(0));
            Assert.AreEqual(ObstacleSlotState.Shadow, swap.StateAt(1));
            Assert.AreEqual(0.98f, swap.ShadowProgress, 1e-4f);
        }

        [Test]
        public void AtShadowSeconds_ShowsSlotsAwayFromPlayer()
        {
            var swap = NewSwap();
            swap.Tick(0.25f, Far, Radius);
            swap.Tick(0.25f, Far, Radius);
            Assert.AreEqual(ObstacleSlotState.Shown, swap.StateAt(0));
            Assert.AreEqual(ObstacleSlotState.Shown, swap.StateAt(1));
            Assert.IsTrue(swap.IsFinished);
        }

        [Test]
        public void SlotUnderPlayer_IsBlockedUntilPlayerLeaves()
        {
            var swap = NewSwap();
            swap.Tick(0.5f, OnA, Radius);
            Assert.AreEqual(ObstacleSlotState.Blocked, swap.StateAt(0));
            Assert.AreEqual(ObstacleSlotState.Shown, swap.StateAt(1));
            Assert.IsFalse(swap.IsFinished);

            swap.Tick(0f, OnA, Radius);
            Assert.AreEqual(ObstacleSlotState.Blocked, swap.StateAt(0), "重なっている間は待つ");

            swap.Tick(0f, Far, Radius);
            Assert.AreEqual(ObstacleSlotState.Shown, swap.StateAt(0));
            Assert.IsTrue(swap.IsFinished);
        }

        [Test]
        public void ShownSlot_StaysShownWhenPlayerComesBack()
        {
            var swap = NewSwap();
            swap.Tick(0.5f, Far, Radius);
            swap.Tick(0f, OnA, Radius);
            Assert.AreEqual(ObstacleSlotState.Shown, swap.StateAt(0));
        }

        [Test]
        public void ZeroShadowSeconds_ShowsOnFirstTick()
        {
            var swap = new ObstacleSwap(Two, 0f);
            Assert.AreEqual(1f, swap.ShadowProgress);
            swap.Tick(0f, Far, Radius);
            Assert.AreEqual(ObstacleSlotState.Shown, swap.StateAt(0));
            Assert.AreEqual(ObstacleSlotState.Shown, swap.StateAt(1));
        }

        [Test]
        public void EmptyList_IsFinishedAtOnce() => Assert.IsTrue(new ObstacleSwap(Array.Empty<Rect>(), 0.5f).IsFinished);

        [Test]
        public void NegativeShadowSeconds_Throws() => Assert.Throws<ArgumentOutOfRangeException>(() => new ObstacleSwap(Two, -0.1f));

        [Test]
        public void NegativeDeltaTime_Throws() => Assert.Throws<ArgumentOutOfRangeException>(() => NewSwap().Tick(-0.1f, Far, Radius));
    }
}

using System;
using System.Collections.Generic;
using NUnit.Framework;
using PictoArcher.Core;

namespace PictoArcher.Tests.EditMode
{
    public class WaveSequenceTests
    {
        static (WaveSequence seq, List<int> started) Make(int start)
        {
            var seq = new WaveSequence(start, 1.5f);
            var started = new List<int>();
            seq.WaveStarted += started.Add;
            return (seq, started);
        }

        [Test] public void Begin_StartsFirstWave()
        {
            var (seq, started) = Make(1);
            seq.Begin();
            CollectionAssert.AreEqual(new[] { 1 }, started);
            Assert.AreEqual(WavePhase.Fighting, seq.Phase);
            Assert.AreEqual(1, seq.CurrentWave);
        }

        [Test] public void Clear_ShowsNextNumber_ThenStartsAfterRest()
        {
            var (seq, started) = Make(1);
            seq.Begin();
            seq.NotifyCleared();
            Assert.AreEqual(2, seq.CurrentWave);
            Assert.AreEqual(WavePhase.Resting, seq.Phase);
            seq.Tick(1.0f);
            CollectionAssert.AreEqual(new[] { 1 }, started);
            seq.Tick(0.5f);
            CollectionAssert.AreEqual(new[] { 1, 2 }, started);
            Assert.AreEqual(WavePhase.Fighting, seq.Phase);
        }

        [Test] public void DoubleClear_AdvancesOnce()
        {
            var (seq, started) = Make(1);
            seq.Begin();
            seq.NotifyCleared();
            seq.NotifyCleared();
            Assert.AreEqual(2, seq.CurrentWave);
            seq.Tick(1.5f);
            seq.Tick(1.5f);
            CollectionAssert.AreEqual(new[] { 1, 2 }, started);
        }

        [Test] public void LongFrameDuringRest_StartsOnlyOneWave()
        {
            var (seq, started) = Make(1);
            seq.Begin();
            seq.NotifyCleared();
            seq.Tick(100f);
            CollectionAssert.AreEqual(new[] { 1, 2 }, started);
            Assert.AreEqual(2, seq.CurrentWave);
        }

        [Test] public void LastWaveCleared_CompletesOnce()
        {
            var (seq, started) = Make(50);
            int completed = 0;
            seq.Completed += () => completed++;
            seq.Begin();
            seq.NotifyCleared();
            seq.NotifyCleared();
            seq.Tick(10f);
            Assert.AreEqual(1, completed);
            Assert.AreEqual(50, seq.CurrentWave);
            Assert.AreEqual(WavePhase.Completed, seq.Phase);
            CollectionAssert.AreEqual(new[] { 50 }, started);
        }

        [Test] public void StopThenClear_DoesNotAdvance()
        {
            var (seq, started) = Make(1);
            seq.Begin();
            seq.Stop();
            seq.NotifyCleared();
            seq.Tick(10f);
            Assert.AreEqual(1, seq.CurrentWave);
            Assert.AreEqual(WavePhase.Stopped, seq.Phase);
            CollectionAssert.AreEqual(new[] { 1 }, started);
        }

        [Test] public void ClearThenStop_NextWaveNeverStarts()
        {
            var (seq, started) = Make(1);
            seq.Begin();
            seq.NotifyCleared();
            seq.Stop();
            seq.Tick(10f);
            Assert.AreEqual(WavePhase.Stopped, seq.Phase);
            CollectionAssert.AreEqual(new[] { 1 }, started);
        }

        [Test] public void StartWaveOutOfRange_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new WaveSequence(-1, 1.5f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new WaveSequence(51, 1.5f));
        }
    }
}

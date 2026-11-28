using System;
using NUnit.Framework;
using PictoArcher.Core;

namespace PictoArcher.Tests.EditMode
{
    public class BestRecordTests
    {
        [Test] public void New_HasNotPlayed() { var r = new BestRecord(); Assert.IsFalse(r.HasPlayed); Assert.IsFalse(r.HasCleared); Assert.AreEqual(0, r.BestWave); }
        [Test] public void Submit_KeepsHighestWave() { var r = new BestRecord(); r.Submit(23, false, 0f); r.Submit(12, false, 0f); Assert.AreEqual(23, r.BestWave); Assert.IsTrue(r.HasPlayed); }
        [Test] public void Submit_Cleared_KeepsShortestTime() { var r = new BestRecord(); r.Submit(50, true, 1500f); r.Submit(50, true, 1600f); r.Submit(50, true, 1400f); Assert.IsTrue(r.HasCleared); Assert.AreEqual(1400f, r.BestClearSeconds); }
        [Test] public void Submit_DiedAfterClear_KeepsClearAndWave() { var r = new BestRecord(); r.Submit(50, true, 1500f); r.Submit(7, false, 0f); Assert.AreEqual(50, r.BestWave); Assert.IsTrue(r.HasCleared); Assert.AreEqual(1500f, r.BestClearSeconds); }
        [Test] public void Submit_NotCleared_IgnoresTime() { var r = new BestRecord(); r.Submit(30, false, 5f); Assert.IsFalse(r.HasCleared); Assert.AreEqual(0f, r.BestClearSeconds); }
        [Test] public void Submit_NegativeWave_Throws() => Assert.Throws<ArgumentOutOfRangeException>(() => new BestRecord().Submit(-1, false, 0f));
        [Test] public void Submit_NegativeTime_Throws() => Assert.Throws<ArgumentOutOfRangeException>(() => new BestRecord().Submit(50, true, -1f));
        [Test] public void Constructor_RestoresValues() { var r = new BestRecord(50, true, 1234f); Assert.AreEqual(50, r.BestWave); Assert.IsTrue(r.HasCleared); Assert.AreEqual(1234f, r.BestClearSeconds); }
    }
}

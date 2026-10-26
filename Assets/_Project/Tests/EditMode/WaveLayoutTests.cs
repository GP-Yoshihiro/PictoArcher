using System;
using System.Linq;
using NUnit.Framework;
using PictoArcher.Core;

namespace PictoArcher.Tests.EditMode
{
    public class WaveLayoutTests
    {
        [Test] public void Layout_MatchesSpec()
        {
            Assert.AreEqual(50, WaveLayout.TotalWaves);
            Assert.AreEqual(WaveKind.Tutorial, WaveLayout.GetKind(0));
            foreach (var w in new[] { 5, 15, 25, 35, 45 }) Assert.AreEqual(WaveKind.MiniBoss, WaveLayout.GetKind(w), $"wave {w}");
            foreach (var w in new[] { 10, 20, 30, 40 }) Assert.AreEqual(WaveKind.Boss, WaveLayout.GetKind(w), $"wave {w}");
            foreach (var w in new[] { 11, 21, 31, 41 }) Assert.AreEqual(WaveKind.Angel, WaveLayout.GetKind(w), $"wave {w}");
            Assert.AreEqual(WaveKind.FinalBoss, WaveLayout.GetKind(50));
            Assert.AreEqual(36, Enumerable.Range(1, 50).Count(w => WaveLayout.GetKind(w) == WaveKind.Combat));
        }

        [Test] public void OutOfRange_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => WaveLayout.GetKind(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => WaveLayout.GetKind(51));
        }
    }
}

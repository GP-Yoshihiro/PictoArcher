using System;
using NUnit.Framework;
using PictoArcher.Core;

namespace PictoArcher.Tests.EditMode
{
    public class ExperienceLevelsTests
    {
        [Test]
        public void StartsAtLevel1_WithFirstRequirement()
        {
            var levels = new ExperienceLevels(3, 4);
            Assert.AreEqual(1, levels.Level);
            Assert.AreEqual(0, levels.Experience);
            Assert.AreEqual(3, levels.ToNextLevel);
        }

        [Test]
        public void Add_BelowRequirement_GainsNoLevel()
        {
            var levels = new ExperienceLevels(3, 4);
            Assert.AreEqual(0, levels.Add(2));
            Assert.AreEqual(1, levels.Level);
            Assert.AreEqual(2, levels.Experience);
        }

        [Test]
        public void Add_ReachingRequirement_LevelsUpAndCarriesTheRest()
        {
            var levels = new ExperienceLevels(3, 4);
            Assert.AreEqual(1, levels.Add(5));
            Assert.AreEqual(2, levels.Level);
            Assert.AreEqual(2, levels.Experience);
            Assert.AreEqual(7, levels.ToNextLevel, "2 から 3 へは 3 + 4");
        }

        [Test]
        public void Add_ALot_GainsSeveralLevelsAtOnce()
        {
            var levels = new ExperienceLevels(3, 4);
            // 3 + 7 + 11 = 21 で ★4、残り 1
            Assert.AreEqual(3, levels.Add(22));
            Assert.AreEqual(4, levels.Level);
            Assert.AreEqual(1, levels.Experience);
        }

        [Test]
        public void ZeroGrowth_KeepsTheSameRequirement()
        {
            var levels = new ExperienceLevels(5, 0);
            levels.Add(5);
            Assert.AreEqual(5, levels.ToNextLevel);
        }

        [Test]
        public void InvalidValues_Throw()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new ExperienceLevels(0, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ExperienceLevels(1, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ExperienceLevels(1, 1).Add(-1));
        }
    }
}

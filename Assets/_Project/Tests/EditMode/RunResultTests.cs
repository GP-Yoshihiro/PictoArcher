using System;
using System.Collections.Generic;
using NUnit.Framework;
using PictoArcher.Game;

namespace PictoArcher.Tests.EditMode
{
    public class RunResultTests
    {
        [Test] public void Skills_AreDistinctInOrder()
        {
            var r = new RunResult(RunOutcome.Died, 23, 0f, 10, WeaponId.Laser, new[] { SkillId.Fire, SkillId.AttackUp, SkillId.Fire });
            CollectionAssert.AreEqual(new[] { SkillId.Fire, SkillId.AttackUp }, r.Skills);
        }

        [Test] public void Skills_AreCopied()
        {
            var list = new List<SkillId> { SkillId.Ice };
            var r = new RunResult(RunOutcome.Died, 1, 0f, 0, WeaponId.Gun, list);
            list.Add(SkillId.Fire);
            Assert.AreEqual(1, r.Skills.Count);
        }

        [Test] public void NullSkills_AreEmpty() => Assert.AreEqual(0, new RunResult(RunOutcome.Cleared, 50, 1f, 0, WeaponId.Gun, null).Skills.Count);
        [Test] public void ReachedWaveOutOfRange_Throws() => Assert.Throws<ArgumentOutOfRangeException>(() => new RunResult(RunOutcome.Died, 51, 0f, 0, WeaponId.Gun, null));
        [Test] public void NegativeTime_Throws() => Assert.Throws<ArgumentOutOfRangeException>(() => new RunResult(RunOutcome.Died, 3, -1f, 0, WeaponId.Gun, null));
        [Test] public void IsCleared_FollowsOutcome() => Assert.IsTrue(new RunResult(RunOutcome.Cleared, 50, 1f, 0, WeaponId.Gun, null).IsCleared);
    }
}

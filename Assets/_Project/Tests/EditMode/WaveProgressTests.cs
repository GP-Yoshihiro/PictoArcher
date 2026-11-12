using NUnit.Framework;
using PictoArcher.Core;

namespace PictoArcher.Tests.EditMode
{
    public class WaveProgressTests
    {
        [Test] public void NotClearedUntilSpawningFinished()
        {
            var p = new WaveProgress();
            p.RegisterEnemy(1);
            p.MarkKilled(1);
            Assert.IsFalse(p.IsCleared);
            p.FinishSpawning();
            Assert.IsTrue(p.IsCleared);
        }

        [Test] public void EmptyWave_ClearsWhenSpawningFinished()
        {
            var p = new WaveProgress();
            p.FinishSpawning();
            Assert.IsTrue(p.IsCleared);
        }

        [Test] public void DuplicateAndUnknownKills_AreIgnored()
        {
            var p = new WaveProgress();
            p.RegisterEnemy(1); p.RegisterEnemy(2); p.FinishSpawning();
            p.MarkKilled(1); p.MarkKilled(1); p.MarkKilled(99);
            Assert.AreEqual(1, p.AliveCount);
            Assert.IsFalse(p.IsCleared);
        }

        [Test] public void Cleared_FiresOnce()
        {
            var p = new WaveProgress();
            int count = 0;
            p.Cleared += () => count++;
            p.RegisterEnemy(1); p.FinishSpawning();
            p.MarkKilled(1); p.MarkKilled(1);
            Assert.AreEqual(1, count);
        }
    }
}

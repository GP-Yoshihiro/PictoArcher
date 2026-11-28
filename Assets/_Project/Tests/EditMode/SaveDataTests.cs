using NUnit.Framework;
using PictoArcher.Core;

namespace PictoArcher.Tests.EditMode
{
    public class SaveDataTests
    {
        [Test] public void Empty_HasNoRecordNoCoins_AndNoWeapon()
        {
            var data = SaveData.Empty();
            Assert.AreEqual(0, data.BestWave);
            Assert.IsFalse(data.HasCleared);
            Assert.AreEqual(0f, data.BestClearSeconds);
            Assert.AreEqual(0, data.Coins);
            Assert.AreEqual(string.Empty, data.Weapon);
        }

        [Test] public void Ctor_KeepsValidValues()
        {
            var data = new SaveData(50, true, 812.5f, 340, "Laser");
            Assert.AreEqual(50, data.BestWave);
            Assert.IsTrue(data.HasCleared);
            Assert.AreEqual(812.5f, data.BestClearSeconds);
            Assert.AreEqual(340, data.Coins);
            Assert.AreEqual("Laser", data.Weapon);
        }

        [TestCase(-3, 0)]
        [TestCase(51, 50)]
        [TestCase(9999, 50)]
        public void Ctor_ClampsWaveIntoRange(int saved, int expected)
            => Assert.AreEqual(expected, new SaveData(saved, false, 0f, 0, "Gun").BestWave);

        [TestCase(-1f)]
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        public void Ctor_BrokenClearTime_DropsTheClear(float seconds)
        {
            var data = new SaveData(50, true, seconds, 0, "Gun");
            Assert.IsFalse(data.HasCleared);
            Assert.AreEqual(0f, data.BestClearSeconds);
        }

        [Test] public void Ctor_Cleared_MeansTheLastWaveWasReached()
            => Assert.AreEqual(50, new SaveData(7, true, 600f, 0, "Gun").BestWave);

        [Test] public void Ctor_NotCleared_ZeroesTheTime()
            => Assert.AreEqual(0f, new SaveData(12, false, 300f, 0, "Gun").BestClearSeconds);

        [Test] public void Ctor_NegativeCoins_BecomeZero()
            => Assert.AreEqual(0, new SaveData(0, false, 0f, -20, "Gun").Coins);

        [Test] public void Ctor_NullWeapon_BecomesEmpty()
            => Assert.AreEqual(string.Empty, new SaveData(0, false, 0f, 0, null).Weapon);

        [Test] public void ToRecord_And_From_RoundTrip()
        {
            var record = new BestRecord();
            record.Submit(50, true, 640f);
            var data = SaveData.From(record, 75, "Launcher");
            var back = data.ToRecord();
            Assert.AreEqual(50, back.BestWave);
            Assert.IsTrue(back.HasCleared);
            Assert.AreEqual(640f, back.BestClearSeconds);
            Assert.AreEqual(75, data.Coins);
            Assert.AreEqual("Launcher", data.Weapon);
        }

        [Test] public void From_NullRecord_Throws()
            => Assert.Throws<System.ArgumentNullException>(() => SaveData.From(null, 0, "Gun"));
    }
}

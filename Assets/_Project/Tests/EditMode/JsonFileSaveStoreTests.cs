using System.IO;
using NUnit.Framework;
using PictoArcher.Core;
using PictoArcher.Game;
using UnityEngine;
using UnityEngine.TestTools;

namespace PictoArcher.Tests.EditMode
{
    public class JsonFileSaveStoreTests
    {
        string _folder;
        string _path;

        [SetUp]
        public void SetUp()
        {
            _folder = Path.Combine(Path.GetTempPath(), "PictoArcherSaveTests_" + System.Guid.NewGuid().ToString("N"));
            _path = Path.Combine(_folder, "save.json");
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_folder)) Directory.Delete(_folder, true);
        }

        [Test] public void Load_WithoutFile_IsEmpty()
        {
            var data = new JsonFileSaveStore(_path).Load();
            Assert.AreEqual(0, data.BestWave);
            Assert.AreEqual(string.Empty, data.Weapon);
            Assert.IsFalse(File.Exists(_path), "読むだけではファイルを作らない");
        }

        [Test] public void Save_ThenLoad_InANewStore_RoundTrips()
        {
            new JsonFileSaveStore(_path).Save(new SaveData(50, true, 777.25f, 1234, "Launcher"));
            var data = new JsonFileSaveStore(_path).Load();
            Assert.AreEqual(50, data.BestWave);
            Assert.IsTrue(data.HasCleared);
            Assert.AreEqual(777.25f, data.BestClearSeconds);
            Assert.AreEqual(1234, data.Coins);
            Assert.AreEqual("Launcher", data.Weapon);
        }

        [Test] public void Save_Twice_OverwritesAndLeavesNoTempFile()
        {
            var store = new JsonFileSaveStore(_path);
            store.Save(new SaveData(3, false, 0f, 1, "Gun"));
            store.Save(new SaveData(8, false, 0f, 2, "Laser"));
            var data = store.Load();
            Assert.AreEqual(8, data.BestWave);
            Assert.AreEqual("Laser", data.Weapon);
            Assert.IsFalse(File.Exists(_path + ".tmp"));
        }

        [Test] public void Save_WritesTheVersion()
        {
            new JsonFileSaveStore(_path).Save(SaveData.Empty());
            var file = JsonUtility.FromJson<SaveFile>(File.ReadAllText(_path));
            Assert.AreEqual(SaveFile.CurrentVersion, file.Version);
        }

        [TestCase("{ this is not json")]
        [TestCase("")]
        public void Load_BrokenFile_IsEmpty_AndKeepsACopy(string broken)
        {
            Directory.CreateDirectory(_folder);
            File.WriteAllText(_path, broken);
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("壊れていた"));
            var data = new JsonFileSaveStore(_path).Load();
            Assert.AreEqual(0, data.BestWave);
            Assert.AreEqual(broken, File.ReadAllText(_path + ".broken"));
        }

        [Test] public void Load_OutOfRangeValues_AreFixed()
        {
            Directory.CreateDirectory(_folder);
            File.WriteAllText(_path, "{\"_version\":1,\"_bestWave\":999,\"_hasCleared\":true,\"_bestClearSeconds\":-5,\"_coins\":-3,\"_weapon\":\"Laser\"}");
            var data = new JsonFileSaveStore(_path).Load();
            Assert.AreEqual(WaveLayout.TotalWaves, data.BestWave);
            Assert.IsFalse(data.HasCleared);
            Assert.AreEqual(0, data.Coins);
            Assert.AreEqual("Laser", data.Weapon);
        }

        [Test] public void Ctor_EmptyPath_Throws()
            => Assert.Throws<System.ArgumentException>(() => new JsonFileSaveStore(""));
    }
}

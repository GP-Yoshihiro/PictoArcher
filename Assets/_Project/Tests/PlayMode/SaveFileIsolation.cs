using System.IO;
using NUnit.Framework;
using PictoArcher.Game;

namespace PictoArcher.Tests.PlayMode
{
    /// <summary>
    /// PlayMode のテストは本物の Main を動かすので、本物の保存先（persistentDataPath の save.json）を使う。
    /// 全テストの前に今の保存を退避し、終わったら戻す。Root を読み込むテストは [SetUp] で Clear を呼び、前のテストの記録や武器を持ち越さない。
    /// </summary>
    [SetUpFixture]
    public sealed class SaveFileIsolation
    {
        static string SavePath => JsonFileSaveStore.DefaultPath();
        static string BackupPath => SavePath + ".testbackup";

        [OneTimeSetUp]
        public void BackUp()
        {
            // 前の実行が途中で止まって退避が残っているなら、それが本物なので上書きしない
            if (File.Exists(BackupPath)) Clear();
            else if (File.Exists(SavePath)) File.Move(SavePath, BackupPath);
        }

        [OneTimeTearDown]
        public void Restore()
        {
            Clear();
            if (File.Exists(BackupPath)) File.Move(BackupPath, SavePath);
        }

        /// <summary>テストが作った保存を消す（初めての起動と同じ状態にする）。</summary>
        public static void Clear()
        {
            Delete(SavePath);
            Delete(SavePath + ".tmp");
            Delete(SavePath + ".broken");
        }

        static void Delete(string path)
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }
}

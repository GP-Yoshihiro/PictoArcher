using System;
using System.IO;
using PictoArcher.Core;
using UnityEngine;

namespace PictoArcher.Game
{
    /// <summary>
    /// 保存を JSON のファイル1つに読み書きする。ファイルがなければ何もない状態から始める。
    /// 壊れていたら .broken に写して残し、何もない状態から始める（遊べなくしない）。
    /// 書くときは .tmp に書いてから入れ替え、書いている途中で止まっても前のファイルを壊さない。読み書きに失敗したら警告をコンソールに出すだけ。
    /// </summary>
    public sealed class JsonFileSaveStore : ISaveStore
    {
        public const string FileName = "save.json";

        readonly string _path;

        public JsonFileSaveStore(string path)
        {
            if (string.IsNullOrEmpty(path)) throw new ArgumentException("保存先がありません", nameof(path));
            _path = path;
        }

        /// <summary>本番の保存先（Application.persistentDataPath の save.json）。</summary>
        public static string DefaultPath() => Path.Combine(Application.persistentDataPath, FileName);

        public SaveData Load()
        {
            string json;
            try
            {
                if (!File.Exists(_path)) return SaveData.Empty();
                json = File.ReadAllText(_path);
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
                Debug.LogWarning($"保存を読めませんでした（{_path}）：{e.Message}");
                return SaveData.Empty();
            }

            SaveFile file = null;
            try
            {
                file = JsonUtility.FromJson<SaveFile>(json);
            }
            catch (ArgumentException)
            {
                // 下で壊れたファイルとして扱う
            }
            if (file == null)
            {
                KeepBroken();
                return SaveData.Empty();
            }
            return file.ToData();
        }

        public void Save(SaveData data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            var temp = _path + ".tmp";
            try
            {
                var folder = Path.GetDirectoryName(_path);
                if (!string.IsNullOrEmpty(folder)) Directory.CreateDirectory(folder);
                File.WriteAllText(temp, JsonUtility.ToJson(SaveFile.From(data)));
                if (!File.Exists(_path)) File.Move(temp, _path);
                else Replace(temp);
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
                Debug.LogWarning($"保存できませんでした（{_path}）：{e.Message}");
            }
        }

        void Replace(string temp)
        {
            try
            {
                File.Replace(temp, _path, null);
            }
            catch (PlatformNotSupportedException)
            {
                // 入れ替えができない環境では上書きで写す
                File.Copy(temp, _path, true);
                File.Delete(temp);
            }
        }

        void KeepBroken()
        {
            var broken = _path + ".broken";
            Debug.LogWarning($"保存が壊れていたので、初めから始めます（元のファイルは {broken}）。");
            try
            {
                File.Copy(_path, broken, true);
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
                Debug.LogWarning($"壊れた保存を残せませんでした：{e.Message}");
            }
        }
    }
}

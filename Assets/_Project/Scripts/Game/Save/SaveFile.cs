using System;
using PictoArcher.Core;
using UnityEngine;

namespace PictoArcher.Game
{
    /// <summary>保存ファイルの JSON の形（JsonUtility で読み書きする）。中身の確かめと直しは SaveData がする。</summary>
    [Serializable]
    public sealed class SaveFile
    {
        /// <summary>ファイルの形の版。形を変えたら上げ、読み込みで古い版を読み替える。</summary>
        public const int CurrentVersion = 1;

        [SerializeField] int _version = CurrentVersion;
        [SerializeField] int _bestWave;
        [SerializeField] bool _hasCleared;
        [SerializeField] float _bestClearSeconds;
        [SerializeField] int _coins;
        [SerializeField] string _weapon = string.Empty;

        public int Version => _version;

        public static SaveFile From(SaveData data) => new SaveFile
        {
            _bestWave = data.BestWave,
            _hasCleared = data.HasCleared,
            _bestClearSeconds = data.BestClearSeconds,
            _coins = data.Coins,
            _weapon = data.Weapon,
        };

        public SaveData ToData() => new SaveData(_bestWave, _hasCleared, _bestClearSeconds, _coins, _weapon);
    }
}

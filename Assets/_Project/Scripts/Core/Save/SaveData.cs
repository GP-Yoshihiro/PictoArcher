using System;

namespace PictoArcher.Core
{
    /// <summary>
    /// 保存する中身（最高記録、コインの合計、前回の武器）。作ったあとは変わらない。
    /// 読み込んだファイルが壊れていても続けて遊べるように、作るときにおかしな値を直す（範囲の外、負、NaN、null）。
    /// 武器は Core が武器の種類を知らないので名前の文字列で持つ（読み戻しは Game）。
    /// </summary>
    public sealed class SaveData
    {
        public SaveData(int bestWave, bool hasCleared, float bestClearSeconds, int coins, string weapon)
        {
            bool validTime = !float.IsNaN(bestClearSeconds) && !float.IsInfinity(bestClearSeconds) && bestClearSeconds >= 0f;
            HasCleared = hasCleared && validTime;
            // クリアしたなら最後のウェーブまで進んでいる
            BestWave = HasCleared ? WaveLayout.TotalWaves : Math.Min(Math.Max(bestWave, 0), WaveLayout.TotalWaves);
            BestClearSeconds = HasCleared ? bestClearSeconds : 0f;
            Coins = Math.Max(coins, 0);
            Weapon = weapon ?? string.Empty;
        }

        /// <summary>何も保存していないとき（初めての起動、ファイルが壊れていたとき）。</summary>
        public static SaveData Empty() => new SaveData(0, false, 0f, 0, string.Empty);

        public static SaveData From(BestRecord record, int coins, string weapon)
        {
            if (record == null) throw new ArgumentNullException(nameof(record));
            return new SaveData(record.BestWave, record.HasCleared, record.BestClearSeconds, coins, weapon);
        }

        public int BestWave { get; }
        public bool HasCleared { get; }
        /// <summary>クリアの最短タイム（秒）。HasCleared のときだけ意味がある。</summary>
        public float BestClearSeconds { get; }
        /// <summary>これまでに拾ったコインの合計。</summary>
        public int Coins { get; }
        /// <summary>前回選んだ武器の名前。空なら選んでいない。</summary>
        public string Weapon { get; }

        public BestRecord ToRecord() => new BestRecord(BestWave, HasCleared, BestClearSeconds);
    }
}

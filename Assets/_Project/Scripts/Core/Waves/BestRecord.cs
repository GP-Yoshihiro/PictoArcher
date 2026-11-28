using System;

namespace PictoArcher.Core
{
    /// <summary>最高記録（到達ウェーブとクリアの最短タイム）。良いほうだけを残す。保存は C-14 まではメモリだけ。</summary>
    public sealed class BestRecord
    {
        public BestRecord() { }

        /// <summary>保存した値から作り直す（C-14 の読み込み用）。</summary>
        public BestRecord(int bestWave, bool hasCleared, float bestClearSeconds)
        {
            if (bestWave < 0) throw new ArgumentOutOfRangeException(nameof(bestWave));
            if (bestClearSeconds < 0f) throw new ArgumentOutOfRangeException(nameof(bestClearSeconds));
            BestWave = bestWave;
            HasCleared = hasCleared;
            BestClearSeconds = hasCleared ? bestClearSeconds : 0f;
        }

        /// <summary>一番先まで進んだウェーブ。0 は記録なし。</summary>
        public int BestWave { get; private set; }
        public bool HasPlayed => BestWave > 0;
        public bool HasCleared { get; private set; }
        /// <summary>クリアの最短タイム（秒）。HasCleared のときだけ意味がある。</summary>
        public float BestClearSeconds { get; private set; }

        public void Submit(int reachedWave, bool cleared, float clearSeconds)
        {
            if (reachedWave < 0) throw new ArgumentOutOfRangeException(nameof(reachedWave));
            if (clearSeconds < 0f) throw new ArgumentOutOfRangeException(nameof(clearSeconds));
            if (reachedWave > BestWave) BestWave = reachedWave;
            if (!cleared) return;
            if (!HasCleared || clearSeconds < BestClearSeconds) BestClearSeconds = clearSeconds;
            HasCleared = true;
        }
    }
}

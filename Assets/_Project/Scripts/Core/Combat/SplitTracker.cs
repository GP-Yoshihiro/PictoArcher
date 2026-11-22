using System;
using System.Collections.Generic;

namespace PictoArcher.Core
{
    /// <summary>
    /// 「HP がこの割合を切ったら分裂する」のしきい値を数える。各しきい値は1回だけ数え、死んだとき（HP 0 以下）は数えない。
    /// </summary>
    public sealed class SplitTracker
    {
        readonly float[] _thresholds;
        readonly bool[] _crossed;

        /// <param name="thresholds">HP の割合（0 より大きく 1 以下）。順番は問わない。</param>
        public SplitTracker(IReadOnlyList<float> thresholds)
        {
            _thresholds = new float[thresholds.Count];
            for (int i = 0; i < thresholds.Count; i++)
            {
                if (thresholds[i] <= 0f || thresholds[i] > 1f) throw new ArgumentOutOfRangeException(nameof(thresholds), "Threshold must be in (0, 1].");
                _thresholds[i] = thresholds[i];
            }
            _crossed = new bool[_thresholds.Length];
        }

        /// <summary>いまの HP で新しく切ったしきい値の数（分裂する回数）を返す。</summary>
        public int Update(int hp, int maxHp)
        {
            if (maxHp <= 0) throw new ArgumentOutOfRangeException(nameof(maxHp), "Max HP must be positive.");
            if (hp <= 0) return 0;
            float ratio = (float)hp / maxHp;
            int count = 0;
            for (int i = 0; i < _thresholds.Length; i++)
            {
                if (_crossed[i] || ratio > _thresholds[i]) continue;
                _crossed[i] = true;
                count++;
            }
            return count;
        }
    }
}

using System;
using System.Collections.Generic;
using PictoArcher.Core;

namespace PictoArcher.Game
{
    /// <summary>1回のプレイの結果。Battle が作り、GameContext.FinishRun で Result へ渡す。作ったら変わらない。</summary>
    public sealed class RunResult
    {
        readonly List<SkillId> _skills = new List<SkillId>();

        public RunResult(RunOutcome outcome, int reachedWave, float clearSeconds, int coins, WeaponId weapon, IReadOnlyList<SkillId> skills)
        {
            if (reachedWave < 0 || reachedWave > WaveLayout.TotalWaves) throw new ArgumentOutOfRangeException(nameof(reachedWave));
            if (clearSeconds < 0f || float.IsNaN(clearSeconds) || float.IsInfinity(clearSeconds)) throw new ArgumentOutOfRangeException(nameof(clearSeconds));
            Outcome = outcome;
            ReachedWave = reachedWave;
            ClearSeconds = clearSeconds;
            Coins = coins;
            Weapon = weapon;
            if (skills == null) return;
            // 同じ強化を重ねて取っても1つだけ、取った順に
            for (int i = 0; i < skills.Count; i++)
            {
                if (!_skills.Contains(skills[i])) _skills.Add(skills[i]);
            }
        }

        public RunOutcome Outcome { get; }
        public bool IsCleared => Outcome == RunOutcome.Cleared;
        public int ReachedWave { get; }
        /// <summary>プレイの時間（秒。一時停止の間は数えない）。</summary>
        public float ClearSeconds { get; }
        public int Coins { get; }
        public WeaponId Weapon { get; }
        /// <summary>取った強化（重なりを除いて取った順）。</summary>
        public IReadOnlyList<SkillId> Skills => _skills;
    }
}

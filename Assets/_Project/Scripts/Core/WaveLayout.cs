using System;

namespace PictoArcher.Core
{
    public enum WaveKind { Tutorial, Combat, MiniBoss, Boss, FinalBoss, Angel }

    public static class WaveLayout
    {
        public const int TotalWaves = 50;

        public static WaveKind GetKind(int wave)
        {
            if (wave < 0 || wave > TotalWaves) throw new ArgumentOutOfRangeException(nameof(wave));
            if (wave == 0) return WaveKind.Tutorial;
            if (wave == TotalWaves) return WaveKind.FinalBoss;
            if (wave % 10 == 5) return WaveKind.MiniBoss;
            if (wave % 10 == 0) return WaveKind.Boss;
            if (wave % 10 == 1 && wave >= 11) return WaveKind.Angel;
            return WaveKind.Combat;
        }
    }
}

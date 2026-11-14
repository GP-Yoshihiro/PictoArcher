using System;

namespace PictoArcher.Game
{
    /// <summary>
    /// ゲーム全体の時計。一時停止と倍率を持つ。Time.timeScale に反映するのは Main だけ。
    /// </summary>
    public sealed class GameClock
    {
        float _scale = 1f;

        public bool Paused { get; set; }

        public float Scale
        {
            get => _scale;
            set
            {
                if (value < 0f) throw new ArgumentOutOfRangeException(nameof(value), "Scale must not be negative.");
                _scale = value;
            }
        }

        /// <summary>スケールなしの経過時間（一時停止中にも動く UI 用）。</summary>
        public float UnscaledDeltaTime { get; private set; }

        public float EffectiveTimeScale => Paused ? 0f : _scale;

        /// <summary>Main だけが毎フレーム呼ぶ。</summary>
        public void Advance(float unscaledDeltaTime) => UnscaledDeltaTime = unscaledDeltaTime;
    }
}

using System;

namespace PictoArcher.Game
{
    /// <summary>
    /// ゲーム全体の時計。一時停止、倍率、ヒットストップを持つ。Time.timeScale に反映するのは Main だけ。
    /// System は GameContext.Clock を読むだけにし、Tick の deltaTime（止まっている間は 0）で動く。
    /// </summary>
    public sealed class GameClock
    {
        float _scale = 1f;
        float _hitStopRemaining;

        /// <summary>デバッグや演出で時間を止める。重ねシーン（Pause など）の間は SceneTransform が別に止めるので、ここは触らない。</summary>
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

        /// <summary>ヒットストップで止まっている間は true。</summary>
        public bool IsHitStopped => _hitStopRemaining > 0f;

        public float EffectiveTimeScale => Paused || IsHitStopped ? 0f : _scale;

        /// <summary>
        /// seconds 秒（スケールなしの時間）だけ時間を止める。止まっている間にもう一度呼ばれたら、残りの長いほうにする（短い依頼で縮めない）。
        /// 呼んだフレームの次のフレームから止まる。
        /// </summary>
        public void RequestHitStop(float seconds)
        {
            if (seconds < 0f) throw new ArgumentOutOfRangeException(nameof(seconds), "Hit stop seconds must not be negative.");
            if (seconds > _hitStopRemaining) _hitStopRemaining = seconds;
        }

        /// <summary>Main だけが毎フレーム呼ぶ。ヒットストップの残りは一時停止の間は減らさない。</summary>
        public void Advance(float unscaledDeltaTime)
        {
            UnscaledDeltaTime = unscaledDeltaTime;
            if (!Paused && _hitStopRemaining > 0f) _hitStopRemaining = Math.Max(0f, _hitStopRemaining - unscaledDeltaTime);
        }
    }
}

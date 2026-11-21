using System;

namespace PictoArcher.Core
{
    /// <summary>出現予兆などの「予告の時間」を数える。Progress は 0〜1、IsVisible は点滅の表と裏。</summary>
    public sealed class TelegraphTimer
    {
        readonly float _duration;
        readonly float _blinksPerSecond;
        float _elapsed;

        public TelegraphTimer(float duration, float blinksPerSecond)
        {
            if (duration <= 0f) throw new ArgumentOutOfRangeException(nameof(duration), "Duration must be positive.");
            if (blinksPerSecond < 0f) throw new ArgumentOutOfRangeException(nameof(blinksPerSecond), "Blinks per second must not be negative.");
            _duration = duration;
            _blinksPerSecond = blinksPerSecond;
        }

        public float Progress => Math.Min(_elapsed / _duration, 1f);
        public bool IsFinished => _elapsed >= _duration;

        /// <summary>点滅の表（見える側）なら true。1回の点滅は「見える→消える」の2つぶん。</summary>
        public bool IsVisible => _blinksPerSecond <= 0f || ((int)(_elapsed * _blinksPerSecond * 2f) & 1) == 0;

        public void Tick(float deltaTime)
        {
            if (deltaTime < 0f) throw new ArgumentOutOfRangeException(nameof(deltaTime), "Delta time must not be negative.");
            _elapsed += deltaTime;
        }
    }
}

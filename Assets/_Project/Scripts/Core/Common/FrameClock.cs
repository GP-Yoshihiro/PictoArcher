using System;

namespace PictoArcher.Core
{
    /// <summary>
    /// コマ送りのアニメの時計。今が何コマ目かだけを数える（絵は持たない）。
    /// ループするものは頭に戻り、ループしないものは最後のコマで止まって IsFinished になる。
    /// fps が 0 以下なら進めない（ループしないものは、待つ側が止まらないようすぐ終わりにする）。
    /// </summary>
    public sealed class FrameClock
    {
        int _frameCount;
        float _framesPerSecond;
        bool _loops;
        float _elapsed;

        public int FrameIndex { get; private set; }
        public bool IsFinished { get; private set; }

        public void Play(int frameCount, float framesPerSecond, bool loops)
        {
            _frameCount = Math.Max(frameCount, 0);
            _framesPerSecond = framesPerSecond;
            _loops = loops;
            _elapsed = 0f;
            FrameIndex = 0;
            IsFinished = !loops && framesPerSecond <= 0f;
        }

        public void Tick(float deltaTime)
        {
            if (_framesPerSecond <= 0f || IsFinished) return;

            float duration = Math.Max(_frameCount, 1) / _framesPerSecond;
            _elapsed += deltaTime;
            if (_loops)
            {
                _elapsed %= duration;
            }
            else if (_elapsed >= duration)
            {
                _elapsed = duration;
                IsFinished = true;
            }

            int lastFrame = Math.Max(_frameCount - 1, 0);
            FrameIndex = Math.Min((int)(_elapsed * _framesPerSecond), lastFrame);
        }
    }
}

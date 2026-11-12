using System;

namespace PictoArcher.Core
{
    public enum WavePhase { NotStarted, Fighting, Resting, Completed, Stopped }

    public sealed class WaveSequence
    {
        public const float DefaultRestSeconds = 1.5f;

        readonly float _restSeconds;
        float _restElapsed;

        public int CurrentWave { get; private set; }
        public WavePhase Phase { get; private set; } = WavePhase.NotStarted;
        public event Action<int> WaveStarted;
        public event Action Completed;

        public WaveSequence(int startWave, float restSeconds)
        {
            if (startWave < 0 || startWave > WaveLayout.TotalWaves) throw new ArgumentOutOfRangeException(nameof(startWave));
            CurrentWave = startWave;
            _restSeconds = restSeconds;
        }

        public void Begin()
        {
            if (Phase != WavePhase.NotStarted) return;
            Phase = WavePhase.Fighting;
            WaveStarted?.Invoke(CurrentWave);
        }

        public void NotifyCleared()
        {
            if (Phase != WavePhase.Fighting) return;
            if (CurrentWave == WaveLayout.TotalWaves)
            {
                Phase = WavePhase.Completed;
                Completed?.Invoke();
                return;
            }
            CurrentWave++;
            _restElapsed = 0f;
            Phase = WavePhase.Resting;
        }

        public void Tick(float deltaTime)
        {
            if (Phase != WavePhase.Resting) return;
            _restElapsed += deltaTime;
            if (_restElapsed < _restSeconds) return;
            _restElapsed = 0f;
            Phase = WavePhase.Fighting;
            WaveStarted?.Invoke(CurrentWave);
        }

        public void Stop()
        {
            if (Phase == WavePhase.Completed) return;
            Phase = WavePhase.Stopped;
        }
    }
}

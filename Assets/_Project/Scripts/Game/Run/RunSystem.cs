using System;
using PictoArcher.Core;

namespace PictoArcher.Game
{
    /// <summary>
    /// 1回のプレイの進行。ウェーブを順に始め（WaveSequence）、全滅したら休んで次へ。
    /// プレイヤーが死ぬか 50 ウェーブを全部クリアしたら止め、少し待ってから IsOver を true にする（結果の画面へ移るのはシーン）。
    /// </summary>
    public sealed class RunSystem : ITickable, IDisposable, IWaveStatus
    {
        readonly RunDefinition _definition;
        readonly WaveSpawnSystem _spawner;
        readonly IPlayerStatus _player;
        readonly WaveSequence _sequence;
        bool _ended;
        float _endTimer;

        public RunSystem(RunDefinition definition, WaveSpawnSystem spawner, IPlayerStatus player)
        {
            if (definition.Waves.Count == 0) throw new ArgumentException("RunDefinition にウェーブが1つもない", nameof(definition));
            _definition = definition;
            _spawner = spawner;
            _player = player;
            _sequence = new WaveSequence(definition.StartWave, definition.RestSeconds);
            _sequence.WaveStarted += HandleWaveStarted;
            _sequence.Completed += HandleCompleted;
            _spawner.Cleared += HandleCleared;
        }

        public int CurrentWave => _sequence.CurrentWave;
        public int TotalWaves => WaveLayout.TotalWaves;
        public bool IsCleared { get; private set; }
        public bool IsOver => _ended && _endTimer >= _definition.EndDelaySeconds;

        public void Begin() => _sequence.Begin();

        public void Tick(float deltaTime)
        {
            if (!_ended && _player.IsDead)
            {
                // 休みの間に死んでも、次のウェーブは始めない
                _sequence.Stop();
                _ended = true;
            }
            if (_ended)
            {
                _endTimer += deltaTime;
                return;
            }
            _sequence.Tick(deltaTime);
        }

        public void Dispose()
        {
            _spawner.Cleared -= HandleCleared;
            _sequence.Completed -= HandleCompleted;
            _sequence.WaveStarted -= HandleWaveStarted;
        }

        void HandleWaveStarted(int wave) => _spawner.Begin(_definition.WaveFor(wave));

        void HandleCleared()
        {
            if (!_ended) _sequence.NotifyCleared();
        }

        void HandleCompleted()
        {
            IsCleared = true;
            _ended = true;
        }
    }
}

using System;
using System.Collections.Generic;

namespace PictoArcher.Core
{
    public sealed class WaveProgress
    {
        readonly HashSet<int> _alive = new HashSet<int>();
        bool _spawningFinished;
        bool _clearedRaised;

        public int AliveCount => _alive.Count;
        public bool IsCleared => _spawningFinished && _alive.Count == 0;
        public event Action Cleared;

        public void RegisterEnemy(int enemyId) => _alive.Add(enemyId);

        public void MarkKilled(int enemyId)
        {
            if (_alive.Remove(enemyId)) RaiseIfCleared();
        }

        public void FinishSpawning()
        {
            _spawningFinished = true;
            RaiseIfCleared();
        }

        void RaiseIfCleared()
        {
            if (_clearedRaised || !IsCleared) return;
            _clearedRaised = true;
            Cleared?.Invoke();
        }
    }
}

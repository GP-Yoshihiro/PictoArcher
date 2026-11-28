using System;
using System.Collections.Generic;
using PictoArcher.Core;
using UnityEngine;

namespace PictoArcher.Game
{
    /// <summary>
    /// 1ウェーブの出現と全滅の見張り。Begin で全部の出現位置に同時に予兆を出し、出た敵を WaveProgress に登録する。
    /// 予兆が全部終わったら出し終わりにし、分裂の子を含めて全部死んだら Cleared を出す。
    /// </summary>
    public sealed class WaveSpawnSystem : ITickable, IDisposable
    {
        struct Tracked
        {
            public IEnemyTarget Enemy;
            public int Id;
        }

        readonly EnemySystem _enemies;
        readonly List<Tracked> _alive = new List<Tracked>();
        WaveProgress _progress;
        bool _waitingForSpawns;
        int _nextId;

        public event Action Cleared;

        public WaveSpawnSystem(EnemySystem enemies)
        {
            _enemies = enemies;
            _enemies.Spawned += HandleSpawned;
            _enemies.Died += HandleDied;
        }

        public void Begin(WaveDefinition wave)
        {
            if (_waitingForSpawns) throw new InvalidOperationException("前のウェーブを出し終わる前に、次のウェーブは始められません");
            if (_progress != null) _progress.Cleared -= HandleCleared;
            _alive.Clear();
            _progress = new WaveProgress();
            _progress.Cleared += HandleCleared;
            var spawns = wave.Spawns;
            for (int i = 0; i < spawns.Count; i++) _enemies.Spawn(spawns[i].Enemy, spawns[i].Position, wave.TelegraphSeconds, wave.Tier);
            _waitingForSpawns = true;
        }

        public void Tick(float deltaTime)
        {
            // 予兆が全部終わって敵になったら、出し終わり（敵が0体なら、ここでクリアになる）
            if (!_waitingForSpawns || _enemies.PendingCount > 0) return;
            _waitingForSpawns = false;
            _progress.FinishSpawning();
        }

        public void Dispose()
        {
            if (_progress != null) _progress.Cleared -= HandleCleared;
            _enemies.Died -= HandleDied;
            _enemies.Spawned -= HandleSpawned;
            _alive.Clear();
        }

        void HandleSpawned(IEnemyTarget enemy)
        {
            if (_progress == null) return;
            int id = _nextId++;
            _alive.Add(new Tracked { Enemy = enemy, Id = id });
            _progress.RegisterEnemy(id);
        }

        // EnemyDeath は敵そのものを持たないので、死んでいて同じ位置にいる1体を探す
        // （分裂する敵は子を出すまで Died が遅れて届くので、IsAlive だけで探すと早くクリアになる）
        // EnemyDeath に敵の参照がないので、死んでいて位置が同じ敵で数える。
        // まだ数えていない死んだ敵は、Died がこれから届く敵だけ（分裂の親も、子を出した次の Tick の始めに届き、見た目はまだ消えていない）
        void HandleDied(EnemyDeath death)
        {
            int fallback = -1;
            for (int i = 0; i < _alive.Count; i++)
            {
                var tracked = _alive[i];
                if (tracked.Enemy.IsAlive) continue;
                if (fallback < 0) fallback = i;
                if (tracked.Enemy.Position != death.Position) continue;
                MarkKilledAt(i);
                return;
            }
            // 位置が食い違っても、ウェーブが終わらなくならないよう、先頭の死んだ敵で数える
            if (fallback >= 0) MarkKilledAt(fallback);
        }

        void MarkKilledAt(int index)
        {
            var tracked = _alive[index];
            _alive.RemoveAt(index);
            _progress.MarkKilled(tracked.Id);
        }

        void HandleCleared() => Cleared?.Invoke();
    }
}

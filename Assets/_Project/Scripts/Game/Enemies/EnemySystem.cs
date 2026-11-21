using System;
using System.Collections.Generic;
using PictoArcher.Core;
using UnityEngine;

namespace PictoArcher.Game
{
    /// <summary>
    /// 敵の出現（予兆→出現）と、出ている敵の Tick。シーンが Init で作り、Tick で1行呼び、Dispose で片付ける。
    /// 敵が死んだらここが Registry から外し、やられのアニメが終わったら見た目を消す。
    /// </summary>
    public sealed class EnemySystem : ITickable, IDisposable, IEnemyEvents
    {
        sealed class Pending
        {
            public SpawnTelegraphView View;
            public TelegraphTimer Timer;
            public EnemyDefinition Definition;
            public Vector2 Position;
        }

        readonly EnemyRegistry _registry;
        readonly SpawnTelegraphView _telegraphPrefab;
        readonly SpawnTelegraphSettings _telegraphSettings;
        readonly ITarget _player;
        readonly IHittable _playerBody;
        readonly List<Pending> _pending = new List<Pending>();
        readonly List<EnemyUnit> _units = new List<EnemyUnit>();

        public EnemySystem(EnemyRegistry registry, SpawnTelegraphView telegraphPrefab, SpawnTelegraphSettings telegraphSettings, ITarget player, IHittable playerBody)
        {
            _registry = registry;
            _telegraphPrefab = telegraphPrefab;
            _telegraphSettings = telegraphSettings;
            _player = player;
            _playerBody = playerBody;
        }

        public event Action<DamageReport> Damaged;
        public event Action<EnemyDeath> Died;
        /// <summary>予兆が終わって敵が出たとき。ウェーブの数え方（出した数）に使う。</summary>
        public event Action<IEnemyTarget> Spawned;

        /// <summary>予兆の途中でまだ出ていない敵の数。</summary>
        public int PendingCount => _pending.Count;

        /// <summary>position に予兆を出し、seconds 秒あとにそこへ敵を出す。</summary>
        public void Spawn(EnemyDefinition definition, Vector2 position, float seconds)
        {
            // 時間が正しくないとタイマーが例外を出す。見た目を作る前に作って、予兆が管理されずに残るのを防ぐ
            var timer = new TelegraphTimer(seconds, _telegraphSettings.BlinksPerSecond);
            var view = UnityEngine.Object.Instantiate(_telegraphPrefab);
            view.Init(position, _telegraphSettings.EndScale);
            view.Apply(0f, true);
            _pending.Add(new Pending
            {
                View = view,
                Timer = timer,
                Definition = definition,
                Position = position,
            });
        }

        public void Tick(float deltaTime)
        {
            TickPending(deltaTime);
            TickUnits(deltaTime);
        }

        void TickPending(float deltaTime)
        {
            for (int i = _pending.Count - 1; i >= 0; i--)
            {
                var pending = _pending[i];
                pending.Timer.Tick(deltaTime);
                if (!pending.Timer.IsFinished)
                {
                    pending.View.Apply(pending.Timer.Progress, pending.Timer.IsVisible);
                    continue;
                }
                _pending.RemoveAt(i);
                UnityEngine.Object.Destroy(pending.View.gameObject);
                SpawnEnemy(pending.Definition, pending.Position);
            }
        }

        void SpawnEnemy(EnemyDefinition definition, Vector2 position)
        {
            var instance = UnityEngine.Object.Instantiate(definition.Prefab);
            EnemyUnit unit;
            try
            {
                var view = instance.GetComponent<EnemyView>();
                view.Init(position);
                unit = new EnemyUnit(definition, view, _player, _playerBody);
            }
            catch
            {
                // 調整値やプレハブの不備で作れなかったら、管理されない敵が残らないよう消してから知らせる
                UnityEngine.Object.Destroy(instance);
                throw;
            }
            unit.Damaged += OnUnitDamaged;
            unit.Died += OnUnitDied;
            _units.Add(unit);
            _registry.Register(unit, unit.Body);
            Spawned?.Invoke(unit);
        }

        void TickUnits(float deltaTime)
        {
            for (int i = _units.Count - 1; i >= 0; i--)
            {
                var unit = _units[i];
                unit.Tick(deltaTime);
                if (!unit.IsRemovable) continue;
                _units.RemoveAt(i);
                Release(unit);
            }
        }

        void OnUnitDamaged(DamageReport report) => Damaged?.Invoke(report);

        void OnUnitDied(EnemyUnit unit, EnemyDeath death)
        {
            Died?.Invoke(death);
            // 死んだ敵は、Died を出したあとで Registry から外す（IEnemyEvents の約束）
            _registry.Unregister(unit);
        }

        void Release(EnemyUnit unit)
        {
            unit.Damaged -= OnUnitDamaged;
            unit.Died -= OnUnitDied;
            _registry.Unregister(unit);
            DestroyIfAlive(unit.View);
        }

        // シーンごと先に消されていても、Dispose が落ちないようにする
        static void DestroyIfAlive(Component view)
        {
            if (view != null) UnityEngine.Object.Destroy(view.gameObject);
        }

        public void Dispose()
        {
            for (int i = 0; i < _pending.Count; i++) DestroyIfAlive(_pending[i].View);
            _pending.Clear();
            for (int i = 0; i < _units.Count; i++) Release(_units[i]);
            _units.Clear();
        }
    }
}

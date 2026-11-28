using System;
using System.Collections.Generic;
using PictoArcher.Core;
using UnityEngine;

namespace PictoArcher.Game
{
    /// <summary>
    /// 敵の出現（予兆→出現）と、出ている敵の Tick。シーンが Init で作り、Tick で1行呼び、Dispose で片付ける。
    /// 敵が死んだらここが Registry から外し、やられのアニメが終わったら見た目を消す。
    /// ボスの雑魚呼びもここで予兆を出し、呼んだボスが先に死んだら、まだ出ていない雑魚は出さない。
    /// </summary>
    public sealed class EnemySystem : ITickable, IDisposable, IEnemyEvents, IBossStatus
    {
        sealed class Pending
        {
            public SpawnTelegraphView View;
            public TelegraphTimer Timer;
            public EnemyDefinition Definition;
            public Vector2 Position;
            public int Tier;
            /// <summary>雑魚呼びのときの呼んだ敵。ふつうの出現は null。</summary>
            public EnemyUnit Summoner;
        }

        readonly EnemyRegistry _registry;
        readonly SpawnTelegraphView _telegraphPrefab;
        readonly SpawnTelegraphSettings _telegraphSettings;
        readonly AttackTelegraphView _attackTelegraphPrefab;
        readonly EnemyProjectileSystem _projectiles;
        readonly ITarget _player;
        readonly IHittable _playerBody;
        readonly IChaseRoute _route;
        readonly List<Pending> _pending = new List<Pending>();
        readonly List<EnemyUnit> _units = new List<EnemyUnit>();
        readonly List<KeyValuePair<EnemyUnit, int>> _splits = new List<KeyValuePair<EnemyUnit, int>>();
        readonly List<EnemyDeath> _deferredDeaths = new List<EnemyDeath>();

        public EnemySystem(EnemyRegistry registry, SpawnTelegraphView telegraphPrefab, SpawnTelegraphSettings telegraphSettings,
            AttackTelegraphView attackTelegraphPrefab, EnemyProjectileSystem projectiles, ITarget player, IHittable playerBody,
            IChaseRoute route = null)
        {
            _registry = registry;
            _telegraphPrefab = telegraphPrefab;
            _telegraphSettings = telegraphSettings;
            _attackTelegraphPrefab = attackTelegraphPrefab;
            _projectiles = projectiles;
            _player = player;
            _playerBody = playerBody;
            _route = route;
        }

        public event Action<DamageReport> Damaged;
        public event Action<EnemyDeath> Died;
        /// <summary>予兆が終わって敵が出たとき。ウェーブの数え方（出した数）に使う。</summary>
        public event Action<IEnemyTarget> Spawned;

        /// <summary>予兆の途中でまだ出ていない敵の数。</summary>
        public int PendingCount => _pending.Count;

        /// <summary>position に予兆を出し、seconds 秒あとにそこへ敵を出す。</summary>
        /// <summary>tier は何回目の登場か（0 が最初）。中ボスなどは tier が上がるほど HP・速さ・分裂の数が増える。定義に予兆の絵があれば、ふつうの予兆の代わりにそれを出す。</summary>
        public void Spawn(EnemyDefinition definition, Vector2 position, float seconds, int tier = 0) => AddPending(definition, position, seconds, tier, null);

        void AddPending(EnemyDefinition definition, Vector2 position, float seconds, int tier, EnemyUnit summoner)
        {
            // 時間が正しくないとタイマーが例外を出す。見た目を作る前に作って、予兆が管理されずに残るのを防ぐ
            var timer = new TelegraphTimer(seconds, _telegraphSettings.BlinksPerSecond);
            var view = UnityEngine.Object.Instantiate(definition.SpawnTelegraph != null ? definition.SpawnTelegraph : _telegraphPrefab);
            view.Init(position, _telegraphSettings.EndScale);
            view.Apply(0f, true);
            _pending.Add(new Pending
            {
                View = view,
                Timer = timer,
                Definition = definition,
                Position = position,
                Tier = tier,
                Summoner = summoner,
            });
        }

        public void Tick(float deltaTime)
        {
            SpawnSplitChildren();
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
                SpawnEnemy(pending.Definition, pending.Position, pending.Tier);
            }
        }

        void SpawnEnemy(EnemyDefinition definition, Vector2 position, int tier)
        {
            var instance = UnityEngine.Object.Instantiate(definition.Prefab);
            EnemyUnit unit;
            try
            {
                var view = instance.GetComponent<EnemyView>();
                view.Init(position);
                unit = new EnemyUnit(definition, view, _player, _playerBody, _attackTelegraphPrefab, _projectiles, tier, _route);
            }
            catch
            {
                // 調整値やプレハブの不備で作れなかったら、管理されない敵が残らないよう消してから知らせる
                UnityEngine.Object.Destroy(instance);
                throw;
            }
            unit.Damaged += OnUnitDamaged;
            unit.Died += OnUnitDied;
            unit.SplitRequested += OnUnitSplitRequested;
            unit.SummonRequested += OnUnitSummonRequested;
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

        // 呼んだ雑魚はいつも最初の強さ（tier 0）
        void OnUnitSummonRequested(EnemyUnit summoner, EnemyDefinition enemy, Vector2 position, float seconds)
            => AddPending(enemy, position, seconds, 0, summoner);

        // 呼んだ敵が死んだら、まだ予兆の途中の雑魚は出さない（ボスを倒したのにあとから雑魚が出ないように）
        void CancelSummonsOf(EnemyUnit summoner)
        {
            for (int i = _pending.Count - 1; i >= 0; i--)
            {
                if (_pending[i].Summoner != summoner) continue;
                DestroyIfAlive(_pending[i].View);
                _pending.RemoveAt(i);
            }
        }

        // ---- IBossStatus ----

        public bool HasBoss => FindBoss() != null;
        public int BossHp => FindBoss()?.Hp ?? 0;
        public int BossMaxHp => FindBoss()?.MaxHp ?? 0;

        EnemyUnit FindBoss()
        {
            for (int i = 0; i < _units.Count; i++)
            {
                if (_units[i].IsAlive && _units[i].Definition.ShowsBossBar) return _units[i];
            }
            return null;
        }

        // 当てた側の処理の途中で敵を増やさないよう、分裂は次の Tick の始めに出す
        void OnUnitSplitRequested(EnemyUnit unit, int count) => _splits.Add(new KeyValuePair<EnemyUnit, int>(unit, count));

        void SpawnSplitChildren()
        {
            if (_splits.Count > 0) SpawnRequestedChildren();
            FlushDeferredDeaths();
        }

        void SpawnRequestedChildren()
        {
            var requests = _splits.ToArray();
            _splits.Clear();
            for (int r = 0; r < requests.Length; r++)
            {
                var parent = requests[r].Key;
                if (parent.View == null) continue;   // 先に片付いていたら出さない
                int count = requests[r].Value;
                var split = parent.Definition.Split;
                var origin = parent.Position;
                for (int i = 0; i < count; i++)
                {
                    float angle = Mathf.PI * 2f * i / count;
                    var offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * split.SpawnRadius;
                    SpawnEnemy(split.Child, origin + offset, 0);
                }
            }
        }

        bool HasPendingSplit(EnemyUnit unit)
        {
            for (int i = 0; i < _splits.Count; i++)
            {
                if (_splits[i].Key == unit) return true;
            }
            return false;
        }

        void FlushDeferredDeaths()
        {
            if (_deferredDeaths.Count == 0) return;
            var deaths = _deferredDeaths.ToArray();
            _deferredDeaths.Clear();
            for (int i = 0; i < deaths.Length; i++) Died?.Invoke(deaths[i]);
        }

        void OnUnitDied(EnemyUnit unit, EnemyDeath death)
        {
            CancelSummonsOf(unit);
            // 分裂の子がまだ出ていない敵が死んだら、Died は子を出したあとに届ける（先に届くと、ウェーブが「敵が0体」と見てクリアにしてしまう）
            if (HasPendingSplit(unit)) _deferredDeaths.Add(death);
            else Died?.Invoke(death);
            // 死んだ敵は、Died を出したあとで Registry から外す（IEnemyEvents の約束）
            _registry.Unregister(unit);
        }

        void Release(EnemyUnit unit)
        {
            unit.Damaged -= OnUnitDamaged;
            unit.Died -= OnUnitDied;
            unit.SplitRequested -= OnUnitSplitRequested;
            unit.SummonRequested -= OnUnitSummonRequested;
            unit.ReleaseTelegraphs();
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
            _splits.Clear();
            _deferredDeaths.Clear();
            for (int i = 0; i < _units.Count; i++) Release(_units[i]);
            _units.Clear();
        }
    }
}

using System.Collections.Generic;
using PictoArcher.Core;
using UnityEngine;

namespace PictoArcher.Game
{
    /// <summary>
    /// 今いる敵の一覧と、近くの敵を探す問い合わせ。BattleScene が1つ作って、使う System に渡す（static にしない）。
    /// 敵を出す側が出したときに Register し、死んだとき（Died を出したあと）か消すときに Unregister する。
    /// 問い合わせは死んでいる敵（IsAlive が false）を返さない。
    /// </summary>
    public sealed class EnemyRegistry
    {
        readonly List<IEnemyTarget> _active = new List<IEnemyTarget>();
        readonly Dictionary<Collider2D, IEnemyTarget> _byBody = new Dictionary<Collider2D, IEnemyTarget>();
        readonly List<Vector2> _positions = new List<Vector2>();
        readonly List<IEnemyTarget> _candidates = new List<IEnemyTarget>();

        public IReadOnlyList<IEnemyTarget> Active => _active;

        /// <summary>body は弾が当たる Collider2D。CollisionRelay2D から受け取った相手を TryGet で敵に戻せる。同じ敵を2回登録しても1つだけ持つ。</summary>
        public void Register(IEnemyTarget enemy, Collider2D body)
        {
            if (_active.Contains(enemy)) return;
            _active.Add(enemy);
            if (body != null) _byBody[body] = enemy;
        }

        /// <summary>登録していない敵は無視する。</summary>
        public void Unregister(IEnemyTarget enemy)
        {
            if (!_active.Remove(enemy)) return;
            Collider2D found = null;
            foreach (var pair in _byBody)
            {
                if (pair.Value != enemy) continue;
                found = pair.Key;
                break;
            }
            if (!ReferenceEquals(found, null)) _byBody.Remove(found);
        }

        /// <summary>当たった Collider2D から敵を引く。登録していない相手（壁、プレイヤー）や null は false。</summary>
        public bool TryGet(Collider2D body, out IEnemyTarget enemy)
        {
            enemy = null;
            return !ReferenceEquals(body, null) && _byBody.TryGetValue(body, out enemy);
        }

        /// <summary>
        /// from に一番近い、生きている敵。exclude は選ばない（敵から敵へ跳ねる弾、雷の連鎖で自分を外すとき）。いなければ false。
        /// </summary>
        public bool TryFindNearest(Vector2 from, out IEnemyTarget nearest, IEnemyTarget exclude = null)
        {
            _positions.Clear();
            _candidates.Clear();
            for (int i = 0; i < _active.Count; i++)
            {
                var enemy = _active[i];
                if (!enemy.IsAlive || enemy == exclude) continue;
                _candidates.Add(enemy);
                _positions.Add(enemy.Position);
            }

            int index = TargetSelector.FindNearest(from, _positions);
            nearest = index < 0 ? null : _candidates[index];
            _candidates.Clear();
            return nearest != null;
        }

        /// <summary>center から radius 以内（ちょうども含む）の生きている敵で results を置き換え、その数を返す（爆発、吸い寄せ）。results は呼ぶ側が持ち回して使う。</summary>
        public int FindWithin(Vector2 center, float radius, List<IEnemyTarget> results)
        {
            results.Clear();
            float sqrRadius = radius * radius;
            for (int i = 0; i < _active.Count; i++)
            {
                var enemy = _active[i];
                if (enemy.IsAlive && (enemy.Position - center).sqrMagnitude <= sqrRadius) results.Add(enemy);
            }
            return results.Count;
        }

        public void Clear()
        {
            _active.Clear();
            _byBody.Clear();
        }
    }
}

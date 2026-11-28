using System;
using System.Collections.Generic;
using PictoArcher.Core;
using UnityEngine;

namespace PictoArcher.Game
{
    /// <summary>
    /// プレイヤーの弾の発射と、飛んでいる弾の Tick（寿命、当たり）。シーンが Init で作り、Tick で1行呼び、Dispose で片付ける。
    /// 1発は1体にだけ当たって消える。壁（障害物も Wall）に触れたら消える。当たりは CollisionRelay2D が記録したものを Tick で見る。
    /// </summary>
    public sealed class PlayerProjectileSystem : ITickable, IDisposable
    {
        sealed class Flying
        {
            public PlayerProjectileView View;
            public float Remaining;
            public int Damage;
        }

        readonly EnemyRegistry _enemies;
        readonly PlayerProjectileView _prefab;
        readonly List<Flying> _flying = new List<Flying>();
        readonly List<Collider2D> _entered = new List<Collider2D>();

        public PlayerProjectileSystem(EnemyRegistry enemies, PlayerProjectileView prefab)
        {
            _enemies = enemies;
            _prefab = prefab;
        }

        /// <summary>今飛んでいる弾の数。</summary>
        public int Count => _flying.Count;

        public void Fire(Vector2 origin, Vector2 direction, float speed, int damage, float lifetime, SpriteVisual visual)
        {
            var view = UnityEngine.Object.Instantiate(_prefab);
            view.Launch(origin, direction.normalized * speed, visual);
            _flying.Add(new Flying { View = view, Remaining = lifetime, Damage = damage });
        }

        public void Tick(float deltaTime)
        {
            for (int i = _flying.Count - 1; i >= 0; i--)
            {
                var flying = _flying[i];
                flying.Remaining -= deltaTime;
                if (flying.Remaining <= 0f || Hit(flying))
                {
                    _flying.RemoveAt(i);
                    UnityEngine.Object.Destroy(flying.View.gameObject);
                }
            }
        }

        // 敵か壁に触れていたら true（敵なら最初の1体にだけ TakeHit する）
        bool Hit(Flying flying)
        {
            _entered.Clear();
            flying.View.Relay.DrainEntered(_entered);
            for (int i = 0; i < _entered.Count; i++)
            {
                var other = _entered[i];
                if (other == null) continue;
                int layer = other.gameObject.layer;
                if (layer == Layers.Wall) return true;
                if (layer == Layers.Enemy && _enemies.TryGet(other, out var enemy) && enemy.IsAlive)
                {
                    enemy.TakeHit(new Damage(flying.Damage));
                    return true;
                }
            }
            return false;
        }

        public void Dispose()
        {
            for (int i = 0; i < _flying.Count; i++)
            {
                if (_flying[i].View != null) UnityEngine.Object.Destroy(_flying[i].View.gameObject);
            }
            _flying.Clear();
        }
    }
}

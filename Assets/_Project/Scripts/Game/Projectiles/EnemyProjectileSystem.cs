using System;
using System.Collections.Generic;
using PictoArcher.Core;
using UnityEngine;

namespace PictoArcher.Game
{
    /// <summary>
    /// 敵の弾の発射と、飛んでいる弾の Tick（寿命、当たり）。シーンが Init で作り、Tick で1行呼び、Dispose で片付ける。
    /// プレイヤーに触れたら TakeHit して消え、壁に触れたら消える。当たりは CollisionRelay2D が記録したものを Tick で見る。
    /// </summary>
    public sealed class EnemyProjectileSystem : ITickable, IDisposable
    {
        sealed class Flying
        {
            public EnemyProjectileView View;
            public float Remaining;
            public int Damage;
        }

        readonly IHittable _playerBody;
        readonly List<Flying> _flying = new List<Flying>();
        readonly List<Collider2D> _enteredScratch = new List<Collider2D>();

        public EnemyProjectileSystem(IHittable playerBody)
        {
            _playerBody = playerBody;
        }

        /// <summary>今飛んでいる弾の数。</summary>
        public int Count => _flying.Count;

        public void Fire(EnemyProjectileDefinition definition, Vector2 origin, Vector2 direction)
        {
            var view = UnityEngine.Object.Instantiate(definition.Prefab);
            view.Launch(origin, direction.normalized * definition.Speed, definition.Visual);
            _flying.Add(new Flying { View = view, Remaining = definition.Lifetime, Damage = definition.Damage });
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

        // プレイヤーか壁に触れていたら true（プレイヤーなら TakeHit する）
        bool Hit(Flying flying)
        {
            _enteredScratch.Clear();
            flying.View.Relay.DrainEntered(_enteredScratch);
            _enteredScratch.Clear();
            foreach (var other in flying.View.Relay.Touching)
            {
                if (other == null) continue;
                int layer = other.gameObject.layer;
                if (layer == Layers.Player)
                {
                    _playerBody.TakeHit(new Damage(flying.Damage));
                    return true;
                }
                if (layer == Layers.Wall) return true;
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

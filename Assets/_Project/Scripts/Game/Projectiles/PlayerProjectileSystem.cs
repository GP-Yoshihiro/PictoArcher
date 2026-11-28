using System;
using System.Collections.Generic;
using PictoArcher.Core;
using UnityEngine;

namespace PictoArcher.Game
{
    /// <summary>
    /// プレイヤーの弾・光線・爆発の発射と、飛んでいる弾の Tick（寿命、当たり）。シーンが Init で作り、Tick で1行呼び、Dispose で片付ける。
    /// 弾は1体にだけ当たって消える。爆発する弾は敵か壁に当たった所で爆発し、半径の内の敵すべてに当たる。壁（障害物も Wall）に触れたら消える。
    /// 光線は撃ったその場で、壁までの線の上の敵すべてに当たる。当たりは CollisionRelay2D が記録したものを Tick で見る。
    /// </summary>
    public sealed class PlayerProjectileSystem : ITickable, IDisposable, IPlayerShotPort
    {
        sealed class Flying
        {
            public PlayerProjectileView View;
            public float Remaining;
            public int Damage;
            public bool Explodes;
            public float BlastRadius;
            public SpriteVisual BlastVisual;
            public float BlastShowSeconds;
        }

        sealed class Effect
        {
            public ShotEffectView View;
            public float Remaining;
            public IReadOnlyList<Sprite> Frames;
            public readonly FrameClock Clock = new FrameClock();
        }

        readonly EnemyRegistry _enemies;
        readonly PlayerProjectileView _prefab;
        readonly ShotEffectView _effectPrefab;
        readonly List<Flying> _flying = new List<Flying>();
        readonly List<Effect> _effects = new List<Effect>();
        readonly List<Collider2D> _entered = new List<Collider2D>();
        readonly List<IEnemyTarget> _struck = new List<IEnemyTarget>();

        public PlayerProjectileSystem(EnemyRegistry enemies, PlayerProjectileView prefab, ShotEffectView effectPrefab)
        {
            _enemies = enemies;
            _prefab = prefab;
            _effectPrefab = effectPrefab;
        }

        /// <summary>今飛んでいる弾の数（光線と爆発の見た目は数えない）。</summary>
        public int Count => _flying.Count;

        /// <summary>今見えている光線と爆発の数。</summary>
        public int EffectCount => _effects.Count;

        public void Fire(Vector2 origin, Vector2 direction, float speed, int damage, float lifetime, SpriteVisual visual)
            => Launch(origin, direction, speed, damage, lifetime, visual, false, 0f, null, 0f);

        public void FireShell(Vector2 origin, Vector2 direction, float speed, int damage, float lifetime, SpriteVisual visual,
            float blastRadius, SpriteVisual blastVisual, float blastShowSeconds)
            => Launch(origin, direction, speed, damage, lifetime, visual, true, Mathf.Max(blastRadius, 0f), blastVisual, blastShowSeconds);

        public void FireBeam(Vector2 origin, Vector2 direction, float length, float hitRadius, int damage,
            SpriteVisual visual, float visualWidth, float showSeconds)
        {
            if (direction.sqrMagnitude <= 0f) return;
            var unit = direction.normalized;
            // 壁（障害物も Wall）があれば、そこで止まる
            var wall = Physics2D.Raycast(origin, unit, length, Layers.WallMask);
            float reach = wall.collider != null ? wall.distance : length;

            // 当てると死んで一覧が変わることがあるので、先に当たる敵を集めてから当てる
            _struck.Clear();
            var active = _enemies.Active;
            for (int i = 0; i < active.Count; i++)
            {
                if (active[i].IsAlive && BeamHit.Hits(origin, unit, reach, hitRadius, active[i].Position)) _struck.Add(active[i]);
            }
            for (int i = 0; i < _struck.Count; i++) _struck[i].TakeHit(new Damage(damage));

            // 絵は縦長なので、上向きを光線の向きにそろえる
            float degrees = Mathf.Atan2(unit.y, unit.x) * Mathf.Rad2Deg - 90f;
            ShowEffect(origin + unit * (reach * 0.5f), degrees, new Vector2(visualWidth, reach), visual, showSeconds);
        }

        void Launch(Vector2 origin, Vector2 direction, float speed, int damage, float lifetime, SpriteVisual visual,
            bool explodes, float blastRadius, SpriteVisual blastVisual, float blastShowSeconds)
        {
            var view = UnityEngine.Object.Instantiate(_prefab);
            view.Launch(origin, direction.normalized * speed, visual);
            _flying.Add(new Flying
            {
                View = view, Remaining = lifetime, Damage = damage,
                Explodes = explodes, BlastRadius = blastRadius, BlastVisual = blastVisual, BlastShowSeconds = blastShowSeconds,
            });
        }

        public void Tick(float deltaTime)
        {
            for (int i = _flying.Count - 1; i >= 0; i--)
            {
                var flying = _flying[i];
                flying.Remaining -= deltaTime;
                bool done = flying.Remaining <= 0f;
                if (!done && Hit(flying, out var enemy))
                {
                    done = true;
                    if (flying.Explodes) Explode(flying, enemy);
                    else enemy?.TakeHit(new Damage(flying.Damage));
                }
                if (!done) continue;
                _flying.RemoveAt(i);
                UnityEngine.Object.Destroy(flying.View.gameObject);
            }
            TickEffects(deltaTime);
        }

        // 敵か壁に触れていたら true。敵なら最初の1体を enemy に入れる（壁なら null）
        bool Hit(Flying flying, out IEnemyTarget enemy)
        {
            enemy = null;
            _entered.Clear();
            flying.View.Relay.DrainEntered(_entered);
            // 同じフレームに壁と敵の両方に触れたら、壁を優先して敵には当てない（壁の向こうの敵に当てない）
            for (int i = 0; i < _entered.Count; i++)
            {
                if (_entered[i] != null && _entered[i].gameObject.layer == Layers.Wall) return true;
            }
            for (int i = 0; i < _entered.Count; i++)
            {
                var other = _entered[i];
                if (other == null) continue;
                // 予兆の間や死んだあとの敵（Registry にいない・IsAlive が false）は、弾がすり抜ける
                if (other.gameObject.layer == Layers.Enemy && _enemies.TryGet(other, out var found) && found.IsAlive)
                {
                    enemy = found;
                    return true;
                }
            }
            return false;
        }

        // 弾のいる所で爆発し、半径の内の敵すべてに1回ずつ当てる。直接当たった敵は、中心が半径の外でも当てる
        void Explode(Flying flying, IEnemyTarget direct)
        {
            var center = flying.View.Position;
            _enemies.FindWithin(center, flying.BlastRadius, _struck);
            if (direct != null && !_struck.Contains(direct)) _struck.Add(direct);
            for (int i = 0; i < _struck.Count; i++) _struck[i].TakeHit(new Damage(flying.Damage));
            float diameter = flying.BlastRadius * 2f;
            ShowEffect(center, 0f, new Vector2(diameter, diameter), flying.BlastVisual, flying.BlastShowSeconds);
        }

        // 見た目だけを seconds の間出す。コマは seconds の間に1回だけ流す
        void ShowEffect(Vector2 position, float degrees, Vector2 size, SpriteVisual visual, float seconds)
        {
            if (_effectPrefab == null || visual == null || seconds <= 0f) return;
            var frames = visual.Clip.Frames;
            if (frames.Count == 0) return;
            var view = UnityEngine.Object.Instantiate(_effectPrefab);
            view.Show(position, degrees, size, frames[0]);
            var effect = new Effect { View = view, Remaining = seconds, Frames = frames };
            effect.Clock.Play(frames.Count, frames.Count / seconds, false);
            _effects.Add(effect);
        }

        void TickEffects(float deltaTime)
        {
            for (int i = _effects.Count - 1; i >= 0; i--)
            {
                var effect = _effects[i];
                effect.Remaining -= deltaTime;
                if (effect.Remaining > 0f)
                {
                    effect.Clock.Tick(deltaTime);
                    effect.View.SetFrame(effect.Frames[effect.Clock.FrameIndex]);
                    continue;
                }
                _effects.RemoveAt(i);
                UnityEngine.Object.Destroy(effect.View.gameObject);
            }
        }

        public void Dispose()
        {
            for (int i = 0; i < _flying.Count; i++)
            {
                if (_flying[i].View != null) UnityEngine.Object.Destroy(_flying[i].View.gameObject);
            }
            _flying.Clear();
            for (int i = 0; i < _effects.Count; i++)
            {
                if (_effects[i].View != null) UnityEngine.Object.Destroy(_effects[i].View.gameObject);
            }
            _effects.Clear();
        }
    }
}

using System;
using PictoArcher.Core;
using UnityEngine;

namespace PictoArcher.Game
{
    /// <summary>
    /// プレイヤーの体（移動、自動射撃、被弾と無敵時間の点滅、アニメ）。シーンが Init で作り、Tick で1行呼び、Dispose で片付ける。
    /// 敵からは ITarget（追う先）と IHittable（当たり）、HUD からは IPlayerStatus として見える。
    /// </summary>
    public sealed class PlayerSystem : ITickable, IDisposable, ITarget, IHittable, IPlayerStatus
    {
        readonly PlayerView _view;
        readonly PlayerStats _stats;
        readonly SpriteVisual _shotVisual;
        readonly IMoveInput _input;
        readonly EnemyRegistry _enemies;
        readonly PlayerProjectileSystem _shots;
        readonly Health _health;
        readonly HitInvulnerability _invulnerability;
        readonly FireTimer _fireTimer;
        readonly CharacterSpriteAnimator _animator;
        float _now;

        /// <summary>被弾して HP が減ったとき（減った量）。</summary>
        public event Action<int> Damaged;
        /// <summary>HP が 0 になったとき（1回だけ）。</summary>
        public event Action Died;

        public PlayerSystem(PlayerView prefab, Vector2 position, PlayerStats stats, CharacterVisual visual, SpriteVisual shotVisual,
            IMoveInput input, EnemyRegistry enemies, PlayerProjectileSystem shots)
        {
            _stats = stats;
            _shotVisual = shotVisual;
            _input = input;
            _enemies = enemies;
            _shots = shots;
            _health = new Health(stats.MaxHp);
            _invulnerability = new HitInvulnerability(stats.InvulnerabilitySeconds);
            _fireTimer = new FireTimer(stats.FireInterval, stats.MoveFireRateMultiplier);
            _view = UnityEngine.Object.Instantiate(prefab);
            _view.Init(position);
            _animator = new CharacterSpriteAnimator(_view.Sprite, visual);
        }

        public Vector2 Position => _view.Position;
        public bool IsAlive => !_health.IsDead;
        public int Hp => _health.Current;
        public int MaxHp => _health.Max;
        public bool IsDead => _health.IsDead;
        public bool IsInvulnerable => _invulnerability.IsActive(_now);

        public void Tick(float deltaTime)
        {
            _now += deltaTime;
            if (IsDead)
            {
                _view.SetVelocity(Vector2.zero);
                _view.SetVisible(true);
                _animator.Tick(deltaTime);
                return;
            }

            Vector2 move = Vector2.ClampMagnitude(_input.ReadMove(_stats.DragRadiusPixels), 1f);
            bool moving = move.sqrMagnitude > 0f;
            _view.SetVelocity(move * _stats.MoveSpeed);
            if (move.x < 0f) _animator.SetFacingLeft(true);
            else if (move.x > 0f) _animator.SetFacingLeft(false);

            bool hasTarget = _enemies.TryFindNearest(Position, out var target);
            if (_fireTimer.Tick(deltaTime, hasTarget, moving))
            {
                _shots.Fire(Position, AimDirection(target.Position - Position), _stats.ProjectileSpeed, _stats.ProjectileDamage, _stats.ProjectileLifetime, _shotVisual);
                _animator.Play(CharacterPose.Attack);
            }
            else if (_animator.Pose != CharacterPose.Attack || _animator.IsFinished)
            {
                _animator.Play(moving ? CharacterPose.Move : CharacterPose.Idle);
            }
            _animator.Tick(deltaTime);

            _view.SetVisible(!IsInvulnerable || Mathf.Repeat(_now, _view.BlinkSeconds * 2f) >= _view.BlinkSeconds);
        }

        public void TakeHit(Damage damage)
        {
            if (IsDead || !_invulnerability.TryHit(_now)) return;
            int dealt = _health.ApplyDamage(damage.Amount);
            Damaged?.Invoke(dealt);
            if (!_health.IsDead) return;
            _animator.Play(CharacterPose.Death);
            Died?.Invoke();
        }

        // 敵と完全に重なっていると向きが (0,0) になり弾が止まるので、向いている側へ撃つ
        Vector2 AimDirection(Vector2 toTarget)
        {
            if (toTarget.sqrMagnitude > 0.0001f) return toTarget;
            return _view.Sprite.flipX ? Vector2.left : Vector2.right;
        }

        public void Dispose()
        {
            if (_view != null) UnityEngine.Object.Destroy(_view.gameObject);
        }
    }
}

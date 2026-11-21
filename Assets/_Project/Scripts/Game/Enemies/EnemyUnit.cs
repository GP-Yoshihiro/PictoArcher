using System;
using System.Collections.Generic;
using PictoArcher.Core;
using UnityEngine;

namespace PictoArcher.Game
{
    /// <summary>
    /// 敵1体の実行時の状態（HP、動き、アニメ）。定義（EnemyDefinition）からは値を読むだけで、書き換えない。
    /// EnemySystem が作って Tick を呼ぶ。弾などは IHittable.TakeHit で当てる。
    /// </summary>
    public sealed class EnemyUnit : IEnemyTarget
    {
        const float FaceThreshold = 0.01f;

        readonly EnemyDefinition _definition;
        readonly EnemyView _view;
        readonly Health _health;
        readonly ChaseMovement _movement;
        readonly CharacterSpriteAnimator _animator;
        readonly ITarget _player;
        readonly IHittable _playerBody;
        readonly List<Collider2D> _enteredScratch = new List<Collider2D>();
        bool _deathRaised;

        public EnemyUnit(EnemyDefinition definition, EnemyView view, ITarget player, IHittable playerBody)
        {
            _definition = definition;
            _view = view;
            _player = player;
            _playerBody = playerBody;
            _health = new Health(definition.MaxHp);
            _movement = new ChaseMovement(definition.MoveSpeed);
            _animator = new CharacterSpriteAnimator(view.Sprite, definition.Visual);
        }

        public event Action<DamageReport> Damaged;
        /// <summary>死んだとき1回だけ。EnemySystem が自分を渡して受け取り、Registry から外す。</summary>
        public event Action<EnemyUnit, EnemyDeath> Died;

        public EnemyDefinition Definition => _definition;
        public EnemyView View => _view;
        public Collider2D Body => _view.Body;
        public Vector2 Position => _view.Position;
        public bool IsAlive => !_health.IsDead;

        /// <summary>死んで、やられのアニメも終わった。EnemySystem が片付けてよい。</summary>
        public bool IsRemovable => _health.IsDead && _animator.IsFinished;

        public void TakeHit(Damage damage)
        {
            int dealt = _health.ApplyDamage(damage.Amount);
            if (dealt <= 0) return;   // 死んだあとの当たり、0 ダメージは何も起こさない
            bool killed = _health.IsDead;
            Damaged?.Invoke(new DamageReport(this, Position, dealt, damage.Kind, killed));
            if (killed) Die();
        }

        /// <summary>状態異常は、炎・氷・毒の強化を作るタスクで足す。今は何も起こらない。</summary>
        public void ApplyStatus(StatusEffect effect) { }

        public void Tick(float deltaTime)
        {
            if (IsAlive) TickAlive();
            _animator.Tick(deltaTime);
        }

        void TickAlive()
        {
            if (_player.IsAlive)
            {
                var velocity = _movement.Velocity(Position, _player.Position);
                _view.SetVelocity(velocity);
                if (Mathf.Abs(velocity.x) > FaceThreshold) _animator.SetFacingLeft(velocity.x < 0f);
                _animator.Play(CharacterPose.Move);
            }
            else
            {
                _view.SetVelocity(Vector2.zero);
                _animator.Play(CharacterPose.Idle);
            }

            // 「入ってきた」記録は使わないが、溜まり続けないよう毎回捨てる
            _enteredScratch.Clear();
            _view.Relay.DrainEntered(_enteredScratch);
            _enteredScratch.Clear();

            // 触れている間は毎フレーム当てる。続けて減らないのはプレイヤー側の無敵時間で防ぐ
            if (!_player.IsAlive) return;
            foreach (var other in _view.Relay.Touching)
            {
                if (other == null || other.gameObject.layer != Layers.Player) continue;
                _playerBody.TakeHit(new Damage(_definition.ContactDamage));
                break;
            }
        }

        void Die()
        {
            if (_deathRaised) return;
            _deathRaised = true;
            _view.Freeze();
            _animator.Play(CharacterPose.Death);
            Died?.Invoke(this, new EnemyDeath(_definition, Position, _definition.XpReward, _definition.CoinReward));
        }
    }
}

using System;
using System.Collections.Generic;
using PictoArcher.Core;
using UnityEngine;

namespace PictoArcher.Game
{
    /// <summary>
    /// 敵1体の実行時の状態（HP、動き、アニメ、予兆）。定義（EnemyDefinition）からは値を読むだけで、書き換えない。
    /// EnemySystem が作って Tick を呼ぶ。弾などは IHittable.TakeHit で当てる。
    /// 動きと攻撃は IEnemyBehavior（定義から作る）に任せ、その予兆と攻撃はこのクラスが IEnemyAttackPort として受ける。
    /// </summary>
    public sealed class EnemyUnit : IEnemyTarget, IEnemyAttackPort
    {
        const float FaceThreshold = 0.01f;

        readonly EnemyDefinition _definition;
        readonly EnemyView _view;
        readonly Health _health;
        readonly IEnemyBehavior _behavior;
        readonly CharacterSpriteAnimator _animator;
        readonly ITarget _player;
        readonly IHittable _playerBody;
        readonly AttackTelegraphView _telegraphPrefab;
        readonly EnemyProjectileSystem _projectiles;
        readonly List<Collider2D> _enteredScratch = new List<Collider2D>();
        readonly List<AttackTelegraphView> _lineViews = new List<AttackTelegraphView>();
        AttackTelegraphView _circleView;
        int _linesUsed;
        bool _circleUsed;
        readonly SplitTracker _splitTracker;
        readonly float _moveSpeed;
        bool _deathRaised;

        public EnemyUnit(EnemyDefinition definition, EnemyView view, ITarget player, IHittable playerBody,
            AttackTelegraphView telegraphPrefab, EnemyProjectileSystem projectiles, int tier = 0)
        {
            if (definition.Behavior == null) throw new ArgumentException(definition.name + " に動きと攻撃（Behavior）が入っていません。", nameof(definition));
            if (definition.MoveSpeed < 0f) throw new ArgumentOutOfRangeException(nameof(definition), definition.name + " の速度が負です。");
            _definition = definition;
            _view = view;
            _player = player;
            _playerBody = playerBody;
            _telegraphPrefab = telegraphPrefab;
            _projectiles = projectiles;
            Tier = tier;
            _health = new Health(TierScaling.Scale(definition.MaxHp, definition.HpGrowthPerTier, tier));
            _moveSpeed = TierScaling.Scale(definition.MoveSpeed, definition.SpeedGrowthPerTier, tier);
            _splitTracker = definition.Split == null ? null : new SplitTracker(definition.Split.HpThresholds);
            _behavior = definition.Behavior.Create();
            _animator = new CharacterSpriteAnimator(view.Sprite, definition.Visual);
        }

        public event Action<DamageReport> Damaged;
        /// <summary>死んだとき1回だけ。EnemySystem が自分を渡して受け取り、Registry から外す。</summary>
        public event Action<EnemyUnit, EnemyDeath> Died;

        /// <summary>HP が減って分裂するとき、出す数。EnemySystem が受け取って、次の Tick で出す。</summary>
        public event Action<EnemyUnit, int> SplitRequested;

        /// <summary>何回目の登場か（0 が最初）。HP・速さ・分裂の数がこれで増える。</summary>
        public int Tier { get; }
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
            else RequestSplit();
        }

        /// <summary>状態異常は、炎・氷・毒の強化を作るタスクで足す。今は何も起こらない。</summary>
        public void ApplyStatus(StatusEffect effect) { }

        public void Tick(float deltaTime)
        {
            if (IsAlive) TickAlive(deltaTime);
            _animator.Tick(deltaTime);
        }

        /// <summary>予兆の見た目を片付ける（EnemySystem が敵を消すときに呼ぶ）。</summary>
        public void ReleaseTelegraphs()
        {
            for (int i = 0; i < _lineViews.Count; i++) DestroyView(_lineViews[i]);
            _lineViews.Clear();
            DestroyView(_circleView);
            _circleView = null;
        }

        void TickAlive(float deltaTime)
        {
            _linesUsed = 0;
            _circleUsed = false;
            var velocity = Vector2.zero;
            if (_player.IsAlive)
            {
                var context = new EnemyContext(Position, _player.Position, _moveSpeed);
                velocity = _behavior.Tick(deltaTime, context, this);
            }
            if (!IsAlive) return;   // 自爆したときは、このあとの動きや見た目を更新しない
            HideUnusedTelegraphs();

            _view.SetVelocity(velocity);
            UpdateAppearance(velocity);
            DiscardEntered();
            HitPlayerOnContact();
        }

        void UpdateAppearance(Vector2 velocity)
        {
            float faceX = Mathf.Abs(velocity.x) > FaceThreshold ? velocity.x : _player.Position.x - Position.x;
            if (Mathf.Abs(faceX) > FaceThreshold) _animator.SetFacingLeft(faceX < 0f);

            bool telegraphing = _linesUsed > 0 || _circleUsed;
            var pose = telegraphing ? CharacterPose.Attack : velocity != Vector2.zero ? CharacterPose.Move : CharacterPose.Idle;
            if (_animator.Pose != pose) _animator.Play(pose);
        }

        // 「入ってきた」記録は使わないが、溜まり続けないよう毎回捨てる
        void DiscardEntered()
        {
            _enteredScratch.Clear();
            _view.Relay.DrainEntered(_enteredScratch);
            _enteredScratch.Clear();
        }

        // 触れている間は毎フレーム当てる。続けて減らないのはプレイヤー側の無敵時間で防ぐ
        void HitPlayerOnContact()
        {
            if (!_player.IsAlive) return;
            foreach (var other in _view.Relay.Touching)
            {
                if (other == null || other.gameObject.layer != Layers.Player) continue;
                _playerBody.TakeHit(new Damage(_definition.ContactDamage));
                break;
            }
        }

        void RequestSplit()
        {
            if (_splitTracker == null) return;
            int crossed = _splitTracker.Update(_health.Current, _health.Max);
            if (crossed <= 0) return;
            var split = _definition.Split;
            int perSplit = TierScaling.AddPerTier(split.ChildrenPerSplit, split.ExtraChildrenPerTier, Tier);
            SplitRequested?.Invoke(this, crossed * perSplit);
        }

        void Die()
        {
            if (_deathRaised) return;
            _deathRaised = true;
            _view.Freeze();
            ReleaseTelegraphs();
            _animator.Play(CharacterPose.Death);
            Died?.Invoke(this, new EnemyDeath(_definition, Position, _definition.XpReward, _definition.CoinReward));
        }

        // ---- IEnemyAttackPort ----

        public void ShowLine(int slot, Vector2 origin, Vector2 direction, float length, float width, float progress)
        {
            while (_lineViews.Count <= slot) _lineViews.Add(NewTelegraph());
            _lineViews[slot].ShowLine(origin, direction, length, width, progress);
            if (slot + 1 > _linesUsed) _linesUsed = slot + 1;
        }

        public void ShowCircle(Vector2 center, float radius, float progress)
        {
            if (_circleView == null) _circleView = NewTelegraph();
            _circleView.ShowCircle(center, radius, progress);
            _circleUsed = true;
        }

        public void Shoot(EnemyProjectileDefinition projectile, Vector2 direction) => _projectiles.Fire(projectile, Position, direction);

        public void SelfDestruct(float radius, int damage)
        {
            if (!IsAlive) return;
            if (_player.IsAlive && (_player.Position - Position).sqrMagnitude <= radius * radius)
            {
                _playerBody.TakeHit(new Damage(damage, DamageKind.Explosion));
            }
            _health.ApplyDamage(_health.Max);
            Die();
        }

        AttackTelegraphView NewTelegraph()
        {
            var view = UnityEngine.Object.Instantiate(_telegraphPrefab);
            view.Init();
            return view;
        }

        // その Tick に出さなかった予兆は消す
        void HideUnusedTelegraphs()
        {
            for (int i = _linesUsed; i < _lineViews.Count; i++) _lineViews[i].Hide();
            if (!_circleUsed && _circleView != null) _circleView.Hide();
        }

        static void DestroyView(AttackTelegraphView view)
        {
            if (view != null) UnityEngine.Object.Destroy(view.gameObject);
        }
    }
}

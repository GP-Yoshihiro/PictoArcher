using PictoArcher.Core;
using UnityEngine;

namespace PictoArcher.Game
{
    /// <summary>
    /// B さんの試し用の仮のプレイヤー（本物のプレイヤーは A さんの作業）。矢印キーで動き、敵に触れると HP が減る。
    /// 本番のコードからは使わない。
    /// </summary>
    public sealed class SandboxPlayer : ITarget, IHittable
    {
        readonly Rigidbody2D _body;
        readonly Health _health;
        readonly HitInvulnerability _invulnerability;
        readonly float _speed;
        float _now;

        public SandboxPlayer(Rigidbody2D body, int maxHp, float invulnerableSeconds, float speed)
        {
            _body = body;
            _health = new Health(maxHp);
            _invulnerability = new HitInvulnerability(invulnerableSeconds);
            _speed = speed;
        }

        public Vector2 Position => _body.position;
        public bool IsAlive => !_health.IsDead;
        public int Hp => _health.Current;

        public void Tick(float deltaTime, Vector2 input)
        {
            _now += deltaTime;
            _body.linearVelocity = IsAlive ? Vector2.ClampMagnitude(input, 1f) * _speed : Vector2.zero;
        }

        public void TakeHit(Damage damage)
        {
            if (!IsAlive || !_invulnerability.TryHit(_now)) return;
            _health.ApplyDamage(damage.Amount);
            Debug.Log($"SandboxPlayer HP {_health.Current}/{_health.Max}");
        }
    }
}

using UnityEngine;

namespace PictoArcher.Game
{
    /// <summary>
    /// 近づいて止まり、赤い線で予告してから弾を撃つ動き。弾の数が 1 なら単発攻撃型（Shooter）、3 以上で開きを付ければ扇型（SpreadShooter）。
    /// 速さは EnemyDefinition.MoveSpeed。
    /// </summary>
    [CreateAssetMenu(menuName = "PictoArcher/Enemy/ShooterBehavior")]
    public sealed class ShooterBehaviorDefinition : EnemyBehaviorDefinition
    {
        [SerializeField] EnemyProjectileDefinition _projectile;
        [SerializeField, Tooltip("この距離まで近づいたら止まって狙う（ユニット）")] float _attackRange = 6f;
        [SerializeField, Tooltip("撃ってから次の予兆が始まるまでの秒数")] float _interval = 2f;
        [SerializeField, Tooltip("赤い線を出してから撃つまでの秒数")] float _windupSeconds = 0.8f;
        [SerializeField, Tooltip("1回に撃つ弾の数。1 で単発、3 で三発扇型")] int _projectileCount = 1;
        [SerializeField, Tooltip("弾が複数のとき、端から端までの開き（度）")] float _spreadDegrees = 40f;
        [SerializeField, Tooltip("赤い線の長さ（ユニット）")] float _lineLength = 12f;
        [SerializeField, Tooltip("赤い線の太さ（ユニット）")] float _lineWidth = 0.3f;

        public EnemyProjectileDefinition Projectile => _projectile;
        public float AttackRange => _attackRange;
        public float Interval => _interval;
        public float WindupSeconds => _windupSeconds;
        public int ProjectileCount => _projectileCount;
        public float SpreadDegrees => _spreadDegrees;
        public float LineLength => _lineLength;
        public float LineWidth => _lineWidth;

        public override IEnemyBehavior Create() => new ShooterBehavior(this);
    }
}

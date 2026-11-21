using UnityEngine;

namespace PictoArcher.Game
{
    /// <summary>プレイヤーへ突進し、近づいたら止まって赤い円を広げ、広がりきったら自爆する動き（自爆型 Bomber）。速さは EnemyDefinition.MoveSpeed。</summary>
    [CreateAssetMenu(menuName = "PictoArcher/Enemy/BomberBehavior")]
    public sealed class BomberBehaviorDefinition : EnemyBehaviorDefinition
    {
        [SerializeField, Tooltip("この距離まで近づいたら止まって赤い円を出す（ユニット）")] float _triggerRange = 2.5f;
        [SerializeField, Tooltip("赤い円が広がりきるまでの秒数")] float _fuseSeconds = 1f;
        [SerializeField, Tooltip("爆発の半径（赤い円の最終の大きさ）")] float _blastRadius = 2.5f;
        [SerializeField] int _blastDamage = 20;

        public float TriggerRange => _triggerRange;
        public float FuseSeconds => _fuseSeconds;
        public float BlastRadius => _blastRadius;
        public int BlastDamage => _blastDamage;

        public override IEnemyBehavior Create() => new BomberBehavior(this);
    }
}

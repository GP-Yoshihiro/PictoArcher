using UnityEngine;

namespace PictoArcher.Game
{
    /// <summary>
    /// ボスA の動き：近づく → 赤い線で予告して突進 → 近づく → 赤い予兆つきで雑魚を呼ぶ → くり返す（設計書 §8 ボスA）。
    /// 近づく速さは EnemyDefinition.MoveSpeed。
    /// </summary>
    [CreateAssetMenu(menuName = "PictoArcher/Enemy/ChargeSummonBehavior")]
    public sealed class ChargeSummonBehaviorDefinition : EnemyBehaviorDefinition
    {
        [SerializeField, Tooltip("攻撃と攻撃の間に近づく秒数")] float _approachSeconds = 1.5f;
        [SerializeField, Tooltip("突進の前に赤い線を出して止まる秒数")] float _chargeWindupSeconds = 0.8f;
        [SerializeField, Tooltip("突進の速さ（ユニット/秒）")] float _chargeSpeed = 14f;
        [SerializeField, Tooltip("突進する距離（赤い線の長さ）")] float _chargeDistance = 9f;
        [SerializeField, Tooltip("赤い線の太さ")] float _chargeLineWidth = 2f;
        [SerializeField, Tooltip("呼ぶ雑魚の予兆の秒数。その間ボスは止まる")] float _summonWindupSeconds = 1f;
        [SerializeField, Tooltip("呼ぶ雑魚")] EnemyDefinition _summonEnemy;
        [SerializeField, Tooltip("1回に呼ぶ数")] int _summonCount = 3;
        [SerializeField, Tooltip("ボスから雑魚を出す位置までの距離")] float _summonDistance = 3f;
        [SerializeField, Tooltip("雑魚を出してよい範囲（床の内側）")] Rect _summonArea = new Rect(-6f, -10.5f, 12f, 21f);

        public float ApproachSeconds => _approachSeconds;
        public float ChargeWindupSeconds => _chargeWindupSeconds;
        public float ChargeSpeed => _chargeSpeed;
        public float ChargeDistance => _chargeDistance;
        public float ChargeLineWidth => _chargeLineWidth;
        public float SummonWindupSeconds => _summonWindupSeconds;
        public EnemyDefinition SummonEnemy => _summonEnemy;
        public int SummonCount => _summonCount;
        public float SummonDistance => _summonDistance;
        public Rect SummonArea => _summonArea;

        public override IEnemyBehavior Create() => new ChargeSummonBehavior(this);
    }
}

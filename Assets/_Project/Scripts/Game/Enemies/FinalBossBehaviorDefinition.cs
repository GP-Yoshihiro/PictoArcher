using UnityEngine;

namespace PictoArcher.Game
{
    /// <summary>
    /// 最後のボスの動き：近づく → 突進（赤い線）→ 近づく → 弾幕（光りながら赤い線を全方向に出してから撃つ）→ 近づく → 雑魚を呼ぶ → くり返す（設計書 §8 ボス）。
    /// HP が半分を切ると、体の色と光り方（色と点滅の速さ）が変わり、攻撃の間に近づく秒数が短くなり、弾幕の弾が増える。
    /// 近づく速さは EnemyDefinition.MoveSpeed。色に赤・緑・黄・青・紫は使わない。
    /// </summary>
    [CreateAssetMenu(menuName = "PictoArcher/Enemy/FinalBossBehavior")]
    public sealed class FinalBossBehaviorDefinition : EnemyBehaviorDefinition
    {
        [Header("間と変化")]
        [SerializeField, Tooltip("攻撃と攻撃の間に近づく秒数")] float _approachSeconds = 1.2f;
        [SerializeField, Tooltip("HP がこの割合を切ったら変わる")] float _enrageHpRatio = 0.5f;
        [SerializeField, Tooltip("変わったあと、近づく秒数にかける数（1 より小さい）")] float _enragedIntervalScale = 0.6f;

        [Header("突進")]
        [SerializeField, Tooltip("突進の前に赤い線を出して止まる秒数")] float _chargeWindupSeconds = 0.8f;
        [SerializeField, Tooltip("突進の速さ（ユニット/秒）")] float _chargeSpeed = 16f;
        [SerializeField, Tooltip("突進する距離（赤い線の長さ）")] float _chargeDistance = 10f;
        [SerializeField, Tooltip("赤い線の太さ")] float _chargeLineWidth = 2.5f;

        [Header("弾幕")]
        [SerializeField] EnemyProjectileDefinition _projectile;
        [SerializeField, Tooltip("光りながら赤い線を出して止まる秒数")] float _barrageWindupSeconds = 1f;
        [SerializeField, Tooltip("全方向に撃つ弾の数")] int _barrageCount = 12;
        [SerializeField, Tooltip("変わったあとの弾の数")] int _enragedBarrageCount = 18;
        [SerializeField, Tooltip("赤い線の長さ")] float _barrageLineLength = 6f;
        [SerializeField, Tooltip("赤い線の太さ")] float _barrageLineWidth = 0.3f;

        [Header("雑魚を呼ぶ")]
        [SerializeField, Tooltip("呼ぶ雑魚の予兆の秒数。その間ボスは止まる")] float _summonWindupSeconds = 1f;
        [SerializeField, Tooltip("呼ぶ雑魚")] EnemyDefinition _summonEnemy;
        [SerializeField, Tooltip("1回に呼ぶ数")] int _summonCount = 4;
        [SerializeField, Tooltip("ボスから雑魚を出す位置までの距離")] float _summonDistance = 3f;
        [SerializeField, Tooltip("雑魚を出してよい範囲（床の内側）")] Rect _summonArea = new Rect(-6f, -10.5f, 12f, 21f);

        [Header("色と光")]
        [SerializeField, Tooltip("はじめの体の色（かける色）")] Color _tint = new Color(0.8f, 0.8f, 0.8f, 1f);
        [SerializeField, Tooltip("変わったあとの体の色")] Color _enragedTint = new Color(0.5f, 0.42f, 0.42f, 1f);
        [SerializeField, Tooltip("弾幕の前に光る色")] Color _glowColor = Color.white;
        [SerializeField, Tooltip("変わったあとに光る色（桃）")] Color _enragedGlowColor = new Color(1f, 0.6f, 0.8f, 1f);
        [SerializeField, Tooltip("1秒に点滅する回数")] float _glowFlashesPerSecond = 2f;
        [SerializeField, Tooltip("変わったあとの点滅の回数")] float _enragedGlowFlashesPerSecond = 6f;

        public float ApproachSeconds => _approachSeconds;
        public float EnrageHpRatio => _enrageHpRatio;
        public float EnragedIntervalScale => _enragedIntervalScale;
        public float ChargeWindupSeconds => _chargeWindupSeconds;
        public float ChargeSpeed => _chargeSpeed;
        public float ChargeDistance => _chargeDistance;
        public float ChargeLineWidth => _chargeLineWidth;
        public EnemyProjectileDefinition Projectile => _projectile;
        public float BarrageWindupSeconds => _barrageWindupSeconds;
        public int BarrageCount => _barrageCount;
        public int EnragedBarrageCount => _enragedBarrageCount;
        public float BarrageLineLength => _barrageLineLength;
        public float BarrageLineWidth => _barrageLineWidth;
        public float SummonWindupSeconds => _summonWindupSeconds;
        public EnemyDefinition SummonEnemy => _summonEnemy;
        public int SummonCount => _summonCount;
        public float SummonDistance => _summonDistance;
        public Rect SummonArea => _summonArea;
        public Color Tint => _tint;
        public Color EnragedTint => _enragedTint;
        public Color GlowColor => _glowColor;
        public Color EnragedGlowColor => _enragedGlowColor;
        public float GlowFlashesPerSecond => _glowFlashesPerSecond;
        public float EnragedGlowFlashesPerSecond => _enragedGlowFlashesPerSecond;

        public override IEnemyBehavior Create() => new FinalBossBehavior(this);
    }
}

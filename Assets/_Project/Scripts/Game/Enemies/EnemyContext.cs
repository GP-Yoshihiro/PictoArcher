using UnityEngine;

namespace PictoArcher.Game
{
    /// <summary>敵の動く部品が Tick で受け取る、いまの状況。</summary>
    public readonly struct EnemyContext
    {
        public EnemyContext(Vector2 position, Vector2 targetPosition, float moveSpeed, float hpRatio = 1f)
            : this(position, targetPosition, targetPosition, moveSpeed, hpRatio) { }

        public EnemyContext(Vector2 position, Vector2 targetPosition, Vector2 chasePoint, float moveSpeed, float hpRatio = 1f)
        {
            Position = position;
            TargetPosition = targetPosition;
            ChasePoint = chasePoint;
            MoveSpeed = moveSpeed;
            HpRatio = hpRatio;
        }

        /// <summary>この敵の足元の位置。</summary>
        public Vector2 Position { get; }
        /// <summary>狙う相手（プレイヤー）の足元の位置。</summary>
        public Vector2 TargetPosition { get; }
        /// <summary>追うときに歩いて向かう点。障害物があれば回り込む道の途中の点、なければ TargetPosition。狙う向きには使わない。</summary>
        public Vector2 ChasePoint { get; }
        /// <summary>EnemyDefinition.MoveSpeed。</summary>
        public float MoveSpeed { get; }
        /// <summary>今の HP ÷ 最大の HP（1 が満タン）。ボスが半分を切ったら変わるときに使う。</summary>
        public float HpRatio { get; }
    }
}

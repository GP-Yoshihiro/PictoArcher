using UnityEngine;

namespace PictoArcher.Game
{
    /// <summary>敵の動く部品が Tick で受け取る、いまの状況。</summary>
    public readonly struct EnemyContext
    {
        public EnemyContext(Vector2 position, Vector2 targetPosition, float moveSpeed)
        {
            Position = position;
            TargetPosition = targetPosition;
            MoveSpeed = moveSpeed;
        }

        /// <summary>この敵の足元の位置。</summary>
        public Vector2 Position { get; }
        /// <summary>狙う相手（プレイヤー）の足元の位置。</summary>
        public Vector2 TargetPosition { get; }
        /// <summary>EnemyDefinition.MoveSpeed。</summary>
        public float MoveSpeed { get; }
    }
}

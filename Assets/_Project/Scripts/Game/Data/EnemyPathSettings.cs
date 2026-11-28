using UnityEngine;

namespace PictoArcher.Game
{
    /// <summary>敵が障害物を回り込む道の調整値（歩ける広さ、マスの大きさ、体のまわりの余白、先読みのマス数）。</summary>
    [CreateAssetMenu(menuName = "PictoArcher/EnemyPathSettings")]
    public sealed class EnemyPathSettings : ScriptableObject
    {
        [SerializeField, Tooltip("敵が歩ける広さ（壁の内側。ワールド座標）")] Rect _area = new Rect(-7.5f, -14f, 15f, 25f);
        [SerializeField, Min(0.1f), Tooltip("道を探すマスの1辺（ユニット）。小さいほど細かいが重い")] float _cellSize = 0.5f;
        [SerializeField, Min(0f), Tooltip("体の半径に足す余白（ユニット）。角にこすらないため")] float _clearanceMargin = 0.1f;
        [SerializeField, Min(1), Tooltip("道を何マス先まで見通して近道するか")] int _lookAheadCells = 16;

        public Rect Area => _area;
        public float CellSize => _cellSize;
        public float ClearanceMargin => _clearanceMargin;
        public int LookAheadCells => _lookAheadCells;
    }
}

using UnityEngine;

namespace PictoArcher.Game
{
    /// <summary>障害物の切り替えの調整値（影を出す秒数、影の色、プレイヤーと重なるかの判定の半径）。</summary>
    [CreateAssetMenu(menuName = "PictoArcher/ObstacleSwitchSettings")]
    public sealed class ObstacleSwitchSettings : ScriptableObject
    {
        [SerializeField, Min(0f), Tooltip("影を出す秒数")] float _shadowSeconds = 0.5f;
        [SerializeField, Tooltip("影の色。黒の半透明（意味の5色は使わない）")] Color _shadowColor = new Color(0f, 0f, 0f, 0.35f);
        [SerializeField, Min(0f), Tooltip("プレイヤーと重なるかの判定に使う体の半径（ユニット）")] float _playerRadius = 0.6f;

        public float ShadowSeconds => _shadowSeconds;
        public Color ShadowColor => _shadowColor;
        public float PlayerRadius => _playerRadius;
    }
}

using UnityEngine;
using UnityEngine.Serialization;

namespace PictoArcher.Game
{
    /// <summary>プレイヤーの調整値。撃つ間隔・威力・弾の速さは武器の定義（WeaponDefinition）が持つ。</summary>
    [CreateAssetMenu(menuName = "PictoArcher/PlayerStats")]
    public sealed class PlayerStats : ScriptableObject
    {
        [SerializeField, FormerlySerializedAs("maxHp")] int _maxHp = 100;
        [SerializeField, FormerlySerializedAs("moveSpeed")] float _moveSpeed = 6f;
        [SerializeField, FormerlySerializedAs("moveFireRateMultiplier")] float _moveFireRateMultiplier = 1f;
        [SerializeField, FormerlySerializedAs("invulnerabilitySeconds")] float _invulnerabilitySeconds = 0.6f;
        [SerializeField, FormerlySerializedAs("dragRadiusPixels")] float _dragRadiusPixels = 80f;

        public int MaxHp => _maxHp;
        public float MoveSpeed => _moveSpeed;
        public float MoveFireRateMultiplier => _moveFireRateMultiplier;
        public float InvulnerabilitySeconds => _invulnerabilitySeconds;
        public float DragRadiusPixels => _dragRadiusPixels;
    }
}

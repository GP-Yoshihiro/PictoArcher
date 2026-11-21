using UnityEngine;
using UnityEngine.Serialization;

namespace PictoArcher.Game
{
    /// <summary>敵1種類の調整値。</summary>
    [CreateAssetMenu(menuName = "PictoArcher/EnemyDefinition")]
    public sealed class EnemyDefinition : ScriptableObject
    {
        [SerializeField, FormerlySerializedAs("prefab")] GameObject _prefab;
        [SerializeField, FormerlySerializedAs("maxHp")] int _maxHp;
        [SerializeField, FormerlySerializedAs("moveSpeed")] float _moveSpeed;
        [SerializeField, FormerlySerializedAs("contactDamage")] int _contactDamage;
        [SerializeField, FormerlySerializedAs("xpReward")] int _xpReward;
        [SerializeField, FormerlySerializedAs("coinReward")] int _coinReward;
        [SerializeField, Tooltip("見た目の表（Data/Visuals/Characters）。同じモデルの敵は同じ表を指してよい")] CharacterVisual _visual;

        [SerializeField, Tooltip("動きと攻撃の種類（Data/Enemies の ...Behavior アセット）")] EnemyBehaviorDefinition _behavior;
        public EnemyBehaviorDefinition Behavior => _behavior;
        public GameObject Prefab => _prefab;
        public int MaxHp => _maxHp;
        public float MoveSpeed => _moveSpeed;
        public int ContactDamage => _contactDamage;
        public int XpReward => _xpReward;
        public int CoinReward => _coinReward;
        public CharacterVisual Visual => _visual;
    }
}

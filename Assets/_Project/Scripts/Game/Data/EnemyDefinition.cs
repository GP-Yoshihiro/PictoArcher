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
        [SerializeField, Tooltip("HP が減ると分裂する敵だけ入れる（中ボス）。空なら分裂しない")] SplitDefinition _split;
        [SerializeField, Tooltip("登場1回ごとに HP が増える割合（0.5 で、2回目は 1.5 倍）。中ボス・ボスだけ使う")] float _hpGrowthPerTier;
        [SerializeField, Tooltip("登場1回ごとに速さが増える割合")] float _speedGrowthPerTier;
        [SerializeField, Tooltip("出る前の予兆を、ふつうの予兆の代わりにこの絵にする（中ボスの ⚠ など）。空ならふつうの予兆")] SpawnTelegraphView _spawnTelegraph;
        public EnemyBehaviorDefinition Behavior => _behavior;
        public SplitDefinition Split => _split;
        public float HpGrowthPerTier => _hpGrowthPerTier;
        public float SpeedGrowthPerTier => _speedGrowthPerTier;
        public SpawnTelegraphView SpawnTelegraph => _spawnTelegraph;
        public GameObject Prefab => _prefab;
        public int MaxHp => _maxHp;
        public float MoveSpeed => _moveSpeed;
        public int ContactDamage => _contactDamage;
        public int XpReward => _xpReward;
        public int CoinReward => _coinReward;
        public CharacterVisual Visual => _visual;
    }
}

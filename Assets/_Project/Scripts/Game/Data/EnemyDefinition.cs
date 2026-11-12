using UnityEngine;

namespace PictoArcher.Game
{
    [CreateAssetMenu(menuName = "PictoArcher/EnemyDefinition")]
    public class EnemyDefinition : ScriptableObject
    {
        public GameObject prefab;
        public int maxHp;
        public float moveSpeed;
        public int contactDamage;
        public int xpReward;
        public int coinReward;
    }
}

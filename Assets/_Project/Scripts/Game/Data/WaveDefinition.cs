using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace PictoArcher.Game
{
    /// <summary>1ウェーブの出現の並び。</summary>
    [CreateAssetMenu(menuName = "PictoArcher/WaveDefinition")]
    public sealed class WaveDefinition : ScriptableObject
    {
        [SerializeField, FormerlySerializedAs("spawns")] List<SpawnEntry> _spawns = new List<SpawnEntry>();
        [SerializeField, FormerlySerializedAs("telegraphSeconds")] float _telegraphSeconds = 0.8f;
        [SerializeField, Tooltip("このウェーブの障害物の並び。空なら前のウェーブのまま")] ObstacleLayout _obstacleLayout;
        [SerializeField, Min(0), Tooltip("強さの段階（0 が最初）。出す敵に渡し、中ボス・ボスは段階ごとに HP と速さが上がる")] int _tier;

        public IReadOnlyList<SpawnEntry> Spawns => _spawns;
        public float TelegraphSeconds => _telegraphSeconds;
        public ObstacleLayout ObstacleLayout => _obstacleLayout;
        public int Tier => _tier;
    }
}

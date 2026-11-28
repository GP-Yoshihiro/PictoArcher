using System.Collections.Generic;
using UnityEngine;

namespace PictoArcher.Game
{
    /// <summary>障害物の並び1パターン。WaveDefinition から指定し、ウェーブの休みに切り替える（設計書 §6）。</summary>
    [CreateAssetMenu(menuName = "PictoArcher/ObstacleLayout")]
    public sealed class ObstacleLayout : ScriptableObject
    {
        [SerializeField] List<ObstaclePlacement> _placements = new List<ObstaclePlacement>();
        [SerializeField, Tooltip("ボスと中ボスのウェーブ向け（障害物が少ない）")] bool _forBossWaves;

        public IReadOnlyList<ObstaclePlacement> Placements => _placements;
        public bool ForBossWaves => _forBossWaves;
    }
}

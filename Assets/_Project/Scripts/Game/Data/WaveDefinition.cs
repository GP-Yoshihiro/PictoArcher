using System;
using System.Collections.Generic;
using UnityEngine;

namespace PictoArcher.Game
{
    [Serializable]
    public struct SpawnEntry
    {
        public EnemyDefinition enemy;
        public Vector2 position;
    }

    [CreateAssetMenu(menuName = "PictoArcher/WaveDefinition")]
    public class WaveDefinition : ScriptableObject
    {
        public List<SpawnEntry> spawns = new List<SpawnEntry>();
        public float telegraphSeconds = 0.8f;
    }
}

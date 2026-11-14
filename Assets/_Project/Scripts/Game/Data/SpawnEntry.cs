using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace PictoArcher.Game
{
    /// <summary>どの敵をどこに出すか（WaveDefinition の1行）。</summary>
    [Serializable]
    public struct SpawnEntry
    {
        [SerializeField, FormerlySerializedAs("enemy")] EnemyDefinition _enemy;
        [SerializeField, FormerlySerializedAs("position")] Vector2 _position;

        public SpawnEntry(EnemyDefinition enemy, Vector2 position)
        {
            _enemy = enemy;
            _position = position;
        }

        public EnemyDefinition Enemy => _enemy;
        public Vector2 Position => _position;
    }
}

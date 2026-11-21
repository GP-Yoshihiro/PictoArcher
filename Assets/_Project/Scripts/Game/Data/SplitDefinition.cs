using System.Collections.Generic;
using UnityEngine;

namespace PictoArcher.Game
{
    /// <summary>「HP が減ると分裂する」敵（中ボス Splitter）の調整値。EnemyDefinition が指す。</summary>
    [CreateAssetMenu(menuName = "PictoArcher/SplitDefinition")]
    public sealed class SplitDefinition : ScriptableObject
    {
        [SerializeField, Tooltip("分裂で出る敵（ふつうの敵の定義。分裂では出ない種類にしておく）")] EnemyDefinition _child;
        [SerializeField, Tooltip("HP がこの割合（0〜1）を切るたびに分裂する。例：0.66 と 0.33 で2回")] List<float> _hpThresholds = new List<float> { 0.66f, 0.33f };
        [SerializeField, Tooltip("1回の分裂で出る数")] int _childrenPerSplit = 2;
        [SerializeField, Tooltip("登場1回ごとに増える、1回の分裂で出る数")] int _extraChildrenPerTier = 1;
        [SerializeField, Tooltip("分裂で出た敵を置く、本体の足元からの距離（ユニット）")] float _spawnRadius = 1.5f;

        public EnemyDefinition Child => _child;
        public IReadOnlyList<float> HpThresholds => _hpThresholds;
        public int ChildrenPerSplit => _childrenPerSplit;
        public int ExtraChildrenPerTier => _extraChildrenPerTier;
        public float SpawnRadius => _spawnRadius;
    }
}

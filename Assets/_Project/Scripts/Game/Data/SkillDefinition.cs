using PictoArcher.Core;
using UnityEngine;
using UnityEngine.Serialization;

namespace PictoArcher.Game
{
    /// <summary>強化カード1枚の調整値（設計書 §9）。重ね取りの上限 0 は上限なし、重み 0 は抽選に出ない。</summary>
    [CreateAssetMenu(menuName = "PictoArcher/SkillDefinition")]
    public sealed class SkillDefinition : ScriptableObject
    {
        [SerializeField, FormerlySerializedAs("id")] SkillId _id;
        [SerializeField, FormerlySerializedAs("icon")] Sprite _icon;
        [SerializeField, FormerlySerializedAs("maxStacks"), Min(0), Tooltip("重ね取りの上限。0 は上限なし")] int _maxStacks;
        [SerializeField, Min(0), Tooltip("抽選の重み。0 は出ない")] int _weight = 10;

        public SkillId Id => _id;
        public Sprite Icon => _icon;
        public int MaxStacks => _maxStacks;
        public int Weight => _weight;

        public SkillCandidate ToCandidate() => new SkillCandidate((int)_id, _maxStacks, _weight);
    }
}

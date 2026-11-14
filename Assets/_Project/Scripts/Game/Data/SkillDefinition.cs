using UnityEngine;
using UnityEngine.Serialization;

namespace PictoArcher.Game
{
    /// <summary>スキル1種類の調整値。</summary>
    [CreateAssetMenu(menuName = "PictoArcher/SkillDefinition")]
    public sealed class SkillDefinition : ScriptableObject
    {
        [SerializeField, FormerlySerializedAs("id")] SkillId _id;
        [SerializeField, FormerlySerializedAs("icon")] Sprite _icon;
        [SerializeField, FormerlySerializedAs("maxStacks")] int _maxStacks;

        public SkillId Id => _id;
        public Sprite Icon => _icon;
        public int MaxStacks => _maxStacks;
    }
}

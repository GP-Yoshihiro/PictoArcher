using UnityEngine;

namespace PictoArcher.Game
{
    [CreateAssetMenu(menuName = "PictoArcher/SkillDefinition")]
    public class SkillDefinition : ScriptableObject
    {
        public SkillId id;
        public Sprite icon;
        public int maxStacks;
    }
}

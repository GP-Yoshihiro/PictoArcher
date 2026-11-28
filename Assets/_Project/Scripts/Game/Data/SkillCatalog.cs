using System.Collections.Generic;
using PictoArcher.Core;
using UnityEngine;

namespace PictoArcher.Game
{
    /// <summary>強化カード26枚の一覧（設計書 §9）。並びは SkillId と同じ。</summary>
    [CreateAssetMenu(menuName = "PictoArcher/SkillCatalog")]
    public sealed class SkillCatalog : ScriptableObject
    {
        [SerializeField] SkillDefinition[] _skills = new SkillDefinition[0];

        public IReadOnlyList<SkillDefinition> Skills => _skills;

        /// <summary>id の定義。ないなら null。</summary>
        public SkillDefinition DefinitionOf(SkillId id)
        {
            for (int i = 0; i < _skills.Length; i++)
            {
                if (_skills[i] != null && _skills[i].Id == id) return _skills[i];
            }
            return null;
        }

        /// <summary>抽選に渡す候補を作る（空の枠と、excluded にあるもの（武器に合わない強化）は飛ばす）。</summary>
        public SkillCandidate[] BuildCandidates(IReadOnlyList<SkillId> excluded = null)
        {
            var list = new List<SkillCandidate>(_skills.Length);
            for (int i = 0; i < _skills.Length; i++)
            {
                if (_skills[i] != null && !IsExcluded(_skills[i].Id, excluded)) list.Add(_skills[i].ToCandidate());
            }
            return list.ToArray();
        }

        static bool IsExcluded(SkillId id, IReadOnlyList<SkillId> excluded)
        {
            if (excluded == null) return false;
            for (int i = 0; i < excluded.Count; i++)
            {
                if (excluded[i] == id) return true;
            }
            return false;
        }
    }
}

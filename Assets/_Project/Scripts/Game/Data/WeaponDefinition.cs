using System.Collections.Generic;
using UnityEngine;

namespace PictoArcher.Game
{
    /// <summary>
    /// 武器の定義の親（設計書 §4 武器）。撃つ間隔・威力と、その武器では候補に出さない強化を持つ。
    /// 撃ち方は種類ごとの定義（銃・レーザー・ランチャー）が Create で作る IWeapon が決める。
    /// </summary>
    public abstract class WeaponDefinition : DefinitionAsset<IWeapon>
    {
        [SerializeField] WeaponId _id;
        [SerializeField, Tooltip("撃つ間隔（秒）")] float _fireInterval = 0.5f;
        [SerializeField, Tooltip("1回の威力")] int _damage = 10;
        [SerializeField, Tooltip("この武器では強化の候補に出さないもの（レーザーの貫通など）")] SkillId[] _excludedSkills = new SkillId[0];

        public WeaponId Id => _id;
        public float FireInterval => _fireInterval;
        public int Damage => _damage;
        public IReadOnlyList<SkillId> ExcludedSkills => _excludedSkills;
    }
}

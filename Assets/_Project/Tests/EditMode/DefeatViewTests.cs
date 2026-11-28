using NUnit.Framework;
using PictoArcher.Game;
using UnityEditor;
using UnityEngine;

namespace PictoArcher.Tests.EditMode
{
    public class DefeatViewTests
    {
        Sprite _sprite;
        SkillDefinition _fire;
        SkillDefinition _noIcon;

        static SkillDefinition Skill(SkillId id, Sprite icon)
        {
            var skill = ScriptableObject.CreateInstance<SkillDefinition>();
            var so = new SerializedObject(skill);
            so.FindProperty("_id").enumValueIndex = (int)id;
            so.FindProperty("_icon").objectReferenceValue = icon;
            so.ApplyModifiedPropertiesWithoutUndo();
            return skill;
        }

        [SetUp]
        public void SetUp()
        {
            _sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), Vector2.one * 0.5f);
            _fire = Skill(SkillId.Fire, _sprite);
            _noIcon = Skill(SkillId.Ice, null);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_fire);
            Object.DestroyImmediate(_noIcon);
            Object.DestroyImmediate(_sprite);
        }

        [Test] public void Found_ReturnsIcon() => Assert.AreSame(_sprite, DefeatView.FindSkillIcon(new[] { _noIcon, _fire }, SkillId.Fire));
        [Test] public void MissingId_ReturnsNull() => Assert.IsNull(DefeatView.FindSkillIcon(new[] { _fire }, SkillId.AttackUp));
        [Test] public void NullIcon_ReturnsNull() => Assert.IsNull(DefeatView.FindSkillIcon(new[] { _noIcon }, SkillId.Ice));
        [Test] public void NullArray_ReturnsNull() => Assert.IsNull(DefeatView.FindSkillIcon(null, SkillId.Fire));
        [Test] public void NullEntry_IsSkipped() => Assert.AreSame(_sprite, DefeatView.FindSkillIcon(new[] { null, _fire }, SkillId.Fire));
    }
}

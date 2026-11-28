using System.Collections;
using NUnit.Framework;
using PictoArcher.Game;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace PictoArcher.Tests.PlayMode
{
    public class HudStatusPlayModeTests
    {
        GameObject _canvas;
        HudStatusView _view;
        UiIconVisual _icons;
        SkillCatalog _skills;

        [SetUp]
        public void SetUp()
        {
            _canvas = new GameObject("Canvas", typeof(Canvas));
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/UI/HudStatus.prefab");
            _view = Object.Instantiate(prefab, _canvas.transform).GetComponent<HudStatusView>();
            _icons = AssetDatabase.LoadAssetAtPath<UiIconVisual>("Assets/_Project/Data/Visuals/UiIconVisual.asset");
            _skills = AssetDatabase.LoadAssetAtPath<SkillCatalog>("Assets/_Project/Data/Skills/SkillCatalog.asset");
            _view.Init(_icons, _icons.WeaponIconOf(WeaponId.Gun));
        }

        [TearDown]
        public void TearDown() => Object.Destroy(_canvas);

        Transform Child(string path) => _view.transform.Find(path);
        string TextOf(string path) => Child(path).GetComponent<TMP_Text>().text;

        [UnityTest]
        public IEnumerator Values_AppearOnTheHud()
        {
            _view.SetLevel(5);
            _view.SetCoins(23);
            _view.SetExperience(3, 12);
            yield return null;
            Assert.AreEqual("5", TextOf("Level"));
            Assert.AreEqual("23", TextOf("Coins"));
            Assert.AreEqual(0.25f, ((RectTransform)Child("ExperienceTrack/Fill")).anchorMax.x, 0.001f);
            Assert.IsNotNull(Child("LevelIcon").GetComponent<Image>().sprite, "★は絵");
            Assert.IsNotNull(Child("CoinIcon").GetComponent<Image>().sprite, "コインは絵");
        }

        [UnityTest]
        public IEnumerator ExperienceBar_StaysInsideTheTrack()
        {
            _view.SetExperience(50, 12);
            yield return null;
            Assert.AreEqual(1f, ((RectTransform)Child("ExperienceTrack/Fill")).anchorMax.x, 0.001f);
            _view.SetExperience(0, 12);
            yield return null;
            Assert.AreEqual(0f, ((RectTransform)Child("ExperienceTrack/Fill")).anchorMax.x, 0.001f);
        }

        [UnityTest]
        public IEnumerator Slots_WeaponFirstThenSkillsWithStackCount()
        {
            Assert.AreEqual(1, _view.SlotCount, "武器の絵が先頭に1つ");
            var skill = _skills.Skills[0];
            _view.SetSkill(0, skill.Id, skill.Icon, 1);
            _view.SetSkill(1, _skills.Skills[1].Id, _skills.Skills[1].Icon, 3);
            yield return null;
            Assert.AreEqual(3, _view.SlotCount);
            var slots = Child("Slots");
            Assert.AreEqual(skill.Icon, slots.GetChild(1).GetComponentInChildren<Image>().sprite);
            Assert.IsFalse(slots.GetChild(1).Find("Count").gameObject.activeSelf, "1つのときは ×n を出さない");
            Assert.AreEqual("×3", slots.GetChild(2).Find("Count").GetComponent<TMP_Text>().text);
            _view.SetSkill(0, skill.Id, skill.Icon, 2);
            Assert.AreEqual("×2", slots.GetChild(1).Find("Count").GetComponent<TMP_Text>().text, "重ね取りで ×2 になる");
        }

        [UnityTest]
        public IEnumerator Hud_HasNoLettersOnlyPicturesAndDigits()
        {
            yield return null;
            foreach (var graphic in _view.GetComponentsInChildren<Graphic>(true))
            {
                Assert.IsTrue(graphic is Image || graphic is TMP_Text, graphic.name);
            }
            foreach (var text in _view.GetComponentsInChildren<TMP_Text>(true))
            {
                foreach (char c in text.text) Assert.IsTrue("0123456789+-×/%".IndexOf(c) >= 0, text.name + " に数字と記号以外 " + c);
            }
        }
    }
}

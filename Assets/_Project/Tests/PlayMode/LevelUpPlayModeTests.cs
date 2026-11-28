using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using PictoArcher.Game;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace PictoArcher.Tests.PlayMode
{
    public class LevelUpPlayModeTests
    {
        sealed class FakePlayer : IPlayerStatus
        {
            public int Hp { get; set; } = 100;
            public int MaxHp { get; set; } = 100;
            public bool IsDead { get; set; }
            public bool IsInvulnerable => false;
        }

        GameObject _canvas;
        CardSelectView _view;
        FakePlayer _player;
        LevelUpSettings _settings;
        LevelUpSystem _system;
        readonly List<(SkillId, int)> _taken = new List<(SkillId, int)>();

        [SetUp]
        public void SetUp()
        {
            _canvas = new GameObject("Canvas", typeof(Canvas));
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/UI/CardSelect.prefab");
            _view = Object.Instantiate(prefab, _canvas.transform).GetComponent<CardSelectView>();
            _settings = AssetDatabase.LoadAssetAtPath<LevelUpSettings>("Assets/_Project/Data/Player/LevelUpSettings.asset");
            var catalog = AssetDatabase.LoadAssetAtPath<SkillCatalog>("Assets/_Project/Data/Skills/SkillCatalog.asset");
            _player = new FakePlayer();
            _taken.Clear();
            _system = new LevelUpSystem(_settings, catalog, _view, _player, new System.Random(1));
            _system.SkillTaken += (id, stack) => _taken.Add((id, stack));
        }

        [TearDown]
        public void TearDown()
        {
            _system.Dispose();
            Object.Destroy(_canvas);
        }

        GameObject Panel => _view.transform.Find("Panel").gameObject;
        Button Card(int i) => _view.transform.Find("Panel/Card" + i).GetComponent<Button>();

        [UnityTest]
        public IEnumerator ReachingTheNextLevel_ShowsThreeCardsWithIcons()
        {
            Assert.IsFalse(Panel.activeSelf, "始めは隠れている");
            _system.Collect(PickupKind.Experience, _settings.FirstLevelExperience);
            _system.Tick(0f);
            yield return null;
            Assert.IsTrue(_system.IsChoosing);
            Assert.IsTrue(Panel.activeSelf);
            Assert.AreEqual(2, _system.Level);
            for (int i = 0; i < 3; i++)
            {
                Assert.IsTrue(Card(i).gameObject.activeSelf);
                Assert.IsNotNull(Card(i).transform.Find("Icon").GetComponent<Image>().sprite);
            }
        }

        [UnityTest]
        public IEnumerator ClickingACard_TakesTheSkillAndCloses()
        {
            _system.Collect(PickupKind.Experience, _settings.FirstLevelExperience);
            _system.Tick(0f);
            Card(1).onClick.Invoke();
            _system.Tick(0f);
            yield return null;
            Assert.IsFalse(_system.IsChoosing);
            Assert.IsFalse(Panel.activeSelf);
            Assert.AreEqual(1, _taken.Count);
            Assert.AreEqual(1, _taken[0].Item2, "1段目");
            CollectionAssert.AreEqual(new[] { _taken[0].Item1 }, _system.TakenSkills);
            Assert.AreNotEqual(SkillId.Heal, _taken[0].Item1, "HP が満タンなら回復は出ない");
        }

        [UnityTest]
        public IEnumerator TwoLevelsAtOnce_OpensTwice()
        {
            _system.Collect(PickupKind.Experience, _settings.FirstLevelExperience * 2 + _settings.ExperiencePerLevel);
            _system.Tick(0f);
            Card(0).onClick.Invoke();
            _system.Tick(0f);
            Assert.IsTrue(_system.IsChoosing, "残りの1回ぶんがすぐ開く");
            Card(2).onClick.Invoke();
            _system.Tick(0f);
            yield return null;
            Assert.IsFalse(_system.IsChoosing);
            Assert.AreEqual(2, _taken.Count);
        }

        [UnityTest]
        public IEnumerator DeadPlayer_DoesNotOpen()
        {
            _player.IsDead = true;
            _system.Collect(PickupKind.Experience, _settings.FirstLevelExperience);
            _system.Tick(0f);
            yield return null;
            Assert.IsFalse(_system.IsChoosing);
            Assert.IsFalse(Panel.activeSelf);
        }

        [UnityTest]
        public IEnumerator Coins_AreCounted()
        {
            _system.Collect(PickupKind.Coin, 5);
            yield return null;
            Assert.AreEqual(5, _system.Coins);
            Assert.IsFalse(_system.IsChoosing);
        }
    }
}

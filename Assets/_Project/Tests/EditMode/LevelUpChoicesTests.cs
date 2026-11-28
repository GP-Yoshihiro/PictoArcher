using System;
using System.Collections.Generic;
using NUnit.Framework;
using PictoArcher.Core;

namespace PictoArcher.Tests.EditMode
{
    public class LevelUpChoicesTests
    {
        const int Heal = 0;

        static SkillDeck Deck(params SkillCandidate[] candidates) => new SkillDeck(candidates, new Random(1));

        // 回復（上限なし）＋上限つき3つ
        static SkillDeck FourCards() => Deck(
            new SkillCandidate(Heal, 0, 10),
            new SkillCandidate(1, 2, 10),
            new SkillCandidate(2, 1, 10),
            new SkillCandidate(3, 3, 10));

        static int[] SkillIds(IReadOnlyList<LevelUpCard> cards)
        {
            var ids = new List<int>();
            for (int i = 0; i < cards.Count; i++) if (!cards[i].IsCoin) ids.Add(cards[i].SkillId);
            return ids.ToArray();
        }

        [Test]
        public void TryOpen_WithoutPendingLevel_DoesNothing()
        {
            var choices = new LevelUpChoices(FourCards(), Heal, 3);
            Assert.IsFalse(choices.TryOpen(false));
            Assert.IsFalse(choices.IsOpen);
        }

        [Test]
        public void TryOpen_DrawsThreeDifferentCards_AndUsesOnePendingLevel()
        {
            var choices = new LevelUpChoices(FourCards(), Heal, 3);
            choices.AddLevels(2);
            Assert.IsTrue(choices.TryOpen(false));
            Assert.IsTrue(choices.IsOpen);
            Assert.AreEqual(1, choices.Pending);
            var ids = SkillIds(choices.Cards);
            Assert.AreEqual(3, ids.Length);
            CollectionAssert.AllItemsAreUnique(ids);
        }

        [Test]
        public void TryOpen_WhileOpen_DoesNotDrawAgain()
        {
            var choices = new LevelUpChoices(FourCards(), Heal, 3);
            choices.AddLevels(2);
            choices.TryOpen(false);
            Assert.IsFalse(choices.TryOpen(false));
            Assert.AreEqual(1, choices.Pending);
        }

        [Test]
        public void TryOpen_AtFullHp_LeavesHealOut()
        {
            for (int seed = 0; seed < 20; seed++)
            {
                var deck = new SkillDeck(new[] { new SkillCandidate(Heal, 0, 10), new SkillCandidate(1, 2, 10), new SkillCandidate(2, 1, 10), new SkillCandidate(3, 3, 10) }, new Random(seed));
                var choices = new LevelUpChoices(deck, Heal, 3);
                choices.AddLevels(1);
                choices.TryOpen(true);
                CollectionAssert.DoesNotContain(SkillIds(choices.Cards), Heal);
            }
        }

        [Test]
        public void Cards_ShowTheStackTheyWillReach()
        {
            var deck = Deck(new SkillCandidate(1, 3, 10));
            deck.Take(1);
            var choices = new LevelUpChoices(deck, 1, 3);
            choices.AddLevels(1);
            choices.TryOpen(false);
            var card = choices.Cards[0];
            Assert.AreEqual(1, card.SkillId);
            Assert.AreEqual(2, card.NextStack);
            Assert.AreEqual(3, card.MaxStacks);
        }

        [Test]
        public void Choose_TakesTheSkillAndCloses()
        {
            var deck = FourCards();
            var choices = new LevelUpChoices(deck, Heal, 3);
            choices.AddLevels(1);
            choices.TryOpen(false);
            var shown = choices.Cards[1];
            var chosen = choices.Choose(1);
            Assert.AreEqual(shown.SkillId, chosen.SkillId);
            Assert.AreEqual(1, deck.StackOf(chosen.SkillId));
            Assert.IsFalse(choices.IsOpen);
            Assert.AreEqual(0, choices.Cards.Count);
        }

        [Test]
        public void Choose_WhenClosedOrOutOfRange_Throws()
        {
            var choices = new LevelUpChoices(FourCards(), Heal, 3);
            Assert.Throws<InvalidOperationException>(() => choices.Choose(0));
            choices.AddLevels(1);
            choices.TryOpen(false);
            Assert.Throws<ArgumentOutOfRangeException>(() => choices.Choose(3));
        }

        [Test]
        public void FewCardsLeft_FillsWithACoinCard()
        {
            // 上限つきが1つだけ残り、HP が満タン（回復は外す）→ 強化1枚＋コイン
            var deck = Deck(new SkillCandidate(Heal, 0, 10), new SkillCandidate(1, 1, 10));
            var choices = new LevelUpChoices(deck, Heal, 3);
            choices.AddLevels(1);
            choices.TryOpen(true);
            Assert.AreEqual(2, choices.Cards.Count);
            Assert.AreEqual(1, choices.Cards[0].SkillId);
            Assert.IsTrue(choices.Cards[1].IsCoin);
        }

        [Test]
        public void AllLimitedMaxed_OffersHealAndCoin()
        {
            var deck = Deck(new SkillCandidate(Heal, 0, 10), new SkillCandidate(1, 1, 10));
            deck.Take(1);
            var choices = new LevelUpChoices(deck, Heal, 3);
            choices.AddLevels(2);
            choices.TryOpen(false);
            Assert.AreEqual(2, choices.Cards.Count);
            Assert.AreEqual(Heal, choices.Cards[0].SkillId);
            Assert.AreEqual(0, choices.Cards[0].MaxStacks, "回復は上限なしなので ★ を出さない");
            Assert.IsTrue(choices.Cards[1].IsCoin);
        }

        [Test]
        public void AllLimitedMaxed_AtFullHp_OffersOnlyCoin()
        {
            var deck = Deck(new SkillCandidate(Heal, 0, 10), new SkillCandidate(1, 1, 10));
            deck.Take(1);
            var choices = new LevelUpChoices(deck, Heal, 3);
            choices.AddLevels(1);
            choices.TryOpen(true);
            Assert.AreEqual(1, choices.Cards.Count);
            Assert.IsTrue(choices.Cards[0].IsCoin);
            choices.Choose(0);
            Assert.IsFalse(choices.IsOpen);
        }

        [Test]
        public void InvalidValues_Throw()
        {
            Assert.Throws<ArgumentNullException>(() => new LevelUpChoices(null, Heal, 3));
            Assert.Throws<ArgumentOutOfRangeException>(() => new LevelUpChoices(FourCards(), Heal, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new LevelUpChoices(FourCards(), Heal, 3).AddLevels(-1));
        }
    }
}

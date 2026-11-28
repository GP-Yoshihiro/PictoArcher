using System;
using System.Collections.Generic;
using NUnit.Framework;
using PictoArcher.Core;

namespace PictoArcher.Tests.EditMode
{
    public class SkillDeckTests
    {
        // id 0：何度でも（上限なし）、id 1：最大2、id 2：最大1、id 3：重み0（出ない）、id 4：最大3
        static readonly SkillCandidate[] Pool =
        {
            new SkillCandidate(0, 0, 10),
            new SkillCandidate(1, 2, 10),
            new SkillCandidate(2, 1, 10),
            new SkillCandidate(3, 1, 0),
            new SkillCandidate(4, 3, 10),
        };

        static SkillDeck NewDeck(int seed = 1) => new SkillDeck(Pool, new Random(seed));

        [Test]
        public void Draw_ReturnsRequestedCount_WithoutDuplicates()
        {
            var result = new List<int>();
            for (int seed = 0; seed < 200; seed++)
            {
                NewDeck(seed).Draw(3, null, result);
                Assert.AreEqual(3, result.Count);
                CollectionAssert.AllItemsAreUnique(result);
            }
        }

        [Test]
        public void Draw_ClearsTheResultListFirst()
        {
            var result = new List<int> { 99, 98 };
            NewDeck().Draw(3, null, result);
            CollectionAssert.DoesNotContain(result, 99);
        }

        [Test]
        public void Draw_NeverPicksZeroWeight()
        {
            var result = new List<int>();
            for (int seed = 0; seed < 300; seed++)
            {
                NewDeck(seed).Draw(4, null, result);
                CollectionAssert.DoesNotContain(result, 3);
            }
        }

        [Test]
        public void Draw_SkipsExcludedIds()
        {
            var result = new List<int>();
            var excluded = new HashSet<int> { 0, 4 };
            NewDeck().Draw(3, excluded, result);
            CollectionAssert.AreEquivalent(new[] { 1, 2 }, result);
        }

        [Test]
        public void Draw_SkipsMaxedSkills_AndReturnsFewerWhenPoolIsSmall()
        {
            var deck = NewDeck();
            deck.Take(2);
            deck.Take(1);
            deck.Take(1);
            var result = new List<int>();
            deck.Draw(3, null, result);
            CollectionAssert.AreEquivalent(new[] { 0, 4 }, result);
        }

        [Test]
        public void Draw_FollowsWeights()
        {
            var pool = new[] { new SkillCandidate(0, 0, 30), new SkillCandidate(1, 0, 10) };
            var deck = new SkillDeck(pool, new Random(7));
            var result = new List<int>();
            int first = 0;
            const int n = 4000;
            for (int i = 0; i < n; i++)
            {
                deck.Draw(1, null, result);
                if (result[0] == 0) first++;
            }
            Assert.AreEqual(0.75, first / (double)n, 0.03);
        }

        [Test]
        public void Take_StacksUpToMax_ThenRefuses()
        {
            var deck = NewDeck();
            Assert.AreEqual(0, deck.StackOf(1));
            Assert.AreEqual(1, deck.Take(1));
            Assert.IsFalse(deck.IsMaxed(1));
            Assert.AreEqual(2, deck.Take(1));
            Assert.IsTrue(deck.IsMaxed(1));
            Assert.Throws<InvalidOperationException>(() => deck.Take(1));
            Assert.AreEqual(2, deck.StackOf(1));
        }

        [Test]
        public void UnlimitedSkill_IsNeverMaxed()
        {
            var deck = NewDeck();
            for (int i = 0; i < 50; i++) deck.Take(0);
            Assert.AreEqual(50, deck.StackOf(0));
            Assert.IsFalse(deck.IsMaxed(0));
        }

        [Test]
        public void HasLimitedLeft_FalseWhenEveryLimitedSkillIsMaxed()
        {
            var deck = NewDeck();
            Assert.IsTrue(deck.HasLimitedLeft());
            deck.Take(1); deck.Take(1); deck.Take(2);
            deck.Take(4); deck.Take(4); deck.Take(4);
            // id 3 は重み0なので数えない。id 0 は上限なしなので数えない
            Assert.IsFalse(deck.HasLimitedLeft());
        }

        [Test]
        public void UnknownId_Throws()
        {
            var deck = NewDeck();
            Assert.Throws<ArgumentException>(() => deck.Take(42));
            Assert.Throws<ArgumentException>(() => deck.StackOf(42));
        }

        [Test]
        public void Reset_ClearsStacks()
        {
            var deck = NewDeck();
            deck.Take(2);
            deck.Reset();
            Assert.AreEqual(0, deck.StackOf(2));
            Assert.IsFalse(deck.IsMaxed(2));
        }

        [Test]
        public void Constructor_RejectsBadPools()
        {
            Assert.Throws<ArgumentException>(() => new SkillDeck(new[] { new SkillCandidate(0, 1, 1), new SkillCandidate(0, 2, 1) }, new Random(1)));
            Assert.Throws<ArgumentOutOfRangeException>(() => new SkillCandidate(0, -1, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new SkillCandidate(0, 1, -1));
            Assert.Throws<ArgumentNullException>(() => new SkillDeck(null, new Random(1)));
            Assert.Throws<ArgumentNullException>(() => new SkillDeck(Pool, null));
        }

        [Test]
        public void Draw_NegativeCount_Throws()
            => Assert.Throws<ArgumentOutOfRangeException>(() => NewDeck().Draw(-1, null, new List<int>()));

        [Test]
        public void MaxStacksOf_ReturnsTheLimit()
        {
            var deck = new SkillDeck(new[] { new SkillCandidate(4, 0, 1), new SkillCandidate(5, 3, 1) }, new Random(1));
            Assert.AreEqual(0, deck.MaxStacksOf(4));
            Assert.AreEqual(3, deck.MaxStacksOf(5));
            Assert.Throws<ArgumentException>(() => deck.MaxStacksOf(6));
        }
    }
}

using System;
using System.Collections.Generic;

namespace PictoArcher.Core
{
    /// <summary>
    /// レベルアップのカード選び（設計書 §6「レベルアップ」・§9）。
    /// 上がったレベルの数だけ順にカードを出し、1枚選ぶと閉じる。HP が満タンのときは回復を出さない。
    /// 上限つきの強化が尽きたら回復とコインを出す。出せる強化が足りないときは、残りをコインのカードで埋める（最低1枚は選べる）。
    /// </summary>
    public sealed class LevelUpChoices
    {
        readonly SkillDeck _deck;
        readonly int _healId;
        readonly int _cardCount;
        readonly int[] _healExcluded;
        readonly List<int> _drawn = new List<int>();
        readonly List<LevelUpCard> _cards = new List<LevelUpCard>();

        public LevelUpChoices(SkillDeck deck, int healId, int cardCount)
        {
            _deck = deck ?? throw new ArgumentNullException(nameof(deck));
            if (cardCount < 1) throw new ArgumentOutOfRangeException(nameof(cardCount), "カードは 1 枚以上");
            _healId = healId;
            _cardCount = cardCount;
            _healExcluded = new[] { healId };
        }

        /// <summary>まだカードを出していないレベルアップの数。</summary>
        public int Pending { get; private set; }
        /// <summary>カードを出して、選ぶのを待っている間 true。</summary>
        public bool IsOpen { get; private set; }
        public IReadOnlyList<LevelUpCard> Cards => _cards;

        public void AddLevels(int count)
        {
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
            Pending += count;
        }

        /// <summary>待っているレベルアップがあり、まだ開いていなければ、カードを引いて開く。開いたら true。</summary>
        public bool TryOpen(bool hpIsFull)
        {
            if (IsOpen || Pending == 0) return false;
            Pending--;
            IsOpen = true;
            _cards.Clear();
            if (_deck.HasLimitedLeft())
            {
                _deck.Draw(_cardCount, hpIsFull ? _healExcluded : null, _drawn);
                for (int i = 0; i < _drawn.Count; i++) _cards.Add(CardFor(_drawn[i]));
            }
            else if (!hpIsFull)
            {
                _cards.Add(CardFor(_healId));
            }
            if (_cards.Count < _cardCount) _cards.Add(LevelUpCard.Coin());
            return true;
        }

        /// <summary>index 番目のカードを選び、強化なら1段階重ねて閉じる。選んだカードを返す。</summary>
        public LevelUpCard Choose(int index)
        {
            if (!IsOpen) throw new InvalidOperationException("カードが出ていない");
            if (index < 0 || index >= _cards.Count) throw new ArgumentOutOfRangeException(nameof(index));
            var card = _cards[index];
            if (!card.IsCoin) _deck.Take(card.SkillId);
            _cards.Clear();
            IsOpen = false;
            return card;
        }

        LevelUpCard CardFor(int id)
        {
            int max = _deck.MaxStacksOf(id);
            return LevelUpCard.Skill(id, max == 0 ? 0 : _deck.StackOf(id) + 1, max);
        }
    }
}

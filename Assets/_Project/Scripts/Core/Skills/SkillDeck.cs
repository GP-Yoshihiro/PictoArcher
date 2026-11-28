using System;
using System.Collections.Generic;

namespace PictoArcher.Core
{
    /// <summary>
    /// 強化カードの抽選と重ね取り（設計書 §9）。
    /// 抽選は重み付きで重複なし。上限に達した強化と、呼び出し側が外したいもの（HP が満タンのときの回復など）は候補から外す。
    /// </summary>
    public sealed class SkillDeck
    {
        readonly SkillCandidate[] _candidates;
        readonly int[] _stacks;
        readonly Dictionary<int, int> _indexOf = new Dictionary<int, int>();
        readonly Random _random;
        readonly List<int> _available = new List<int>();

        public SkillDeck(IReadOnlyList<SkillCandidate> candidates, Random random)
        {
            if (candidates == null) throw new ArgumentNullException(nameof(candidates));
            _random = random ?? throw new ArgumentNullException(nameof(random));
            _candidates = new SkillCandidate[candidates.Count];
            for (int i = 0; i < candidates.Count; i++)
            {
                if (_indexOf.ContainsKey(candidates[i].Id)) throw new ArgumentException($"同じ id {candidates[i].Id} が2つある", nameof(candidates));
                _indexOf.Add(candidates[i].Id, i);
                _candidates[i] = candidates[i];
            }
            _stacks = new int[_candidates.Length];
        }

        /// <summary>今の重ね取りの段階（0 はまだ取っていない）。</summary>
        public int StackOf(int id) => _stacks[IndexOf(id)];

        public bool IsMaxed(int id) => IsMaxedAt(IndexOf(id));

        /// <summary>重ね取りの上限（0 は上限なし）。</summary>
        public int MaxStacksOf(int id) => _candidates[IndexOf(id)].MaxStacks;

        /// <summary>1段階重ねて、新しい段階を返す。上限に達しているなら例外。</summary>
        public int Take(int id)
        {
            int i = IndexOf(id);
            if (IsMaxedAt(i)) throw new InvalidOperationException($"id {id} は上限 {_candidates[i].MaxStacks} に達している");
            return ++_stacks[i];
        }

        /// <summary>
        /// 抽選に出る（重みがある）上限つきの強化が、まだ1つでも残っているか。
        /// false なら、カードは回復とコインに置き換える（設計書 §6「レベルアップ」）。
        /// </summary>
        public bool HasLimitedLeft()
        {
            for (int i = 0; i < _candidates.Length; i++)
            {
                if (_candidates[i].Weight > 0 && !_candidates[i].IsUnlimited && !IsMaxedAt(i)) return true;
            }
            return false;
        }

        /// <summary>
        /// 最大 count 枚を重み付き・重複なしで引き、result に id を入れる（result は先に空にする）。
        /// 候補が足りなければ、引けた分だけ入る。excluded は null でよい。
        /// </summary>
        public void Draw(int count, ICollection<int> excluded, List<int> result)
        {
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
            if (result == null) throw new ArgumentNullException(nameof(result));
            result.Clear();
            _available.Clear();
            int total = 0;
            for (int i = 0; i < _candidates.Length; i++)
            {
                var c = _candidates[i];
                if (c.Weight == 0 || IsMaxedAt(i)) continue;
                if (excluded != null && excluded.Contains(c.Id)) continue;
                _available.Add(i);
                total += c.Weight;
            }

            while (result.Count < count && _available.Count > 0)
            {
                int roll = _random.Next(total);
                int pick = 0;
                while (roll >= _candidates[_available[pick]].Weight)
                {
                    roll -= _candidates[_available[pick]].Weight;
                    pick++;
                }
                var chosen = _candidates[_available[pick]];
                result.Add(chosen.Id);
                total -= chosen.Weight;
                _available.RemoveAt(pick);
            }
        }

        /// <summary>1回のプレイの始めに、重ね取りを全部 0 に戻す。</summary>
        public void Reset() => Array.Clear(_stacks, 0, _stacks.Length);

        bool IsMaxedAt(int i) => !_candidates[i].IsUnlimited && _stacks[i] >= _candidates[i].MaxStacks;

        int IndexOf(int id)
        {
            if (!_indexOf.TryGetValue(id, out int i)) throw new ArgumentException($"id {id} は候補にない", nameof(id));
            return i;
        }
    }
}

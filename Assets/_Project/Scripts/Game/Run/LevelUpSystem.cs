using System;
using System.Collections.Generic;
using PictoArcher.Core;

namespace PictoArcher.Game
{
    /// <summary>
    /// 拾った経験値でレベルを上げ、上がるたびにカードを出して1枚選ばせる（設計書 §6「レベルアップ」・§9）。
    /// 選んでいる間は IsChoosing が true。シーンはその間 時間を止め、戦いの System を回さない。
    /// 強化の効果そのものは SkillTaken を受ける側（19・20）が入れる。コインのカードはここでコインを足す。
    /// HUD が読む IRunStatus（★n、経験値バー、コイン）もここが持つ。
    /// </summary>
    public sealed class LevelUpSystem : ITickable, IDisposable, IRunStatus
    {
        readonly LevelUpSettings _settings;
        readonly CardSelectView _view;
        readonly IPlayerStatus _player;
        readonly ExperienceLevels _levels;
        readonly LevelUpChoices _choices;
        readonly List<SkillId> _taken = new List<SkillId>();
        readonly List<int> _takenCounts = new List<int>();
        int _pickedCoins;
        int _cardCoins;

        /// <summary>強化を1段階取ったときに届く（強化と、届いた段階。上限なしの回復は 0）。</summary>
        public event Action<SkillId, int> SkillTaken;

        public LevelUpSystem(LevelUpSettings settings, SkillCatalog catalog, CardSelectView view, IPlayerStatus player, Random random,
            IReadOnlyList<SkillId> excludedSkills = null)
        {
            _settings = settings;
            _view = view;
            _player = player;
            _levels = new ExperienceLevels(settings.FirstLevelExperience, settings.ExperiencePerLevel);
            var deck = new SkillDeck(catalog.BuildCandidates(excludedSkills), random);
            _choices = new LevelUpChoices(deck, (int)SkillId.Heal, Math.Min(settings.CardCount, view.Capacity));
            _view.Init();
        }

        public bool IsChoosing => _choices.IsOpen;
        public int Level => _levels.Level;
        public int Experience => _levels.Experience;
        public int ExperienceToNextLevel => _levels.ToNextLevel;
        /// <summary>拾ったコインとコインのカードの合計。</summary>
        public int Coins => _pickedCoins + _cardCoins;
        /// <summary>取った強化（初めて取った順、重ね取りしても1回）。回復はその場で使い切るので入れない。</summary>
        public IReadOnlyList<SkillId> TakenSkills => _taken;

        /// <summary>強化を取った回数（重ね取りの数。取っていなければ 0）。</summary>
        public int TakenCountOf(SkillId id)
        {
            int index = _taken.IndexOf(id);
            return index < 0 ? 0 : _takenCounts[index];
        }

        /// <summary>拾ったものを数える。シーンが PickupSystem.Collected から渡す。</summary>
        public void Collect(PickupKind kind, int amount)
        {
            if (kind == PickupKind.Coin) _pickedCoins += amount;
            else _choices.AddLevels(_levels.Add(amount));
        }

        public void Tick(float deltaTime)
        {
            if (_choices.IsOpen)
            {
                int chosen = _view.ConsumeChosen();
                if (chosen < 0) return;
                Apply(_choices.Choose(chosen));
                _view.Hide();
            }
            // 死んだあとは出さない。続けてレベルが上がっていたら、すぐ次のカードを出す
            if (_player.IsDead) return;
            if (_choices.TryOpen(_player.Hp >= _player.MaxHp)) _view.Show(_choices.Cards);
        }

        public void Dispose() => _view.Dispose();

        void Apply(LevelUpCard card)
        {
            if (card.IsCoin)
            {
                _cardCoins += _settings.CoinCardAmount;
                return;
            }
            var id = (SkillId)card.SkillId;
            if (id != SkillId.Heal)
            {
                int index = _taken.IndexOf(id);
                if (index < 0)
                {
                    _taken.Add(id);
                    _takenCounts.Add(1);
                }
                else
                {
                    _takenCounts[index]++;
                }
            }
            SkillTaken?.Invoke(id, card.NextStack);
        }
    }
}

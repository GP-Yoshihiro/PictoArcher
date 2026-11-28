using System.Collections.Generic;
using PictoArcher.Core;
using UnityEngine;

namespace PictoArcher.Game
{
    /// <summary>
    /// レベルアップのカード選びの画面。全面の板（Raycast Target）で下のボタンを押せないようにし、カードを横に並べる。
    /// 絵は表から引く：強化は SkillCatalog のアイコン、コインのカードは _coinIcon。
    /// </summary>
    public sealed class CardSelectView : MonoBehaviour
    {
        [SerializeField, Tooltip("全面の板。出ている間だけ表示する")] GameObject _panel;
        [SerializeField] SkillCardView[] _cards = new SkillCardView[0];
        [SerializeField] SkillCatalog _catalog;
        [SerializeField] Sprite _coinIcon;

        public int Capacity => _cards.Length;

        public void Init()
        {
            for (int i = 0; i < _cards.Length; i++) _cards[i].Init();
            _panel.SetActive(false);
        }

        public void Show(IReadOnlyList<LevelUpCard> cards)
        {
            _panel.SetActive(true);
            for (int i = 0; i < _cards.Length; i++)
            {
                if (i >= cards.Count)
                {
                    _cards[i].Hide();
                    continue;
                }
                var card = cards[i];
                if (card.IsCoin) _cards[i].Show(_coinIcon, 0, 0);
                else _cards[i].Show(IconOf((SkillId)card.SkillId), card.NextStack, card.MaxStacks);
            }
        }

        public void Hide()
        {
            for (int i = 0; i < _cards.Length; i++) _cards[i].Hide();
            _panel.SetActive(false);
        }

        /// <summary>押されたカードの番号。押されていなければ -1。押した記録は全部消す（2枚同時なら左を採る）。</summary>
        public int ConsumeChosen()
        {
            int chosen = -1;
            for (int i = 0; i < _cards.Length; i++)
            {
                if (_cards[i].ConsumeClicked() && chosen < 0 && _cards[i].gameObject.activeSelf) chosen = i;
            }
            return chosen;
        }

        public void Dispose()
        {
            for (int i = 0; i < _cards.Length; i++) _cards[i].Dispose();
        }

        Sprite IconOf(SkillId id)
        {
            var definition = _catalog.DefinitionOf(id);
            return definition != null ? definition.Icon : null;
        }
    }
}

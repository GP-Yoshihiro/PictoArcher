using UnityEngine;

namespace PictoArcher.Game
{
    /// <summary>レベルアップの調整値（設計書 §6「レベルアップ」。50 ウェーブで 20 回前後が目安）。</summary>
    [CreateAssetMenu(menuName = "PictoArcher/LevelUpSettings")]
    public sealed class LevelUpSettings : ScriptableObject
    {
        [SerializeField, Min(1), Tooltip("★1 から ★2 に要る経験値")] int _firstLevelExperience = 3;
        [SerializeField, Min(0), Tooltip("1 レベルごとに、次に要る経験値が増える量")] int _experiencePerLevel = 4;
        [SerializeField, Min(1), Tooltip("1 回に出すカードの枚数")] int _cardCount = 3;
        [SerializeField, Min(0), Tooltip("強化が尽きたときのコインのカードでもらえる枚数")] int _coinCardAmount = 20;

        public int FirstLevelExperience => _firstLevelExperience;
        public int ExperiencePerLevel => _experiencePerLevel;
        public int CardCount => _cardCount;
        public int CoinCardAmount => _coinCardAmount;
    }
}

using PictoArcher.Core;
using UnityEngine;

namespace PictoArcher.Game
{
    /// <summary>敵が受けたダメージの知らせ（ダメージ数字、ヒットストップ、強化の効果が使う）。IEnemyEvents.Damaged で届く。</summary>
    public readonly struct DamageReport
    {
        public DamageReport(IEnemyTarget enemy, Vector2 position, int amount, DamageKind kind, bool killed)
        {
            Enemy = enemy;
            Position = position;
            Amount = amount;
            Kind = kind;
            Killed = killed;
        }

        public IEnemyTarget Enemy { get; }
        /// <summary>当たったときの敵の足元の位置。</summary>
        public Vector2 Position { get; }
        /// <summary>実際に減った HP（残りより多く当たっても残りの分だけ。無敵などで減らなければ届かない）。</summary>
        public int Amount { get; }
        public DamageKind Kind { get; }
        /// <summary>この当たりで死んだら true。このあとに同じ敵の Died が届く。</summary>
        public bool Killed { get; }
    }
}

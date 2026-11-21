using UnityEngine;

namespace PictoArcher.Game
{
    /// <summary>敵が死んだ知らせ（経験値とコインを落とす、ウェーブの残りを減らす、死亡時の強化）。IEnemyEvents.Died で1体につき1回だけ届く。</summary>
    public readonly struct EnemyDeath
    {
        public EnemyDeath(EnemyDefinition definition, Vector2 position, int experience, int coins)
        {
            Definition = definition;
            Position = position;
            Experience = experience;
            Coins = coins;
        }

        public EnemyDefinition Definition { get; }
        /// <summary>死んだときの足元の位置。経験値とコインはここから落ちる。</summary>
        public Vector2 Position { get; }
        /// <summary>落とす経験値の量（ふつうは Definition.XpReward。強化版のボスなどで変わってよい）。取得率の強化は拾う側が掛ける。</summary>
        public int Experience { get; }
        /// <summary>落とすコインの量（ふつうは Definition.CoinReward）。</summary>
        public int Coins { get; }
    }
}

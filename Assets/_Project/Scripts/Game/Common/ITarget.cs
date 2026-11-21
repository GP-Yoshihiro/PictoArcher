using UnityEngine;

namespace PictoArcher.Game
{
    /// <summary>
    /// 狙う相手を返す口。敵はプレイヤーをこれで受け取り（コンストラクターか Init で渡す）、プレイヤーは EnemyRegistry から敵をこれで受け取る。
    /// 死んだあとも参照が残ることがあるので、使う前に IsAlive を見る。
    /// </summary>
    public interface ITarget
    {
        /// <summary>足元（根の Transform）の位置。</summary>
        Vector2 Position { get; }
        bool IsAlive { get; }
    }
}

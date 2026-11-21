using UnityEngine;

namespace PictoArcher.Game
{
    /// <summary>
    /// 敵の動きと攻撃の部品（追う、止まって撃つ、突進して自爆する）。自分の状態（攻撃の周期など）を持ち、EnemyUnit が生きている間、毎 Tick 呼ぶ。
    /// 予兆と攻撃は attacks を通して出す。予兆は毎 Tick 出し直す（その Tick に出さなかった予兆は消える）。
    /// </summary>
    public interface IEnemyBehavior
    {
        /// <summary>この Tick の速度を返す（止まるなら zero）。</summary>
        Vector2 Tick(float deltaTime, in EnemyContext context, IEnemyAttackPort attacks);
    }
}

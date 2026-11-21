using System;

namespace PictoArcher.Game
{
    /// <summary>
    /// 敵のダメージと死亡のイベント。敵を動かす System（B さんの側）が実装し、BattleScene が Init で登録して Dispose で外す。
    /// 1回の当たりで死んだときは Damaged（Killed が true）→ Died の順に届く。
    /// </summary>
    public interface IEnemyEvents
    {
        event Action<DamageReport> Damaged;
        event Action<EnemyDeath> Died;
    }
}

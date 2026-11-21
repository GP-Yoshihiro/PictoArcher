using PictoArcher.Core;

namespace PictoArcher.Game
{
    /// <summary>
    /// 弾・レーザー・爆発・体当たりが当たる相手（敵もプレイヤーも）。当てる側は Tick の中でこれを呼ぶ。
    /// 無敵時間、HP の減り、死亡の判定、ダメージと死亡のイベントは受ける側が持つ。
    /// </summary>
    public interface IHittable
    {
        void TakeHit(Damage damage);
    }
}

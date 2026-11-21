using PictoArcher.Core;

namespace PictoArcher.Game
{
    /// <summary>状態異常（炎・氷・毒）を受ける口。継続ダメージや遅くなる処理は、受ける側が自分の Tick で進める。</summary>
    public interface IStatusReceiver
    {
        void ApplyStatus(StatusEffect effect);
    }
}

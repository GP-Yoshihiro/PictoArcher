using UnityEngine;

namespace PictoArcher.Game
{
    /// <summary>プレイヤーの移動の入力。PlayerSystem が Tick の中で1回読む（テストでは決まった値を返す偽物に差し替える）。</summary>
    public interface IMoveInput
    {
        /// <summary>動く向き（長さ 0〜1）。dragRadiusPixels はドラッグで最高速になる距離（画面のピクセル）。</summary>
        Vector2 ReadMove(float dragRadiusPixels);
    }
}

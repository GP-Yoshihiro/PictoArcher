using UnityEngine;

namespace PictoArcher.Game
{
    /// <summary>追う敵が次に歩いて向かう点を返す（障害物を回り込む道）。</summary>
    public interface IChaseRoute
    {
        /// <summary>体の半径 radius の敵が self にいるとき、次に向かう点。</summary>
        Vector2 Waypoint(Vector2 self, float radius);
    }
}

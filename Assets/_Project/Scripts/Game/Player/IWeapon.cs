using UnityEngine;

namespace PictoArcher.Game
{
    /// <summary>武器の撃ち方（銃・レーザー・ランチャー）。PlayerSystem が撃つときに呼ぶ。</summary>
    public interface IWeapon
    {
        /// <summary>撃つ間隔（秒）。</summary>
        float FireInterval { get; }

        /// <summary>origin から direction の向きへ撃つ。</summary>
        void Fire(Vector2 origin, Vector2 direction, IPlayerShotPort shots);
    }
}

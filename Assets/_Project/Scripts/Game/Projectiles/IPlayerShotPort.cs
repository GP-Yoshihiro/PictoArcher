using UnityEngine;

namespace PictoArcher.Game
{
    /// <summary>武器が弾・光線・爆発する弾を出す口。PlayerProjectileSystem が持つ。</summary>
    public interface IPlayerShotPort
    {
        /// <summary>まっすぐ飛び、最初に当たった1体にだけ当たって消える弾。</summary>
        void Fire(Vector2 origin, Vector2 direction, float speed, int damage, float lifetime, SpriteVisual visual);

        /// <summary>敵か壁に当たると爆発し、半径の内の敵すべてに当たる弾。当たらずに寿命が来たら爆発せずに消える。</summary>
        void FireShell(Vector2 origin, Vector2 direction, float speed, int damage, float lifetime, SpriteVisual visual,
            float blastRadius, SpriteVisual blastVisual, float blastShowSeconds);

        /// <summary>一瞬で伸びる光線。壁で止まり、線の上の敵すべてに1回ずつ当たる。</summary>
        void FireBeam(Vector2 origin, Vector2 direction, float length, float hitRadius, int damage,
            SpriteVisual visual, float visualWidth, float showSeconds);
    }
}

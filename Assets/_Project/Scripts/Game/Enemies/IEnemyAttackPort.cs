using UnityEngine;

namespace PictoArcher.Game
{
    /// <summary>敵の動く部品が、予兆を出したり攻撃したりするための口。EnemyUnit が実装する。</summary>
    public interface IEnemyAttackPort
    {
        /// <summary>赤い線を出す。slot は 0 から続けて使う（扇形なら弾の数だけ）。</summary>
        void ShowLine(int slot, Vector2 origin, Vector2 direction, float length, float width, float progress);

        /// <summary>赤い円を出す。progress が 0 から 1 になるにつれて radius まで広がる。</summary>
        void ShowCircle(Vector2 center, float radius, float progress);

        /// <summary>自分の足元から direction へ弾を1発撃つ。</summary>
        void Shoot(EnemyProjectileDefinition projectile, Vector2 direction);

        /// <summary>自爆する。自分の足元から radius 以内にプレイヤーがいれば damage を与え、自分は倒れる。</summary>
        void SelfDestruct(float radius, int damage);

        /// <summary>position に予兆を出し、seconds 秒あとにそこへ enemy を出す（ボスの雑魚呼び）。呼んだ敵が先に死んだら出さない。</summary>
        void Summon(EnemyDefinition enemy, Vector2 position, float seconds);

        /// <summary>体の色をかける（白でそのまま）。次に変えるまで残る。赤・緑・黄・青・紫は使わない。</summary>
        void SetTint(Color color);

        /// <summary>体を光らせる（攻撃の前ぶれ）。amount が 0 で体の色のまま、1 で color。予兆と同じく、その Tick に出さなければ消える。</summary>
        void ShowGlow(Color color, float amount);
    }
}

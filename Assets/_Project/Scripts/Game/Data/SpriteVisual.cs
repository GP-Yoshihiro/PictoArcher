using UnityEngine;

namespace PictoArcher.Game
{
    /// <summary>
    /// キャラ以外の1つの物の見た目の表（弾、障害物、エフェクト、拾うもの）。1つの物に1アセット。
    /// コマが1枚なら止まった絵、2枚以上ならコマ送りのアニメ。動かす部品は、弾やエフェクトを作るタスクで FrameClock を使って足す。
    /// </summary>
    [CreateAssetMenu(menuName = "PictoArcher/Visuals/SpriteVisual")]
    public sealed class SpriteVisual : ScriptableObject
    {
        [SerializeField] SpriteClip _clip = new SpriteClip(true);
        [SerializeField, Tooltip("表示の倍率")] float _scale = 1f;

        public SpriteClip Clip => _clip;
        public float Scale => _scale;
    }
}

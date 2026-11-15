using UnityEngine;

namespace PictoArcher.Game
{
    /// <summary>
    /// キャラ1体の見た目の表（プレイヤーの武器ごと、敵のモデルごと、中ボス、ボス）。
    /// 絵が届いたら、コマの枠に絵をアタッチするだけで差し替わる。動かすのは CharacterSpriteAnimator。
    /// </summary>
    [CreateAssetMenu(menuName = "PictoArcher/Visuals/CharacterVisual")]
    public sealed class CharacterVisual : ScriptableObject
    {
        [SerializeField] SpriteClip _idle = new SpriteClip(true);
        [SerializeField] SpriteClip _move = new SpriteClip(true);
        [SerializeField] SpriteClip _attack = new SpriteClip(false);
        [SerializeField] SpriteClip _death = new SpriteClip(false);
        [SerializeField, Tooltip("表示の倍率。中ボス・ボスは 2")] float _scale = 1f;
        [SerializeField, Tooltip("絵の中心から足元までのずれ（ユニット、右向き、倍率をかける前）。キャラの根の位置が足元になる")] Vector2 _footOffset;

        public SpriteClip Idle => _idle;
        public SpriteClip Move => _move;
        public SpriteClip Attack => _attack;
        public SpriteClip Death => _death;
        public float Scale => _scale;
        public Vector2 FootOffset => _footOffset;

        public SpriteClip Clip(CharacterPose pose)
        {
            switch (pose)
            {
                case CharacterPose.Move: return _move;
                case CharacterPose.Attack: return _attack;
                case CharacterPose.Death: return _death;
                default: return _idle;
            }
        }
    }
}

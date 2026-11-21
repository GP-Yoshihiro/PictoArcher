using PictoArcher.Core;
using UnityEngine;

namespace PictoArcher.Game
{
    /// <summary>
    /// CharacterVisual のコマを Tick で切り替えて出す（Animator Controller は使わない）。
    /// プレハブの形：キャラの根（足元。移動や当たり判定はここ）の子に SpriteRenderer を置き、その SpriteRenderer を渡す。
    /// 子の Transform には、表の倍率と足元のずれが入る（スプライトの pivot は中央のままにする）。
    /// 持ち主（プレイヤーや敵の System）が Play・SetFacingLeft・Tick を呼ぶ。
    /// </summary>
    public sealed class CharacterSpriteAnimator
    {
        readonly SpriteRenderer _renderer;
        readonly Transform _transform;
        readonly CharacterVisual _visual;
        readonly FrameClock _clock = new FrameClock();

        SpriteClip _clip;
        bool _facingLeft;

        public CharacterSpriteAnimator(SpriteRenderer renderer, CharacterVisual visual)
        {
            _renderer = renderer;
            _transform = renderer.transform;
            _visual = visual;
            _transform.localScale = new Vector3(visual.Scale, visual.Scale, 1f);
            _renderer.flipX = false;
            ApplyFootOffset();
            StartPose(CharacterPose.Idle);
        }

        public CharacterPose Pose { get; private set; }

        /// <summary>ループしないアニメ（攻撃・やられ）が最後のコマまで進んだか。</summary>
        public bool IsFinished => _clock.IsFinished;

        /// <summary>アニメを切り替える。同じループのアニメを続けて頼んだときは、頭から始め直さない。</summary>
        public void Play(CharacterPose pose)
        {
            if (pose == Pose && _clip.Loops) return;
            StartPose(pose);
        }

        public void SetFacingLeft(bool facingLeft)
        {
            if (facingLeft == _facingLeft) return;
            _facingLeft = facingLeft;
            _renderer.flipX = facingLeft;
            ApplyFootOffset();
        }

        public void Tick(float deltaTime)
        {
            _clock.Tick(deltaTime);
            ShowFrame();
        }

        void StartPose(CharacterPose pose)
        {
            Pose = pose;
            _clip = _visual.Clip(pose);
            _clock.Play(_clip.Frames.Count, _clip.FramesPerSecond, _clip.Loops);
            ShowFrame();
        }

        void ShowFrame()
        {
            var frames = _clip.Frames;
            if (frames.Count == 0) return;   // コマのない枠では前の絵を残す（空の枠は VisualTableTests が見つける）
            var sprite = frames[_clock.FrameIndex];
            if (_renderer.sprite != sprite) _renderer.sprite = sprite;
        }

        // 根を足元にするため、絵の中心を「足元から見た絵の中心」に置く。左向きでは横のずれも反転する
        void ApplyFootOffset()
        {
            var offset = _visual.FootOffset * _visual.Scale;
            _transform.localPosition = new Vector3(_facingLeft ? offset.x : -offset.x, -offset.y, 0f);
        }
    }
}

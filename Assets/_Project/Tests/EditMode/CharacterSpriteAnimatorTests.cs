using System.Collections.Generic;
using NUnit.Framework;
using PictoArcher.Game;
using UnityEditor;
using UnityEngine;

namespace PictoArcher.Tests.EditMode
{
    public class CharacterSpriteAnimatorTests
    {
        readonly List<Object> _created = new List<Object>();
        Sprite _a, _b, _c;
        SpriteRenderer _renderer;

        [SetUp] public void SetUp()
        {
            _a = MakeSprite();
            _b = MakeSprite();
            _c = MakeSprite();
            var root = new GameObject("Root");
            var body = new GameObject("Body");
            body.transform.SetParent(root.transform, false);
            _created.Add(root);
            _renderer = body.AddComponent<SpriteRenderer>();
        }

        [TearDown] public void TearDown()
        {
            for (int i = 0; i < _created.Count; i++) Object.DestroyImmediate(_created[i]);
            _created.Clear();
        }

        [Test] public void Starts_OnFirstIdleFrame()
        {
            var animator = new CharacterSpriteAnimator(_renderer, MakeVisual(1f, Vector2.zero));
            Assert.AreEqual(CharacterPose.Idle, animator.Pose);
            Assert.AreSame(_a, _renderer.sprite);
        }

        [Test] public void Tick_AdvancesMoveFrames()
        {
            var animator = new CharacterSpriteAnimator(_renderer, MakeVisual(1f, Vector2.zero));
            animator.Play(CharacterPose.Move);
            Assert.AreSame(_a, _renderer.sprite);
            animator.Tick(0.1f);
            Assert.AreSame(_b, _renderer.sprite);
        }

        [Test] public void Play_SameLoopingPose_DoesNotRestart()
        {
            var animator = new CharacterSpriteAnimator(_renderer, MakeVisual(1f, Vector2.zero));
            animator.Play(CharacterPose.Move);
            animator.Tick(0.1f);
            animator.Play(CharacterPose.Move);
            Assert.AreSame(_b, _renderer.sprite);
        }

        [Test] public void Death_StopsOnLastFrameAndFinishes()
        {
            var animator = new CharacterSpriteAnimator(_renderer, MakeVisual(1f, Vector2.zero));
            animator.Play(CharacterPose.Death);
            Assert.IsFalse(animator.IsFinished);
            animator.Tick(5f);
            Assert.AreSame(_c, _renderer.sprite);
            Assert.IsTrue(animator.IsFinished);
        }

        [Test] public void ScaleAndFootOffset_GoToTheSpriteTransform()
        {
            new CharacterSpriteAnimator(_renderer, MakeVisual(2f, new Vector2(0.25f, -1f)));
            var transform = _renderer.transform;
            Assert.AreEqual(new Vector3(2f, 2f, 1f), transform.localScale);
            Assert.AreEqual(new Vector3(-0.5f, 2f, 0f), transform.localPosition);
        }

        [Test] public void FacingLeft_FlipsSpriteAndMirrorsFootOffset()
        {
            var animator = new CharacterSpriteAnimator(_renderer, MakeVisual(1f, new Vector2(0.25f, -1f)));
            animator.SetFacingLeft(true);
            Assert.IsTrue(_renderer.flipX);
            Assert.AreEqual(new Vector3(0.25f, 1f, 0f), _renderer.transform.localPosition);
            animator.SetFacingLeft(false);
            Assert.IsFalse(_renderer.flipX);
            Assert.AreEqual(new Vector3(-0.25f, 1f, 0f), _renderer.transform.localPosition);
        }

        [Test] public void EmptyClip_DoesNotThrow()
        {
            var visual = ScriptableObject.CreateInstance<CharacterVisual>();
            _created.Add(visual);
            var animator = new CharacterSpriteAnimator(_renderer, visual);
            Assert.DoesNotThrow(() =>
            {
                animator.Play(CharacterPose.Attack);
                animator.Tick(1f);
            });
            Assert.IsNull(_renderer.sprite);
        }

        // 待機 [a]、移動 [a, b] 10fps ループ、やられ [b, c] 10fps 1回
        CharacterVisual MakeVisual(float scale, Vector2 footOffset)
        {
            var visual = ScriptableObject.CreateInstance<CharacterVisual>();
            _created.Add(visual);
            var so = new SerializedObject(visual);
            SetClip(so.FindProperty("_idle"), 10f, _a);
            SetClip(so.FindProperty("_move"), 10f, _a, _b);
            SetClip(so.FindProperty("_death"), 10f, _b, _c);
            so.FindProperty("_scale").floatValue = scale;
            so.FindProperty("_footOffset").vector2Value = footOffset;
            so.ApplyModifiedPropertiesWithoutUndo();
            return visual;
        }

        static void SetClip(SerializedProperty clip, float fps, params Sprite[] frames)
        {
            var array = clip.FindPropertyRelative("_frames");
            array.arraySize = frames.Length;
            for (int i = 0; i < frames.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = frames[i];
            clip.FindPropertyRelative("_framesPerSecond").floatValue = fps;
        }

        Sprite MakeSprite()
        {
            var texture = new Texture2D(2, 2);
            var sprite = Sprite.Create(texture, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f), 16f);
            _created.Add(sprite);
            _created.Add(texture);
            return sprite;
        }
    }
}

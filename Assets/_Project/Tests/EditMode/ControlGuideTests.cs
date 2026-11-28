using NUnit.Framework;
using PictoArcher.Core;
using UnityEngine;

namespace PictoArcher.Tests.EditMode
{
    public class ControlGuideTests
    {
        static ControlGuide NewGuide() => new ControlGuide(Vector2.zero, 0.5f, 1f);

        [Test] public void StartsVisible_WithUpKeyAndHandAtCenter()
        {
            var g = NewGuide();
            Assert.IsTrue(g.IsVisible);
            Assert.AreEqual(0, g.HighlightedKey);
            Assert.AreEqual(Vector2.zero, g.DragOffset);
        }

        [Test] public void KeysGoUpRightDownLeft_ThenWrap()
        {
            var g = NewGuide();
            int[] seen = new int[5];
            for (int i = 0; i < 5; i++)
            {
                seen[i] = g.HighlightedKey;
                g.Tick(1f, Vector2.zero);
            }
            CollectionAssert.AreEqual(new[] { 0, 1, 2, 3, 0 }, seen);
        }

        [Test] public void HandMovesTowardTheLitKey_AndBack()
        {
            var g = NewGuide();
            g.Tick(0.5f, Vector2.zero);
            Assert.AreEqual(0f, g.DragOffset.x, 1e-4f);
            Assert.AreEqual(1f, g.DragOffset.y, 1e-4f);

            g.Tick(1f, Vector2.zero);   // 右のキーの半ば
            Assert.AreEqual(1f, g.DragOffset.x, 1e-4f);
            Assert.AreEqual(0f, g.DragOffset.y, 1e-4f);

            g.Tick(0.49f, Vector2.zero);   // 右のキーの終わりで真ん中に戻る
            Assert.Less(g.DragOffset.magnitude, 0.05f);
        }

        [Test] public void SmallMove_KeepsGuide()
        {
            var g = NewGuide();
            g.Tick(0.1f, new Vector2(0.3f, 0f));
            Assert.IsTrue(g.IsVisible);
        }

        [Test] public void MovingFarEnough_HidesGuide_ForGood()
        {
            var g = NewGuide();
            g.Tick(0.1f, new Vector2(0.3f, 0.4f));
            Assert.IsFalse(g.IsVisible);
            g.Tick(0.1f, Vector2.zero);
            Assert.IsFalse(g.IsVisible);
        }

        [Test] public void ZeroStep_DoesNotBreak()
        {
            var g = new ControlGuide(Vector2.zero, 0.5f, 0f);
            g.Tick(1f, Vector2.zero);
            Assert.That(g.HighlightedKey, Is.InRange(0, 3));
            Assert.IsFalse(float.IsNaN(g.DragOffset.x));
        }
    }
}

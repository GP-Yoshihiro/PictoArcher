using NUnit.Framework;
using PictoArcher.Core;
using UnityEngine;

namespace PictoArcher.Tests.EditMode
{
    public class MoveVectorResolverTests
    {
        const float R = 80f;
        static Vector2 Kb(float x, float y) => new Vector2(x, y);

        [Test] public void NoInput_IsZero() =>
            Assert.AreEqual(Vector2.zero, MoveVectorResolver.Resolve(Vector2.zero, false, Vector2.zero, Vector2.zero, R));

        [Test] public void KeyboardDiagonal_IsNotFaster() =>
            Assert.AreEqual(1f, MoveVectorResolver.Resolve(Kb(1, 1), false, Vector2.zero, Vector2.zero, R).magnitude, 1e-4f);

        [Test] public void DragInsideDeadZone_IsZero() =>
            Assert.AreEqual(Vector2.zero, MoveVectorResolver.Resolve(Vector2.zero, true, Vector2.zero, new Vector2(9, 0), R));

        [Test] public void DragHalfRadius_IsHalfSpeed() =>
            Assert.AreEqual(new Vector2(0.5f, 0), MoveVectorResolver.Resolve(Vector2.zero, true, Vector2.zero, new Vector2(40, 0), R));

        [Test] public void DragBeyondRadius_IsClampedToOne() =>
            Assert.AreEqual(1f, MoveVectorResolver.Resolve(Vector2.zero, true, Vector2.zero, new Vector2(300, 400), R).magnitude, 1e-4f);

        [Test] public void DragNotActive_IsIgnored() =>
            Assert.AreEqual(Vector2.zero, MoveVectorResolver.Resolve(Vector2.zero, false, Vector2.zero, new Vector2(80, 0), R));

        [Test] public void KeyboardAndDragTogether_KeyboardWinsAndNotFaster()
        {
            var v = MoveVectorResolver.Resolve(Kb(0, 1), true, Vector2.zero, new Vector2(80, 0), R);
            Assert.AreEqual(new Vector2(0, 1), v);
        }
    }
}

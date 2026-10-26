using System.Collections.Generic;
using NUnit.Framework;
using PictoArcher.Core;
using UnityEngine;

namespace PictoArcher.Tests.EditMode
{
    public class TargetSelectorTests
    {
        [Test] public void Empty_ReturnsMinusOne() =>
            Assert.AreEqual(-1, TargetSelector.FindNearest(Vector2.zero, new List<Vector2>()));

        [Test] public void PicksNearest() =>
            Assert.AreEqual(1, TargetSelector.FindNearest(Vector2.zero,
                new List<Vector2> { new(5, 0), new(0, 2), new(-3, 0) }));

        [Test] public void Tie_PicksLowerIndex() =>
            Assert.AreEqual(0, TargetSelector.FindNearest(Vector2.zero,
                new List<Vector2> { new(2, 0), new(-2, 0) }));
    }
}

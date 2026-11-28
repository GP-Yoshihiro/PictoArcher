using System;
using NUnit.Framework;
using PictoArcher.Core;
using UnityEngine;

namespace PictoArcher.Tests.EditMode
{
    public class ObstacleOverlapTests
    {
        static readonly Rect Box = new Rect(0f, 0f, 1f, 1f);

        [Test] public void CenterInside_Overlaps() => Assert.IsTrue(ObstacleOverlap.Overlaps(Box, new Vector2(0.5f, 0.5f), 0.1f));
        [Test] public void FarAway_DoesNotOverlap() => Assert.IsFalse(ObstacleOverlap.Overlaps(Box, new Vector2(5f, 5f), 0.6f));
        [Test] public void NearCornerWithinRadius_Overlaps() => Assert.IsTrue(ObstacleOverlap.Overlaps(Box, new Vector2(1.3f, 1.3f), 0.5f));
        [Test] public void DiagonalOutsideRadius_DoesNotOverlap() => Assert.IsFalse(ObstacleOverlap.Overlaps(Box, new Vector2(1.4f, 1.4f), 0.5f));
        [Test] public void ExactlyRadiusFromEdge_DoesNotOverlap() => Assert.IsFalse(ObstacleOverlap.Overlaps(Box, new Vector2(1.5f, 0.5f), 0.5f));
        [Test] public void NegativeRadius_Throws() => Assert.Throws<ArgumentOutOfRangeException>(() => ObstacleOverlap.Overlaps(Box, Vector2.zero, -0.1f));
    }
}

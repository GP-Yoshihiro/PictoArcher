using System.Collections.Generic;
using UnityEngine;

namespace PictoArcher.Core
{
    public static class TargetSelector
    {
        public static int FindNearest(Vector2 from, IReadOnlyList<Vector2> candidates)
        {
            int best = -1;
            float bestSqr = float.MaxValue;
            for (int i = 0; i < candidates.Count; i++)
            {
                float sqr = (candidates[i] - from).sqrMagnitude;
                if (best < 0 || sqr < bestSqr)
                {
                    best = i;
                    bestSqr = sqr;
                }
            }
            return best;
        }
    }
}

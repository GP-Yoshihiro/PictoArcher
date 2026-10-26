using System.Collections.Generic;
using UnityEngine;

namespace PictoArcher.Game
{
    public static class EnemyRegistry
    {
        static readonly List<Transform> active = new List<Transform>();

        public static IReadOnlyList<Transform> Active => active;

        public static void Register(Transform t)
        {
            if (!active.Contains(t)) active.Add(t);
        }

        public static void Unregister(Transform t) => active.Remove(t);

        public static void Clear() => active.Clear();
    }
}

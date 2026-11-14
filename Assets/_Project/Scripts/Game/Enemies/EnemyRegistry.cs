using System.Collections.Generic;
using UnityEngine;

namespace PictoArcher.Game
{
    /// <summary>今いる敵の一覧。RunScene が1つ作って、使う System に渡す（static にしない）。</summary>
    public sealed class EnemyRegistry
    {
        readonly List<Transform> _active = new List<Transform>();

        public IReadOnlyList<Transform> Active => _active;

        /// <summary>同じものを2回登録しても1つだけ持つ。</summary>
        public void Register(Transform t)
        {
            if (!_active.Contains(t)) _active.Add(t);
        }

        public void Unregister(Transform t) => _active.Remove(t);

        public void Clear() => _active.Clear();
    }
}

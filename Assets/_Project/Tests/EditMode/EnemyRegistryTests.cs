using System.Collections.Generic;
using NUnit.Framework;
using PictoArcher.Core;
using PictoArcher.Game;
using UnityEngine;

namespace PictoArcher.Tests.EditMode
{
    public class EnemyRegistryTests
    {
        sealed class FakeEnemy : IEnemyTarget
        {
            public FakeEnemy(Vector2 position) => Position = position;
            public Vector2 Position { get; set; }
            public bool IsAlive { get; set; } = true;
            public void TakeHit(Damage damage) { }
            public void ApplyStatus(StatusEffect effect) { }
        }

        readonly List<GameObject> _created = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (var go in _created) if (go != null) Object.DestroyImmediate(go);
            _created.Clear();
        }

        Collider2D NewBody(string name)
        {
            var go = new GameObject(name);
            _created.Add(go);
            return go.AddComponent<CircleCollider2D>();
        }

        [Test] public void IgnoresDuplicateRegisterAndUnknownUnregister()
        {
            var registry = new EnemyRegistry();
            var a = new FakeEnemy(Vector2.zero);
            var body = NewBody("a");
            registry.Register(a, body);
            registry.Register(a, body);
            registry.Unregister(new FakeEnemy(Vector2.one));
            Assert.AreEqual(1, registry.Active.Count);
        }

        [Test] public void Instances_DoNotShareState()
        {
            var first = new EnemyRegistry();
            var second = new EnemyRegistry();
            first.Register(new FakeEnemy(Vector2.zero), NewBody("a"));
            Assert.AreEqual(1, first.Active.Count);
            Assert.AreEqual(0, second.Active.Count);
        }

        [Test] public void TryGet_FindsEnemyByItsCollider_UntilUnregistered()
        {
            var registry = new EnemyRegistry();
            var a = new FakeEnemy(Vector2.zero);
            var body = NewBody("a");
            registry.Register(a, body);

            Assert.IsTrue(registry.TryGet(body, out var found));
            Assert.AreSame(a, found);
            Assert.IsFalse(registry.TryGet(NewBody("other"), out _));
            Assert.IsFalse(registry.TryGet(null, out _));

            registry.Unregister(a);
            Assert.IsFalse(registry.TryGet(body, out _));
        }

        [Test] public void TryFindNearest_ReturnsClosestLivingEnemy()
        {
            var registry = new EnemyRegistry();
            var far = new FakeEnemy(new Vector2(5f, 0f));
            var near = new FakeEnemy(new Vector2(1f, 0f));
            var dead = new FakeEnemy(new Vector2(0.5f, 0f)) { IsAlive = false };
            registry.Register(far, NewBody("far"));
            registry.Register(near, NewBody("near"));
            registry.Register(dead, NewBody("dead"));

            Assert.IsTrue(registry.TryFindNearest(Vector2.zero, out var found));
            Assert.AreSame(near, found, "死んでいる敵は選ばないはずです");
        }

        [Test] public void TryFindNearest_SkipsExcludedEnemy()
        {
            var registry = new EnemyRegistry();
            var self = new FakeEnemy(Vector2.zero);
            var other = new FakeEnemy(new Vector2(3f, 0f));
            registry.Register(self, NewBody("self"));
            registry.Register(other, NewBody("other"));

            Assert.IsTrue(registry.TryFindNearest(Vector2.zero, out var found, exclude: self));
            Assert.AreSame(other, found);
        }

        [Test] public void TryFindNearest_FalseWhenNoLivingEnemy()
        {
            var registry = new EnemyRegistry();
            Assert.IsFalse(registry.TryFindNearest(Vector2.zero, out var none));
            Assert.IsNull(none);

            registry.Register(new FakeEnemy(Vector2.one) { IsAlive = false }, NewBody("dead"));
            Assert.IsFalse(registry.TryFindNearest(Vector2.zero, out _));
        }

        [Test] public void FindWithin_ReplacesResultsWithLivingEnemiesInsideRadius()
        {
            var registry = new EnemyRegistry();
            var inside = new FakeEnemy(new Vector2(1f, 0f));
            var onEdge = new FakeEnemy(new Vector2(0f, 2f));
            var outside = new FakeEnemy(new Vector2(3f, 0f));
            var dead = new FakeEnemy(new Vector2(0.5f, 0f)) { IsAlive = false };
            registry.Register(inside, NewBody("inside"));
            registry.Register(onEdge, NewBody("edge"));
            registry.Register(outside, NewBody("outside"));
            registry.Register(dead, NewBody("dead"));

            var results = new List<IEnemyTarget> { outside };
            int count = registry.FindWithin(Vector2.zero, 2f, results);

            Assert.AreEqual(2, count);
            CollectionAssert.AreEquivalent(new IEnemyTarget[] { inside, onEdge }, results, "前の中身は消し、半径ちょうども入れるはずです");
        }

        [Test] public void Clear_RemovesEnemiesAndColliders()
        {
            var registry = new EnemyRegistry();
            var body = NewBody("a");
            registry.Register(new FakeEnemy(Vector2.zero), body);
            registry.Clear();
            Assert.AreEqual(0, registry.Active.Count);
            Assert.IsFalse(registry.TryGet(body, out _));
        }
    }
}

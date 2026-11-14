using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using PictoArcher.Game;
using UnityEngine;

namespace PictoArcher.Tests.EditMode
{
    public class CollisionRelay2DTests
    {
        readonly List<GameObject> _created = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (var go in _created) if (go != null) Object.DestroyImmediate(go);
            _created.Clear();
        }

        CollisionRelay2D NewRelay()
        {
            var go = new GameObject("relay");
            _created.Add(go);
            return go.AddComponent<CollisionRelay2D>();
        }

        Collider2D NewOther()
        {
            var go = new GameObject("other");
            _created.Add(go);
            return go.AddComponent<BoxCollider2D>();
        }

        static void Call(CollisionRelay2D relay, string method, Collider2D other) =>
            typeof(CollisionRelay2D)
                .GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(relay, new object[] { other });

        [Test] public void DrainEntered_ReturnsEnteredOnceThenEmpty()
        {
            var relay = NewRelay();
            var other = NewOther();
            Call(relay, "OnTriggerEnter2D", other);

            var into = new List<Collider2D>();
            Assert.AreEqual(1, relay.DrainEntered(into));
            Assert.AreSame(other, into[0]);

            into.Clear();
            Assert.AreEqual(0, relay.DrainEntered(into));
            Assert.AreEqual(0, into.Count);
        }

        [Test] public void Exit_RemovesFromTouching()
        {
            var relay = NewRelay();
            var other = NewOther();
            Call(relay, "OnTriggerEnter2D", other);
            CollectionAssert.Contains(relay.Touching, other);
            Call(relay, "OnTriggerExit2D", other);
            CollectionAssert.DoesNotContain(relay.Touching, other);
        }
    }
}

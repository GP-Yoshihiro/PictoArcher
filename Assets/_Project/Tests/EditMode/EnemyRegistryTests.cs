using System.Collections.Generic;
using NUnit.Framework;
using PictoArcher.Game;
using UnityEngine;

namespace PictoArcher.Tests.EditMode
{
    public class EnemyRegistryTests
    {
        readonly List<GameObject> _created = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (var go in _created) if (go != null) Object.DestroyImmediate(go);
            _created.Clear();
        }

        Transform NewTransform(string name)
        {
            var go = new GameObject(name);
            _created.Add(go);
            return go.transform;
        }

        [Test] public void IgnoresDuplicateRegisterAndUnknownUnregister()
        {
            var registry = new EnemyRegistry();
            var a = NewTransform("a");
            var b = NewTransform("b");
            registry.Register(a);
            registry.Register(a);
            registry.Unregister(b);
            Assert.AreEqual(1, registry.Active.Count);
        }

        [Test] public void Instances_DoNotShareState()
        {
            var first = new EnemyRegistry();
            var second = new EnemyRegistry();
            first.Register(NewTransform("a"));
            Assert.AreEqual(1, first.Active.Count);
            Assert.AreEqual(0, second.Active.Count);
        }
    }
}

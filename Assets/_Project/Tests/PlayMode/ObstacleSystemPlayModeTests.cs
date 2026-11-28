using System.Collections;
using NUnit.Framework;
using PictoArcher.Core;
using PictoArcher.Game;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace PictoArcher.Tests.PlayMode
{
    public class ObstacleSystemPlayModeTests
    {
        static readonly Vector2 Far = new Vector2(100f, 100f);

        sealed class FakePlayer : ITarget
        {
            public Vector2 Position { get; set; } = Far;
            public bool IsAlive => true;
        }

        FakePlayer _player;
        GameObject _parent;
        ObstacleSystem _system;
        ObstacleSwitchSettings _settings;
        ObstacleLayout _layout1;
        ObstacleLayout _layout2;

        [SetUp]
        public void SetUp()
        {
            _player = new FakePlayer();
            _parent = new GameObject("Obstacles");
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Field/Obstacle.prefab").GetComponent<ObstacleView>();
            _settings = AssetDatabase.LoadAssetAtPath<ObstacleSwitchSettings>("Assets/_Project/Data/Obstacles/ObstacleSwitchSettings.asset");
            _layout1 = AssetDatabase.LoadAssetAtPath<ObstacleLayout>("Assets/_Project/Data/Obstacles/Layout1_Corners.asset");
            _layout2 = AssetDatabase.LoadAssetAtPath<ObstacleLayout>("Assets/_Project/Data/Obstacles/Layout2_Columns.asset");
            _system = new ObstacleSystem(prefab, _parent.transform, _settings, _player);
        }

        [TearDown]
        public void TearDown()
        {
            _system.Dispose();
            Object.Destroy(_parent);
        }

        ObstacleView[] Views() => _parent.GetComponentsInChildren<ObstacleView>();
        static BoxCollider2D Box(ObstacleView v) => v.GetComponent<BoxCollider2D>();
        static SpriteRenderer Sprite(ObstacleView v) => v.GetComponent<SpriteRenderer>();

        void AssertAllSolid(int count)
        {
            var views = Views();
            Assert.AreEqual(count, views.Length);
            foreach (var v in views)
            {
                Assert.IsTrue(Box(v).enabled, v.name);
                Assert.AreEqual(Color.white, Sprite(v).color);
            }
        }

        // 置いた順に子になるので、i 番目の View は i 番目の Placement
        void AssertPositions(ObstacleLayout layout)
        {
            var views = Views();
            Assert.AreEqual(layout.Placements.Count, views.Length);
            for (int i = 0; i < views.Length; i++)
            {
                var expected = layout.Placements[i].Bounds.center;
                var actual = (Vector2)Box(views[i]).bounds.center;
                Assert.AreEqual(expected.x, actual.x, 1e-3f);
                Assert.AreEqual(expected.y, actual.y, 1e-3f);
            }
        }

        [UnityTest]
        public IEnumerator ShowImmediately_PlacesSolidObstaclesOnWallLayer()
        {
            _system.ShowImmediately(_layout1);
            yield return null;
            AssertAllSolid(4);
            foreach (var v in Views()) Assert.AreEqual(Layers.Wall, v.gameObject.layer);
            AssertPositions(_layout1);
            Assert.IsFalse(_system.IsSwitching);
        }

        [UnityTest]
        public IEnumerator ShowImmediately_ObstacleUnderPlayer_WaitsUntilPlayerLeaves()
        {
            _player.Position = _layout1.Placements[0].Bounds.center;
            _system.ShowImmediately(_layout1);
            yield return null;
            var views = Views();
            Assert.IsFalse(Box(views[0]).enabled, "プレイヤーの下は出さない");
            Assert.AreEqual(_settings.ShadowColor, Sprite(views[0]).color);
            for (int i = 1; i < views.Length; i++) Assert.IsTrue(Box(views[i]).enabled);
            Assert.IsTrue(_system.IsSwitching);

            _player.Position = Far;
            _system.Tick(0f);
            AssertAllSolid(4);
        }

        [UnityTest]
        public IEnumerator SwitchTo_RemovesOldAndShowsShadowsWithoutColliders()
        {
            _system.ShowImmediately(_layout1);
            _system.SwitchTo(_layout2);
            _system.Tick(0.1f);
            yield return null;
            var views = Views();
            Assert.AreEqual(6, views.Length, "古い障害物は消える");
            foreach (var v in views)
            {
                Assert.IsFalse(Box(v).enabled);
                Assert.AreEqual(_settings.ShadowColor, Sprite(v).color);
            }
            Assert.IsTrue(_system.IsSwitching);
        }

        [UnityTest]
        public IEnumerator SwitchTo_AfterShadowSeconds_TurnsSolid()
        {
            _system.ShowImmediately(_layout1);
            _system.SwitchTo(_layout2);
            _system.Tick(0.3f);
            _system.Tick(0.3f);
            yield return null;
            AssertAllSolid(6);
            Assert.IsFalse(_system.IsSwitching);
            Assert.AreSame(_layout2, _system.Current);
        }

        [UnityTest]
        public IEnumerator SwitchTo_ObstacleUnderPlayer_StaysShadowUntilPlayerLeaves()
        {
            _player.Position = _layout2.Placements[0].Bounds.center;
            _system.SwitchTo(_layout2);
            _system.Tick(0.6f);
            yield return null;
            var views = Views();
            Assert.IsFalse(Box(views[0]).enabled);
            for (int i = 1; i < views.Length; i++) Assert.IsTrue(Box(views[i]).enabled);
            Assert.IsTrue(_system.IsSwitching);

            _player.Position = Far;
            _system.Tick(0.016f);
            AssertAllSolid(6);
            Assert.IsFalse(_system.IsSwitching);
        }

        [UnityTest]
        public IEnumerator SwitchTo_SameLayout_KeepsObstaclesSolid()
        {
            _system.ShowImmediately(_layout1);
            yield return null;
            var before = Views();
            _system.SwitchTo(_layout1);
            _system.Tick(0.1f);
            yield return null;
            AssertAllSolid(4);
            CollectionAssert.AreEqual(before, Views(), "同じ物が残る（ちらつかない）");
        }

        [UnityTest]
        public IEnumerator SwitchTo_Null_KeepsCurrentLayout()
        {
            _system.ShowImmediately(_layout1);
            _system.SwitchTo(null);
            yield return null;
            Assert.AreSame(_layout1, _system.Current);
            AssertAllSolid(4);
        }

        [UnityTest]
        public IEnumerator SwitchTo_DuringShadow_ReplacesPendingObstacles()
        {
            _system.SwitchTo(_layout2);
            _system.Tick(0.1f);
            _system.SwitchTo(_layout1);
            yield return null;
            var views = Views();
            Assert.AreEqual(4, views.Length, "古い影は残らない");
            foreach (var v in views) Assert.IsFalse(Box(v).enabled);
        }

        [UnityTest]
        public IEnumerator WaveCleared_SwitchesToNextWavesLayout()
        {
            var defs = new[] { ScriptableObject.CreateInstance<WaveDefinition>(), ScriptableObject.CreateInstance<WaveDefinition>() };
            var layouts = new[] { _layout1, _layout2 };
            for (int i = 0; i < 2; i++)
            {
                var so = new SerializedObject(defs[i]);
                so.FindProperty("_obstacleLayout").objectReferenceValue = layouts[i];
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            var waves = new WaveSequence(1, WaveSequence.DefaultRestSeconds);
            waves.WaveStarted += n => { if (n == 1) _system.ShowImmediately(defs[0].ObstacleLayout); };
            waves.Begin();
            yield return null;
            AssertPositions(_layout1);

            waves.NotifyCleared();
            if (waves.Phase == WavePhase.Resting) _system.SwitchTo(defs[waves.CurrentWave - 1].ObstacleLayout);
            for (int i = 0; i < 15; i++)
            {
                waves.Tick(0.1f);
                _system.Tick(0.1f);
            }
            yield return null;
            Assert.AreEqual(2, waves.CurrentWave);
            Assert.AreEqual(WavePhase.Fighting, waves.Phase);
            AssertPositions(_layout2);
            AssertAllSolid(6);

            Object.DestroyImmediate(defs[0]);
            Object.DestroyImmediate(defs[1]);
        }

        [UnityTest]
        public IEnumerator Dispose_DestroysAllObstacles()
        {
            _system.ShowImmediately(_layout1);
            _system.Dispose();
            yield return null;
            Assert.AreEqual(0, _parent.transform.childCount);
        }
    }
}

using System.Collections;
using NUnit.Framework;
using PictoArcher.Core;
using PictoArcher.Game;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace PictoArcher.Tests.PlayMode
{
    public class ChaserPlayModeTests
    {
        const string ChaserPath = "Assets/_Project/Data/Enemies/Chaser.asset";
        const string SandboxPath = "Assets/_Project/Scenes/Sandbox/Sandbox_B.unity";

        bool _sandboxLoaded;

        sealed class FakePlayer : ITarget, IHittable
        {
            public Vector2 Position { get; set; }
            public bool IsAlive => true;
            public int Hits;
            public void TakeHit(Damage damage) => Hits++;
        }

        static EnemySystem NewSystem(EnemyRegistry registry, FakePlayer player)
        {
            var telegraph = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Enemies/SpawnTelegraph.prefab").GetComponent<SpawnTelegraphView>();
            var settings = AssetDatabase.LoadAssetAtPath<SpawnTelegraphSettings>("Assets/_Project/Data/Enemies/SpawnTelegraphSettings.asset");
            var attack = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Enemies/AttackTelegraph.prefab").GetComponent<AttackTelegraphView>();
            return new EnemySystem(registry, telegraph, settings, attack, new EnemyProjectileSystem(player), player, player);
        }

        [UnityTest]
        public IEnumerator Spawn_WithZeroSeconds_Throws_AndLeavesNoTelegraph()
        {
            var definition = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(ChaserPath);
            var system = NewSystem(new EnemyRegistry(), new FakePlayer());

            Assert.Throws<System.ArgumentOutOfRangeException>(() => system.Spawn(definition, Vector2.zero, 0f));

            Assert.AreEqual(0, system.PendingCount);
            Assert.AreEqual(0, Object.FindObjectsByType<SpawnTelegraphView>(FindObjectsSortMode.None).Length, "例外のあとに予兆が残らない");
            system.Dispose();
            yield return null;
        }

        [UnityTest]
        public IEnumerator Spawn_WithBrokenDefinition_Throws_AndLeavesNoEnemyObject()
        {
            var broken = Object.Instantiate(AssetDatabase.LoadAssetAtPath<EnemyDefinition>(ChaserPath));
            var serialized = new SerializedObject(broken);
            serialized.FindProperty("_moveSpeed").floatValue = -1f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            var registry = new EnemyRegistry();
            var system = NewSystem(registry, new FakePlayer());

            system.Spawn(broken, Vector2.zero, 0.1f);
            Assert.Throws<System.ArgumentOutOfRangeException>(() => system.Tick(0.2f));
            yield return null;   // Destroy は次のフレームで反映される

            Assert.AreEqual(0, Object.FindObjectsByType<EnemyView>(FindObjectsSortMode.None).Length, "作れなかった敵のオブジェクトが残らない");
            Assert.AreEqual(0, registry.Active.Count);
            system.Dispose();
            Object.DestroyImmediate(broken);
        }

        [UnityTest]
        public IEnumerator Chaser_DiesAfterThreeHits_AndRaisesDiedOnce_EvenWithSimultaneousHits()
        {
            var definition = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(ChaserPath);
            var registry = new EnemyRegistry();
            var player = new FakePlayer { Position = new Vector2(50f, 0f) };
            var system = NewSystem(registry, player);
            int damaged = 0, died = 0;
            IEnemyTarget spawned = null;
            system.Damaged += _ => damaged++;
            system.Died += _ => died++;
            system.Spawned += e => spawned = e;

            system.Spawn(definition, Vector2.zero, 0.1f);
            Assert.AreEqual(1, system.PendingCount);
            system.Tick(0.2f);
            Assert.IsNotNull(spawned, "予兆のあとに敵が出る");
            Assert.AreEqual(1, registry.Active.Count);

            spawned.TakeHit(new Damage(10));
            spawned.TakeHit(new Damage(10));
            Assert.AreEqual(0, died);
            spawned.TakeHit(new Damage(10));
            spawned.TakeHit(new Damage(10));   // 同じフレームに2発当たっても Died は1回
            Assert.AreEqual(3, damaged);
            Assert.AreEqual(1, died);
            Assert.IsFalse(spawned.IsAlive);
            Assert.AreEqual(0, registry.Active.Count, "死んだ敵は Registry から外れる");

            system.Dispose();
            yield return null;
        }

        [UnityTest]
        public IEnumerator Sandbox_SpawnsAfterTelegraph_AndEnemyApproachesPlayer()
        {
            _sandboxLoaded = true;
            EditorSceneManager.LoadSceneInPlayMode(SandboxPath, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            // 再生前から開いていた Sandbox のシーンには、Main が Root を加算で足す（Main.LoadRootIfMissing と同じ）
            SceneManager.LoadScene(SceneTransform.RootSceneName, LoadSceneMode.Additive);
            yield return null;
            yield return null;

            yield return new WaitForSeconds(0.3f);
            Assert.AreEqual(0, Object.FindObjectsByType<EnemyView>(FindObjectsSortMode.None).Length, "予兆の間は敵がいない");
            Assert.AreEqual(1, Object.FindObjectsByType<SpawnTelegraphView>(FindObjectsSortMode.None).Length, "赤い円と ! が出ている");

            yield return new WaitForSeconds(0.9f);
            var enemies = Object.FindObjectsByType<EnemyView>(FindObjectsSortMode.None);
            Assert.AreEqual(1, enemies.Length, "予兆のあとに敵が出る");
            Assert.AreEqual(0, Object.FindObjectsByType<SpawnTelegraphView>(FindObjectsSortMode.None).Length, "予兆は消える");

            var enemy = enemies[0];
            float before = ((Vector2)enemy.transform.position).magnitude;
            yield return new WaitForSeconds(1f);
            Assert.Less(((Vector2)enemy.transform.position).magnitude, before - 0.5f, "プレイヤー（原点）へ近づく");
        }

        // Sandbox のシーンを動かしたままにすると、あとのテストに敵や当たり判定が混ざる。失敗したときも Root から始め直して片付ける
        [UnityTearDown]
        public IEnumerator CleanUpSandbox()
        {
            if (!_sandboxLoaded) yield break;
            _sandboxLoaded = false;
            SceneManager.LoadScene(SceneTransform.RootSceneName);
            yield return null;
            yield return null;
        }
    }
}

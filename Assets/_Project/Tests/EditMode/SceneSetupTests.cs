using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using PictoArcher.Game;

namespace PictoArcher.Tests.EditMode
{
    public class SceneSetupTests
    {
        const string SceneFolder = "Assets/_Project/Scenes/";

        [Test]
        public void BuildSettings_ListBootHomeRunInSceneIdOrder()
        {
            var scenes = EditorBuildSettings.scenes;
            CollectionAssert.AreEqual(
                Enum.GetNames(typeof(SceneId)),
                scenes.Select(s => Path.GetFileNameWithoutExtension(s.path)).ToArray());
            Assert.IsTrue(scenes.All(s => s.enabled));
        }

        [Test]
        public void MainPrefab_HasMain() =>
            Assert.IsNotNull(Resources.Load<Main>("Main"));

        [TestCase("Boot", typeof(BootScene))]
        [TestCase("Home", typeof(HomeScene))]
        [TestCase("Run", typeof(RunScene))]
        public void Scene_HasOneRootSceneBaseOfType(string name, Type type)
        {
            var scene = OpenScene(name);
            try
            {
                var roots = scene.GetRootGameObjects()
                    .Select(go => go.GetComponent<SceneBase>())
                    .Where(s => s != null)
                    .ToArray();
                Assert.AreEqual(1, roots.Length);
                Assert.AreEqual(type, roots[0].GetType());
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        [Test]
        public void HomeStartButton_IsWired()
        {
            var scene = OpenScene("Home");
            try
            {
                Assert.IsNotNull(StartButton(scene));
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        // 文字ゼロのルール：開始ボタンは ▶ の絵だけで、文字の部品を持たない。
        [Test]
        public void HomeStartButton_HasNoTextChild()
        {
            var scene = OpenScene("Home");
            try
            {
                var button = StartButton(scene);
                Assert.IsNotNull(button);
                Assert.IsEmpty(button.GetComponentsInChildren<Text>(true));
                Assert.IsEmpty(button.GetComponentsInChildren<TMP_Text>(true));
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        static Scene OpenScene(string name)
        {
            string path = SceneFolder + name + ".unity";
            Assert.IsTrue(File.Exists(path), path + " がありません");
            return EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
        }

        static UiButtonRelay StartButton(Scene scene)
        {
            var home = scene.GetRootGameObjects()
                .Select(go => go.GetComponent<HomeScene>())
                .First(s => s != null);
            return new SerializedObject(home).FindProperty("_startButton").objectReferenceValue as UiButtonRelay;
        }
    }
}

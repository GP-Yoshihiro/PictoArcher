using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using PictoArcher.Game;

namespace PictoArcher.Tests.EditMode
{
    public class SceneSetupTests
    {
        const string SceneFolder = "Assets/_Project/Scenes/";

        static string[] ContentScenes => Enum.GetNames(typeof(SceneId));
        static string[] OverlayScenes => Enum.GetNames(typeof(OverlayId));

        [Test]
        public void BuildSettings_ListRootThenSceneIdsThenOverlayIds()
        {
            var scenes = EditorBuildSettings.scenes;
            var expected = new[] { SceneTransform.RootSceneName }.Concat(ContentScenes).Concat(OverlayScenes).ToArray();
            CollectionAssert.AreEqual(expected, scenes.Select(s => Path.GetFileNameWithoutExtension(s.path)).ToArray());
            Assert.IsTrue(scenes.All(s => s.enabled));
        }

        [Test]
        public void Root_HasMainRootSceneCameraAndEventSystem() => WithScene(SceneTransform.RootSceneName, scene =>
        {
            var roots = scene.GetRootGameObjects();
            Assert.AreEqual(1, roots.Count(go => go.GetComponent<Main>() != null), "Main");
            Assert.AreEqual(1, roots.Count(go => go.GetComponent<RootScene>() != null), "RootScene");
            Assert.AreEqual(1, roots.Sum(go => go.GetComponentsInChildren<Camera>(true).Length), "Camera");
            Assert.AreEqual(1, roots.Sum(go => go.GetComponentsInChildren<EventSystem>(true).Length), "EventSystem");
        });

        [TestCase("Root", typeof(RootScene))]
        [TestCase("Home", typeof(HomeScene))]
        [TestCase("Battle", typeof(BattleScene))]
        [TestCase("Result", typeof(ResultScene))]
        [TestCase("Pause", typeof(PauseScene))]
        public void Scene_HasOneRootSceneBaseOfType(string name, Type type) => WithScene(name, scene =>
        {
            var roots = scene.GetRootGameObjects()
                .Select(go => go.GetComponent<SceneBase>())
                .Where(s => s != null)
                .ToArray();
            Assert.AreEqual(1, roots.Length);
            Assert.AreEqual(type, roots[0].GetType());
        });

        // Root 以外のシーンには、カメラ・AudioListener・EventSystem・Main を置かない（重ねると2つになる）。
        [Test]
        public void ContentAndOverlayScenes_HaveNoCameraListenerEventSystemOrMain()
        {
            foreach (var name in ContentScenes.Concat(OverlayScenes))
            {
                WithScene(name, scene =>
                {
                    var roots = scene.GetRootGameObjects();
                    Assert.AreEqual(0, roots.Sum(go => go.GetComponentsInChildren<Camera>(true).Length), name + " の Camera");
                    Assert.AreEqual(0, roots.Sum(go => go.GetComponentsInChildren<AudioListener>(true).Length), name + " の AudioListener");
                    Assert.AreEqual(0, roots.Sum(go => go.GetComponentsInChildren<EventSystem>(true).Length), name + " の EventSystem");
                    Assert.AreEqual(0, roots.Sum(go => go.GetComponentsInChildren<Main>(true).Length), name + " の Main");
                });
            }
        }

        // 文字ゼロのルール：ボタンは絵だけで、文字の部品を持たない。
        [TestCase("Home", typeof(HomeScene), "_startButton")]
        [TestCase("Battle", typeof(BattleScene), "_pauseButton")]
        [TestCase("Result", typeof(ResultScene), "_homeButton")]
        [TestCase("Result", typeof(ResultScene), "_retryButton")]
        [TestCase("Pause", typeof(PauseScene), "_resumeButton")]
        [TestCase("Pause", typeof(PauseScene), "_homeButton")]
        public void Button_IsWiredAndHasNoText(string sceneName, Type sceneType, string field) => WithScene(sceneName, scene =>
        {
            var owner = scene.GetRootGameObjects()
                .Select(go => go.GetComponent(sceneType))
                .First(c => c != null);
            var button = new SerializedObject(owner).FindProperty(field).objectReferenceValue as UiButtonRelay;
            Assert.IsNotNull(button, sceneName + " の " + field + " がつながっていません");
            Assert.IsNotNull(button.GetComponent<Button>());
            Assert.IsEmpty(button.GetComponentsInChildren<Text>(true));
            Assert.IsEmpty(button.GetComponentsInChildren<TMP_Text>(true));
        });

        // 重ねシーンは下のシーンより前に描き、全面の板で下のボタンを押せないようにする。
        [Test]
        public void PauseCanvas_IsInFrontAndBlocksBelow() => WithScene("Pause", scene =>
        {
            var canvas = scene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<Canvas>(true)).First(c => c.isRootCanvas);
            Assert.AreEqual(RenderMode.ScreenSpaceOverlay, canvas.renderMode);
            Assert.GreaterOrEqual(canvas.sortingOrder, 100);
            var blocker = canvas.GetComponentsInChildren<Image>(true).FirstOrDefault(i => i.GetComponent<Button>() == null && i.raycastTarget
                && i.rectTransform.anchorMin == Vector2.zero && i.rectTransform.anchorMax == Vector2.one);
            Assert.IsNotNull(blocker, "全面の板（anchor 0〜1、Raycast Target）がありません");
        });

        static void WithScene(string name, Action<Scene> check)
        {
            string path = SceneFolder + name + ".unity";
            Assert.IsTrue(File.Exists(path), path + " がありません");
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            try
            {
                check(scene);
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }
    }
}

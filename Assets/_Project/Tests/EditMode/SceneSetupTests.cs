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

        [TestCase("Home", typeof(HomeScene), "_view")]
        [TestCase("Home", typeof(HomeScene), "_icons")]
        [TestCase("Result", typeof(ResultScene), "_clearView")]
        [TestCase("Result", typeof(ResultScene), "_defeatView")]
        [TestCase("Result", typeof(ResultScene), "_icons")]
        [TestCase("Result", typeof(ResultScene), "_retryIcon")]
        [TestCase("Result", typeof(ResultScene), "_homeIcon")]
        public void SceneField_IsWired(string sceneName, Type sceneType, string field) => WithScene(sceneName, scene =>
        {
            var owner = scene.GetRootGameObjects()
                .Select(go => go.GetComponent(sceneType))
                .First(c => c != null);
            Assert.IsNotNull(new SerializedObject(owner).FindProperty(field).objectReferenceValue, sceneName + " の " + field + " がつながっていません");
        });

        // シーンの文字は、数字と記号だけのフォントで出す
        [TestCase("Home")]
        [TestCase("Result")]
        public void SceneTexts_UseDigitFontOnly(string sceneName) => WithScene(sceneName, scene =>
        {
            var texts = scene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<TMP_Text>(true)).ToArray();
            Assert.IsNotEmpty(texts, sceneName + " に数字の文字がありません");
            foreach (var text in texts)
                Assert.AreEqual("Assets/_Project/Fonts/Digits.asset", AssetDatabase.GetAssetPath(text.font), text.name + " のフォント");
        });

        // View の部品がすべてつながっている（null のままだと開いたときに例外になる）
        [TestCase("Home", typeof(HomeView))]
        [TestCase("Result", typeof(ClearView))]
        [TestCase("Result", typeof(DefeatView))]
        public void ViewFields_AreWired(string sceneName, Type viewType) => WithScene(sceneName, scene =>
        {
            var view = scene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren(viewType, true)).Single();
            var it = new SerializedObject(view).GetIterator();
            for (bool enter = true; it.NextVisible(enter); enter = false)
            {
                if (it.propertyType == SerializedPropertyType.ObjectReference && it.name != "m_Script")
                    Assert.IsNotNull(it.objectReferenceValue, sceneName + " の " + viewType.Name + "." + it.name + " がつながっていません");
            }
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

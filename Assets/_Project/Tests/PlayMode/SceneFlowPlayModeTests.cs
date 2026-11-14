using System.Collections;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using PictoArcher.Game;

namespace PictoArcher.Tests.PlayMode
{
    public class SceneFlowPlayModeTests
    {
        const int MaxFrames = 300;

        [UnityTest]
        public IEnumerator Boot_GoesToHome_ThenStartButtonGoesToRun()
        {
            yield return LoadBoot();

            yield return WaitForActiveScene("Home");
            Assert.AreEqual("Home", SceneManager.GetActiveScene().name);

            var relay = Object.FindAnyObjectByType<UiButtonRelay>();
            Assert.IsNotNull(relay);
            relay.GetComponent<Button>().onClick.Invoke();

            yield return WaitForActiveScene("Run");
            Assert.AreEqual("Run", SceneManager.GetActiveScene().name);

            // Run を1フレーム動かす。ここまでに Boot・Home・Run でエラーか例外が出ていたら、テストは失敗になる。
            yield return null;
        }

        [UnityTest]
        public IEnumerator SceneWhoseDisposeThrows_StillMovesToNextScene()
        {
            yield return LoadBoot();
            yield return WaitForActiveScene("Home");

            var scene = SceneManager.CreateScene("DisposeThrows");
            SceneManager.SetActiveScene(scene);
            new GameObject("DisposeThrowsScene", typeof(DisposeThrowsScene));
            LogAssert.Expect(LogType.Exception, new Regex(DisposeThrowsScene.Message));

            yield return WaitForActiveScene("Home");
            Assert.AreEqual("Home", SceneManager.GetActiveScene().name, "Dispose の例外のあと、次のシーンの読み込みが始まっていません。");
            yield return null;
        }

        [UnityTest]
        public IEnumerator SceneOutsideAssetsWithoutSceneBase_OnlyWarns()
        {
            yield return LoadBoot();
            yield return WaitForActiveScene("Home");

            // 保存していない新しいシーン（エディタの「無題」と同じ）。SceneBase がなくてもエラーにしない。
            var scene = SceneManager.CreateScene("Untitled");
            SceneManager.SetActiveScene(scene);
            LogAssert.Expect(LogType.Warning, new Regex("SceneBase"));
            yield return null;
            yield return null;
        }

        /// <summary>
        /// Boot を読み込む。テストの一時シーンは Assets の外にあるので、Main は警告だけを出し、エラーは出さない
        /// （Expect していないエラーと例外は、テストを落とす）。
        /// </summary>
        static IEnumerator LoadBoot()
        {
            SceneManager.LoadScene("Boot");
            yield return null;
        }

        static IEnumerator WaitForActiveScene(string name)
        {
            for (int i = 0; i < MaxFrames && SceneManager.GetActiveScene().name != name; i++)
                yield return null;
        }
    }
}

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
            yield return LoadBootIgnoringTestSceneError();

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
            yield return LoadBootIgnoringTestSceneError();
            yield return WaitForActiveScene("Home");

            var scene = SceneManager.CreateScene("DisposeThrows");
            SceneManager.SetActiveScene(scene);
            new GameObject("DisposeThrowsScene", typeof(DisposeThrowsScene));
            LogAssert.Expect(LogType.Exception, new Regex(DisposeThrowsScene.Message));

            yield return WaitForActiveScene("Home");
            Assert.AreEqual("Home", SceneManager.GetActiveScene().name, "Dispose の例外のあと、次のシーンの読み込みが始まっていません。");
            yield return null;
        }

        /// <summary>
        /// テストの一時シーンには SceneBase がないので、Main がエラーを1回出す。そのエラーだけを見逃し、
        /// Boot を読み込んだあとはエラーを見逃さない（見逃さない間に出たエラーと例外は、Expect していなければテストを落とす）。
        /// </summary>
        static IEnumerator LoadBootIgnoringTestSceneError()
        {
            LogAssert.ignoreFailingMessages = true;
            SceneManager.LoadScene("Boot");
            yield return null;
            LogAssert.ignoreFailingMessages = false;
        }

        static IEnumerator WaitForActiveScene(string name)
        {
            for (int i = 0; i < MaxFrames && SceneManager.GetActiveScene().name != name; i++)
                yield return null;
        }
    }
}

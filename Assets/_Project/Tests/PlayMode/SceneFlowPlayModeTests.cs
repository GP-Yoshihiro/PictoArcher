using System.Collections;
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
            // テストの一時シーンには SceneBase がないので、Main がエラーを1回出す。それは見逃す。
            LogAssert.ignoreFailingMessages = true;

            SceneManager.LoadScene("Boot");
            yield return WaitForActiveScene("Home");
            Assert.AreEqual("Home", SceneManager.GetActiveScene().name);

            var relay = Object.FindAnyObjectByType<UiButtonRelay>();
            Assert.IsNotNull(relay);
            relay.GetComponent<Button>().onClick.Invoke();

            yield return WaitForActiveScene("Run");
            Assert.AreEqual("Run", SceneManager.GetActiveScene().name);
        }

        static IEnumerator WaitForActiveScene(string name)
        {
            for (int i = 0; i < MaxFrames && SceneManager.GetActiveScene().name != name; i++)
                yield return null;
        }
    }
}

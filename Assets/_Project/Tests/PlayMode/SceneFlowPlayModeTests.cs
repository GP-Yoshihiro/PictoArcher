using System.Collections;
using System.Linq;
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
        public IEnumerator Root_GoesToHome_ThenStartGoesToBattle()
        {
            yield return LoadRoot();

            Click<HomeScene>("_startButton");
            yield return WaitForActiveScene("Battle");
            Assert.AreEqual("Battle", SceneManager.GetActiveScene().name);
            yield return WaitForSceneCount(2);
            Assert.AreEqual(2, SceneManager.sceneCount, "Root と Battle だけが残るはずです");

            // Battle を1フレーム動かす。ここまでにエラーか例外が出ていたら、テストは失敗になる。
            yield return null;
        }

        [UnityTest]
        public IEnumerator HostRequestsResult_ThenHomeAndRetryButtonsWork()
        {
            yield return LoadRoot();

            // ↻ は Battle へ
            var first = OpenHostScene("HostRetry");
            yield return WaitUntilInitialized(first);
            first.Context.RequestScene(SceneId.Result);
            yield return WaitForActiveScene("Result");
            Click<ResultScene>("_retryButton");
            yield return WaitForActiveScene("Battle");
            Assert.AreEqual("Battle", SceneManager.GetActiveScene().name);

            // 🏠 は Home へ
            var second = OpenHostScene("HostHome");
            yield return WaitUntilInitialized(second);
            second.Context.RequestScene(SceneId.Result);
            yield return WaitForActiveScene("Result");
            // 結果なしで開いた Result は、どちらの板も出さない
            Assert.IsFalse(View<ClearView>().gameObject.activeSelf);
            Assert.IsFalse(View<DefeatView>().gameObject.activeSelf);
            Click<ResultScene>("_homeButton");
            yield return WaitForActiveScene("Home");
            Assert.AreEqual("Home", SceneManager.GetActiveScene().name);
            yield return WaitForSceneCount(2);
            CollectionAssert.AreEquivalent(new[] { "Root", "Home" }, LoadedSceneNames());
            yield return null;
        }

        [UnityTest]
        public IEnumerator SceneWhoseDisposeThrows_StillMovesToNextScene()
        {
            yield return LoadRoot();

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
            yield return LoadRoot();

            // 保存していない新しいシーン（エディタの「無題」と同じ）。SceneBase がなくてもエラーにしない。
            var scene = SceneManager.CreateScene("Untitled");
            SceneManager.SetActiveScene(scene);
            LogAssert.Expect(LogType.Warning, new Regex("SceneBase"));
            yield return null;
            yield return null;
        }

        [UnityTest]
        public IEnumerator RootInit_RunsBeforeAdoptedScene()
        {
            // 前のテストの Root を、Main が次のフレームを動かす前に消す。
            yield return LoadRoot();
            Object.Destroy(Object.FindAnyObjectByType<Main>().gameObject);
            yield return null;

            // エディタで Home などから再生したときと同じ形：開いているシーンがあり、そこへ Root が加算で足される。
            var scene = SceneManager.CreateScene("RootInitTest");
            SceneManager.SetActiveScene(scene);
            for (int i = SceneManager.sceneCount - 1; i >= 0; i--)
            {
                var other = SceneManager.GetSceneAt(i);
                if (other != scene) yield return SceneManager.UnloadSceneAsync(other);
            }
            bool? rootWasReady = null;
            var host = new GameObject("HostScene").AddComponent<HostScene>();
            host.OnInit = () => rootWasReady = Object.FindAnyObjectByType<RootScene>()?.IsInitialized;

            SceneManager.LoadScene(SceneTransform.RootSceneName, LoadSceneMode.Additive);
            yield return WaitUntilInitialized(host);

            Assert.AreEqual(true, rootWasReady, "開いていたシーンの Init より先に RootScene の Init が呼ばれていません。");
            Assert.AreEqual("RootInitTest", SceneManager.GetActiveScene().name, "開いていたシーンがそのまま中身として始まるはずです");
            yield return null;
        }

        [UnityTest]
        public IEnumerator Battle_PauseOpens_StopsTime_ResumeCloses()
        {
            yield return LoadRoot();
            Click<HomeScene>("_startButton");
            yield return WaitForActiveScene("Battle");

            Click<BattleScene>("_pauseButton");
            yield return WaitForSceneLoaded("Pause");
            Assert.AreEqual(3, SceneManager.sceneCount);
            Assert.AreEqual("Battle", SceneManager.GetActiveScene().name, "重ねシーンはアクティブなシーンを変えません");
            yield return null;
            Assert.AreEqual(0f, Time.timeScale, "Pause の間は時間が止まるはずです");

            Click<PauseScene>("_resumeButton");
            yield return WaitForSceneCount(2);
            yield return null;
            Assert.AreEqual(2, SceneManager.sceneCount);
            Assert.AreEqual(1f, Time.timeScale);
        }

        [UnityTest]
        public IEnumerator Battle_PauseThenHome_LeavesOnlyRootAndHome()
        {
            yield return LoadRoot();
            Click<HomeScene>("_startButton");
            yield return WaitForActiveScene("Battle");
            Click<BattleScene>("_pauseButton");
            yield return WaitForSceneLoaded("Pause");

            Click<PauseScene>("_homeButton");
            yield return WaitForActiveScene("Home");
            yield return WaitForSceneCount(2);
            CollectionAssert.AreEquivalent(new[] { "Root", "Home" }, LoadedSceneNames());
            yield return null;
            Assert.AreEqual(1f, Time.timeScale);
        }

        [UnityTest]
        public IEnumerator Battle_PauseDoubleClick_DoesNotReopenAfterResume()
        {
            yield return LoadRoot();
            Click<HomeScene>("_startButton");
            yield return WaitForActiveScene("Battle");

            // 1回目で Pause を開く。読み込み中（Battle は Tick されない）にもう1回押す。
            Click<BattleScene>("_pauseButton");
            yield return null;
            Click<BattleScene>("_pauseButton");
            yield return WaitForSceneLoaded("Pause");

            Click<PauseScene>("_resumeButton");
            yield return WaitForSceneCount(2);
            for (int i = 0; i < 5; i++) yield return null;
            CollectionAssert.AreEquivalent(new[] { "Root", "Battle" }, LoadedSceneNames(), "閉じたあとに、止まっている間のクリックで Pause が開き直しています");
        }

        [UnityTest]
        public IEnumerator MainDestroyedWhilePaused_RestoresTimeScale()
        {
            yield return LoadRoot();
            Click<HomeScene>("_startButton");
            yield return WaitForActiveScene("Battle");
            Click<BattleScene>("_pauseButton");
            yield return WaitForSceneLoaded("Pause");
            yield return null;
            Assert.AreEqual(0f, Time.timeScale);

            Object.Destroy(Object.FindAnyObjectByType<Main>().gameObject);
            yield return null;
            Assert.AreEqual(1f, Time.timeScale, "Main が消えたら Time.timeScale を 1 に戻すはずです（再生を止めたとき・次のテスト）");
        }

        [UnityTest]
        public IEnumerator HostOpensPause_StopsHostTicks_AndGetsResult()
        {
            yield return LoadRoot();
            var host = OpenHostScene("HostPause");
            yield return WaitUntilInitialized(host);

            Assert.IsTrue(host.Context.OpenOverlay(OverlayId.Pause));
            Assert.IsFalse(host.Context.OpenOverlay(OverlayId.Pause), "同じフレームの2回目は受け付けないはずです");
            yield return WaitForSceneLoaded("Pause");
            yield return null;
            int ticks = host.TickCount;
            yield return null;
            yield return null;
            Assert.AreEqual(ticks, host.TickCount, "Pause の間は下のシーンを Tick しないはずです");

            Assert.IsTrue(host.Context.CloseOverlay(3));
            yield return null;
            yield return null;
            Assert.AreEqual(OverlayId.Pause, host.LastClosed);
            Assert.AreEqual(3, host.LastResult);
            Assert.Greater(host.TickCount, ticks, "閉じたら下のシーンがまた動くはずです");
        }

        [UnityTest]
        public IEnumerator ClosePause_KeepsClockPaused()
        {
            yield return LoadRoot();
            var host = OpenHostScene("HostClock");
            yield return WaitUntilInitialized(host);

            host.Context.Clock.Paused = true;
            host.Context.OpenOverlay(OverlayId.Pause);
            yield return WaitForSceneLoaded("Pause");
            host.Context.CloseOverlay();
            yield return WaitForSceneCount(2);
            yield return null;
            Assert.AreEqual(0f, Time.timeScale, "閉じても GameClock の一時停止は消さないはずです");
            host.Context.Clock.Paused = false;
            yield return null;
        }

        [UnityTest]
        public IEnumerator Clear_ShowsClearView_ThenHomeShowsBestTime()
        {
            yield return LoadRoot();
            Click<HomeScene>("_startButton");
            yield return WaitForActiveScene("Battle");
            Assert.AreEqual("Battle", SceneManager.GetActiveScene().name);

            var host = OpenHostScene("HostClear");
            yield return WaitUntilInitialized(host);
            Assert.IsTrue(host.Context.FinishRun(new RunResult(RunOutcome.Cleared, 50, 1425.4f, 321, WeaponId.Gun, null)));
            yield return WaitForActiveScene("Result");
            Assert.AreEqual("Result", SceneManager.GetActiveScene().name);
            var clear = View<ClearView>();
            Assert.IsTrue(clear.gameObject.activeSelf, "クリアの板が出る");
            Assert.IsFalse(View<DefeatView>().gameObject.activeSelf);
            Assert.AreEqual("23:45", TextOf(clear, "_timeText"));
            Assert.AreEqual("321", TextOf(clear, "_coinText"));

            Click<ResultScene>("_homeButton");
            yield return WaitForActiveScene("Home");
            var home = View<HomeView>();
            Assert.IsTrue(Field<GameObject>(home, "_recordRow").activeSelf);
            Assert.IsTrue(Field<GameObject>(home, "_bestTimeGroup").activeSelf, "クリア済みなら ⏱ 最短タイム");
            Assert.IsFalse(Field<Component>(home, "_bestWaveText").gameObject.activeSelf);
            Assert.AreEqual("23:45", TextOf(home, "_bestTimeText"));
            yield return null;
        }

        [UnityTest]
        public IEnumerator Died_ShowsDefeatView_ThenRetryGoesToBattle()
        {
            yield return LoadRoot();
            var host = OpenHostScene("HostDied");
            yield return WaitUntilInitialized(host);
            Assert.IsTrue(host.Context.FinishRun(new RunResult(RunOutcome.Died, 23, 0f, 5, WeaponId.Laser, new[] { SkillId.Fire })));
            yield return WaitForActiveScene("Result");
            var defeat = View<DefeatView>();
            Assert.IsTrue(defeat.gameObject.activeSelf, "死亡の板が出る");
            Assert.IsFalse(View<ClearView>().gameObject.activeSelf);
            Assert.AreEqual("23/50", TextOf(defeat, "_waveText"));
            Assert.IsNotNull(Field<Image>(defeat, "_weaponIcon").sprite);

            Click<ResultScene>("_retryButton");
            yield return WaitForActiveScene("Battle");
            Assert.AreEqual("Battle", SceneManager.GetActiveScene().name);
            yield return WaitForSceneCount(2);
            CollectionAssert.AreEquivalent(new[] { "Root", "Battle" }, LoadedSceneNames());
            yield return null;
        }

        [UnityTest]
        public IEnumerator Battle_PlayerDies_ShowsDefeatViewWithReachedWave()
        {
            yield return LoadRoot();
            Click<HomeScene>("_startButton");
            yield return WaitForActiveScene("Battle");
            yield return null;
            var battle = Object.FindAnyObjectByType<BattleScene>();
            Field<PlayerSystem>(battle, "_player").TakeHit(new PictoArcher.Core.Damage(99999));

            // 死んでから結果の画面へ移るまで少し待つ（RunDefinition の EndDelaySeconds）
            for (float t = 0f; t < 5f && SceneManager.GetActiveScene().name != "Result"; t += Time.unscaledDeltaTime) yield return null;
            Assert.AreEqual("Result", SceneManager.GetActiveScene().name, "死ぬと結果の画面へ移る");
            yield return null;
            var defeat = View<DefeatView>();
            Assert.IsTrue(defeat.gameObject.activeSelf, "死亡の板が出る");
            Assert.AreEqual("1/50", TextOf(defeat, "_waveText"));
        }

        [UnityTest]
        public IEnumerator Home_FirstTime_HidesRecordRow()
        {
            yield return LoadRoot();
            Assert.IsFalse(Field<GameObject>(View<HomeView>(), "_recordRow").activeSelf, "記録がないときは出さない");
            yield return null;
        }

        static T View<T>() where T : Component
        {
            var view = Object.FindAnyObjectByType<T>(FindObjectsInactive.Include);
            Assert.IsNotNull(view, typeof(T).Name + " がありません");
            return view;
        }

        static T Field<T>(object owner, string field) where T : class =>
            owner.GetType().GetField(field, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(owner) as T;

        // PlayMode のテストは TMP を参照しないので、text はリフレクションで読む
        static string TextOf(object owner, string field)
        {
            var text = Field<Component>(owner, field);
            return (string)text.GetType().GetProperty("text").GetValue(text);
        }

        /// <summary>
        /// Root を読み込み、Home が始まるまで待つ。毎回新しい Main になる。テストの一時シーンは Assets の外にあるので、
        /// Main は警告だけを出し、エラーは出さない（Expect していないエラーと例外は、テストを落とす）。
        /// </summary>
        static IEnumerator LoadRoot()
        {
            SceneManager.LoadScene(SceneTransform.RootSceneName);
            yield return null;
            yield return WaitForActiveScene("Home");
            Assert.AreEqual("Home", SceneManager.GetActiveScene().name);
        }

        /// <summary>テスト用のシーンを作ってアクティブにする（外からの切り替え。今の中身は外される）。</summary>
        static HostScene OpenHostScene(string name)
        {
            var scene = SceneManager.CreateScene(name);
            SceneManager.SetActiveScene(scene);
            return new GameObject("HostScene").AddComponent<HostScene>();
        }

        static void Click<T>(string field) where T : SceneBase
        {
            var owner = Object.FindAnyObjectByType<T>();
            Assert.IsNotNull(owner, typeof(T).Name + " がありません");
            var relay = (UiButtonRelay)typeof(T)
                .GetField(field, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .GetValue(owner);
            relay.GetComponent<Button>().onClick.Invoke();
        }

        static string[] LoadedSceneNames() =>
            Enumerable.Range(0, SceneManager.sceneCount).Select(SceneManager.GetSceneAt).Where(s => s.isLoaded).Select(s => s.name).ToArray();

        static IEnumerator WaitUntilInitialized(HostScene host)
        {
            for (int i = 0; i < MaxFrames && host.Context == null; i++) yield return null;
            Assert.IsNotNull(host.Context, "HostScene が始まりません");
        }

        static IEnumerator WaitForActiveScene(string name)
        {
            for (int i = 0; i < MaxFrames && SceneManager.GetActiveScene().name != name; i++)
                yield return null;
        }

        static IEnumerator WaitForSceneLoaded(string name)
        {
            for (int i = 0; i < MaxFrames && !SceneManager.GetSceneByName(name).isLoaded; i++)
                yield return null;
            Assert.IsTrue(SceneManager.GetSceneByName(name).isLoaded, name + " が読み込まれません");
        }

        static IEnumerator WaitForSceneCount(int count)
        {
            for (int i = 0; i < MaxFrames && SceneManager.sceneCount != count; i++)
                yield return null;
        }
    }
}

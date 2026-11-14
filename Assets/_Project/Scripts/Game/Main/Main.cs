using PictoArcher.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PictoArcher.Game
{
    /// <summary>
    /// Unity の標準ライフサイクルを書く唯一のクラス。今のシーンの SceneBase に Init / Tick / Dispose を順に呼ぶ。
    /// Resources/Main.prefab から、どのシーンを再生しても最初に作られる。static の状態は持たない。
    /// </summary>
    [DefaultExecutionOrder(-10000)]
    public sealed class Main : MonoBehaviour
    {
        const string PrefabPath = "Main";

        SceneFlow<SceneId> _flow;
        GameClock _clock;
        GameContext _context;
        SceneBase _scene;
        Scene _activeScene;
        AsyncOperation _loading;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void CreateInstance()
        {
            var prefab = Resources.Load<Main>(PrefabPath);
            if (prefab == null)
            {
                Debug.LogError("[Main] Resources/" + PrefabPath + ".prefab が見つかりません。");
                return;
            }
            DontDestroyOnLoad(Instantiate(prefab).gameObject);
        }

        void Awake()
        {
            // 全体の準備はここ（BootScene ではない）。どのシーンから再生しても同じになる。
            _clock = new GameClock();
            _flow = new SceneFlow<SceneId>();
            _context = new GameContext(_flow, _clock);
        }

        void Update()
        {
            // 1. 時計
            _clock.Advance(Time.unscaledDeltaTime);

            // 2. シーンの開始（最初のフレーム、読み込みの終わり、外からの読み込み）。開始したフレームは Tick しない。
            bool started = false;
            if (_flow.Phase == SceneFlowPhase.Empty)
            {
                StartActiveScene();
                started = true;
            }
            else if (_flow.Phase == SceneFlowPhase.Loading)
            {
                if (_loading != null && _loading.isDone)
                {
                    _loading = null;
                    StartActiveScene();
                    started = true;
                }
            }
            else if (SceneManager.GetActiveScene() != _activeScene)
            {
                // テストやエディタが SceneManager.LoadScene で直接読み込んだとき（外からの読み込みはテストとエディタのためだけ）。
                // 古いシーンはもう壊れているので Dispose は呼ばない。
                StartActiveScene();
                started = true;
            }

            // 3. 今のシーンを Tick
            if (!started && _flow.Phase == SceneFlowPhase.Running && _scene != null)
                _scene.Tick(Time.deltaTime);

            // 4. 切り替えの依頼があれば、今のシーンを片付けて読み込みを始める
            if (_flow.TryBeginLoad(out var next))
            {
                if (_scene != null) _scene.Dispose();
                _scene = null;
                _loading = SceneManager.LoadSceneAsync(next.ToString(), LoadSceneMode.Single);
            }

            // 5. 時間の倍率
            Time.timeScale = _clock.EffectiveTimeScale;
        }

        void FixedUpdate()
        {
            if (_flow.Phase == SceneFlowPhase.Running && _scene != null)
                _scene.FixedTick(Time.fixedDeltaTime);
        }

        void LateUpdate()
        {
            if (_flow.Phase == SceneFlowPhase.Running && _scene != null)
                _scene.LateTick(Time.deltaTime);
        }

        void OnDestroy()
        {
            if (_flow != null && _flow.Phase == SceneFlowPhase.Running && _scene != null)
                _scene.Dispose();
        }

        void StartActiveScene()
        {
            // Init の中の依頼（Boot → Home）を受け付けるため、Init より先に「動いている」にする。
            if (_flow.Phase != SceneFlowPhase.Running) _flow.EnterRunning();

            var active = SceneManager.GetActiveScene();
            _activeScene = active;
            _scene = null;

            SceneBase found = null;
            int count = 0;
            var roots = active.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                var candidate = roots[i].GetComponent<SceneBase>();
                if (candidate == null) continue;
                found = candidate;
                count++;
            }

            if (count != 1)
            {
                Debug.LogError("[Main] シーン「" + active.name + "」の一番上の階層に SceneBase が " + count + " 個あります。ちょうど1つにしてください。");
                return;
            }

            _scene = found;
            _scene.Init(_context);
        }
    }
}

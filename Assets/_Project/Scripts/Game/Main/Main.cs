using PictoArcher.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PictoArcher.Game
{
    /// <summary>
    /// Unity の標準ライフサイクルを書く唯一のクラス。Root.unity に置く。時計を進め、SceneTransform を呼び、Time.timeScale を決める。
    /// Root は消えないので DontDestroyOnLoad は要らない。static の状態は持たない。
    /// </summary>
    [DefaultExecutionOrder(-10000)]
    public sealed class Main : MonoBehaviour
    {
        GameClock _clock;
        SceneTransform _transform;

        /// <summary>エディタで Root 以外のシーンから再生したとき、Root を足す（ビルドは Root から始まるので何もしない）。</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void LoadRootIfMissing()
        {
            if (SceneManager.GetSceneByName(SceneTransform.RootSceneName).isLoaded) return;
            SceneManager.LoadScene(SceneTransform.RootSceneName, LoadSceneMode.Additive);
        }

        void Awake()
        {
            _clock = new GameClock();
            var flow = new SceneFlow<SceneId, OverlayId>();
            var context = new GameContext(flow, _clock);
            _transform = new SceneTransform(flow, context, gameObject.scene);
        }

        void Update()
        {
            // 1. 時計　2〜4. シーン（始める → Tick → 依頼）　5. 時間の倍率
            _clock.Advance(Time.unscaledDeltaTime);
            _transform.Update(Time.deltaTime, Time.unscaledDeltaTime);
            Time.timeScale = _transform.FreezesTime ? 0f : _clock.EffectiveTimeScale;
        }

        void FixedUpdate() => _transform.FixedTick(Time.fixedDeltaTime);

        void LateUpdate() => _transform.LateTick(Time.deltaTime, Time.unscaledDeltaTime);

        void OnDestroy()
        {
            _transform?.Dispose();
            // Pause 中に再生を止めても、次の再生やテストに 0 を持ち越さない。
            Time.timeScale = 1f;
        }
    }
}

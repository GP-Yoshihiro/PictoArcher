using System;
using System.Collections.Generic;
using PictoArcher.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PictoArcher.Game
{
    /// <summary>
    /// シーンの管理。消えない Root の上に中身のシーン（SceneId）を加算で入れ替え、その上に重ねシーン（OverlayId）を積む。
    /// 各 SceneBase の Init / Tick / Dispose を呼ぶ。Main と並んで SceneManager を使ってよい唯一のクラス。
    /// </summary>
    public sealed class SceneTransform
    {
        public const string RootSceneName = "Root";

        readonly SceneFlow<SceneId, OverlayId> _flow;
        readonly GameContext _context;
        readonly Scene _root;

        readonly List<SceneBase> _overlays = new List<SceneBase>();
        readonly List<Scene> _overlayScenes = new List<Scene>();
        readonly List<SceneBase> _startedThisFrame = new List<SceneBase>();

        SceneBase _rootScene;
        SceneBase _content;
        Scene _contentScene;
        bool _started;

        AsyncOperation _loading;
        string _loadingName;
        bool _loadingOverlay;

        public SceneTransform(SceneFlow<SceneId, OverlayId> flow, GameContext context, Scene root)
        {
            _flow = flow;
            _context = context;
            _root = root;
        }

        /// <summary>中身のシーンが止まっている（重ねシーンが止めている）か、読み込み中なら true。Main が Time.timeScale を 0 にする。</summary>
        public bool FreezesTime => !_started || _loading != null || (_overlays.Count > 0 && LowestRunningLayer() >= 0);

        /// <summary>毎フレーム1回、Main.Update から呼ぶ。始める → 動かす → 依頼を1つ処理する、の順。</summary>
        public void Update(float deltaTime, float unscaledDeltaTime)
        {
            _startedThisFrame.Clear();

            // 1. 始める（最初のフレーム、読み込みの終わり、外からのアクティブなシーンの変更）
            if (!_started) StartUp();
            else if (_loading != null) { if (_loading.isDone) FinishLoading(); }
            else if (SceneManager.GetActiveScene() != ExpectedActiveScene()) AdoptExternalScene();

            // 2. 動かす範囲を下から Tick
            if (_loading == null) TickLayers(deltaTime, unscaledDeltaTime, late: false);
            else TickOne(_rootScene, unscaledDeltaTime, late: false);

            // 3. 依頼を1つ処理する
            if (_loading == null && _flow.TryBegin(out var request)) Begin(request);
        }

        /// <summary>中身のシーンだけに呼ぶ（Time.timeScale が 0 のあいだ Unity は FixedUpdate を呼ばない）。</summary>
        public void FixedTick(float fixedDeltaTime)
        {
            if (_loading != null || !ContentRuns()) return;
            if (IsAlive(_content) && !_startedThisFrame.Contains(_content)) _content.FixedTick(fixedDeltaTime);
        }

        public void LateTick(float deltaTime, float unscaledDeltaTime)
        {
            if (!_started) return;
            if (_loading == null) TickLayers(deltaTime, unscaledDeltaTime, late: true);
            else TickOne(_rootScene, unscaledDeltaTime, late: true);
        }

        /// <summary>ゲームの終わり（Root が消えるとき）に呼ぶ。上から Dispose し、Root は最後。シーンは外さない。</summary>
        public void Dispose()
        {
            for (int i = _overlays.Count - 1; i >= 0; i--) SafeDispose(_overlays[i]);
            _overlays.Clear();
            _overlayScenes.Clear();
            SafeDispose(_content);
            _content = null;
            SafeDispose(_rootScene);
            _rootScene = null;
        }

        // ---- 始める ----

        void StartUp()
        {
            _started = true;
            _flow.EnterRunning();

            // Root の Init は、どこから再生してもほかのどのシーンより先。
            var root = FindSingleSceneBase(_root, warnIfOutsideAssets: false) as RootScene;
            if (root == null)
            {
                Debug.LogError("[SceneTransform] Root シーンの一番上の階層に RootScene がちょうど1つありません。");
                return;
            }
            _rootScene = root;
            _startedThisFrame.Add(root);
            root.Init(_context);

            var active = SceneManager.GetActiveScene();
            if (active != _root) StartContent(active);
            else _flow.Request(root.FirstScene);
        }

        void FinishLoading()
        {
            _loading = null;
            var scene = FindLoadedScene(_loadingName);
            if (_loadingOverlay) FinishOverlay(scene);
            else
            {
                if (scene.IsValid()) SceneManager.SetActiveScene(scene);
                _flow.EnterRunning();
                StartContent(scene);
            }
        }

        void StartContent(Scene scene)
        {
            _contentScene = scene;
            _content = scene.IsValid() ? FindSingleSceneBase(scene, warnIfOutsideAssets: true) : null;
            if (_content == null) return;
            _startedThisFrame.Add(_content);
            _content.Init(_context);
        }

        void FinishOverlay(Scene scene)
        {
            var overlay = scene.IsValid() ? FindSingleSceneBase(scene, warnIfOutsideAssets: false) : null;
            if (overlay == null)
            {
                // エラーは FindSingleSceneBase が出している。重ねシーンを外して取り消す。
                if (scene.IsValid()) Unload(scene);
                _flow.CancelOverlayOpen();
                return;
            }
            _overlays.Add(overlay);
            _overlayScenes.Add(scene);
            _flow.EnterRunning();
            _startedThisFrame.Add(overlay);
            overlay.Init(_context);
        }

        /// <summary>テストやエディタが外からアクティブなシーンを変えたとき。今の中身と重ねシーンを片付け、新しいシーンを中身として始める。</summary>
        void AdoptExternalScene()
        {
            var active = SceneManager.GetActiveScene();
            CloseAllOverlays();
            var old = _contentScene;
            SafeDispose(_content);
            _content = null;
            _contentScene = default;
            if (old.IsValid() && old.isLoaded && old != active && old != _root) Unload(old);
            _flow.Restart();
            if (active != _root) StartContent(active);
        }

        // ---- 依頼 ----

        void Begin(SceneRequest<SceneId, OverlayId> request)
        {
            switch (request.Kind)
            {
                case SceneRequestKind.Change: BeginChange(request.Scene); break;
                case SceneRequestKind.OpenOverlay: BeginOpen(request.Overlay); break;
                case SceneRequestKind.CloseOverlay: Close(request.Overlay, request.Result); break;
            }
        }

        void BeginChange(SceneId next)
        {
            // Dispose が例外を出しても読み込みは必ず始める（止めると、どのシーンにも進めなくなる）。
            CloseAllOverlays();
            SafeDispose(_content);
            _content = null;
            var old = _contentScene;
            _contentScene = default;
            SceneManager.SetActiveScene(_root);
            if (old.IsValid() && old.isLoaded && old != _root) Unload(old);

            _loadingName = next.ToString();
            _loadingOverlay = false;
            _loading = SceneManager.LoadSceneAsync(_loadingName, LoadSceneMode.Additive);
            if (_loading == null)
            {
                Debug.LogError("[SceneTransform] シーン「" + _loadingName + "」を読み込めません。Build Settings に入っているか確かめてください。Root だけで動きます。");
                _flow.EnterRunning();
            }
        }

        void BeginOpen(OverlayId overlay)
        {
            _loadingName = overlay.ToString();
            _loadingOverlay = true;
            _loading = SceneManager.LoadSceneAsync(_loadingName, LoadSceneMode.Additive);
            if (_loading == null)
            {
                Debug.LogError("[SceneTransform] 重ねシーン「" + _loadingName + "」を読み込めません。Build Settings に入っているか確かめてください。開くのを取り消します。");
                _flow.CancelOverlayOpen();
            }
        }

        void Close(OverlayId overlay, int result)
        {
            int top = _overlays.Count - 1;
            if (top < 0) return;
            CloseOverlayAt(top);

            // すぐ下のシーン（重ねシーンか中身のシーン）に知らせる。例外は記録して先へ進む。
            var below = top - 1 >= 0 ? _overlays[top - 1] : _content;
            if (!IsAlive(below)) return;
            try
            {
                below.OnOverlayClosed(overlay, result);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        void CloseAllOverlays()
        {
            for (int i = _overlays.Count - 1; i >= 0; i--) CloseOverlayAt(i);
        }

        void CloseOverlayAt(int index)
        {
            var overlay = _overlays[index];
            var scene = _overlayScenes[index];
            _overlays.RemoveAt(index);
            _overlayScenes.RemoveAt(index);
            SafeDispose(overlay);
            if (scene.IsValid() && scene.isLoaded) Unload(scene);
        }

        // ---- 動かす ----

        /// <summary>動いている一番下の層。-1 が中身のシーン、0 以上が重ねシーンの番号。</summary>
        int LowestRunningLayer()
        {
            int layer = _overlays.Count - 1;
            while (layer >= 0 && IsAlive(_overlays[layer]) && _overlays[layer].KeepsBelowRunning) layer--;
            return layer;
        }

        bool ContentRuns() => _overlays.Count == 0 || LowestRunningLayer() < 0;

        void TickLayers(float deltaTime, float unscaledDeltaTime, bool late)
        {
            TickOne(_rootScene, unscaledDeltaTime, late);
            int lowest = LowestRunningLayer();
            if (lowest < 0) TickOne(_content, deltaTime, late);
            for (int i = Math.Max(lowest, 0); i < _overlays.Count; i++) TickOne(_overlays[i], unscaledDeltaTime, late);
        }

        void TickOne(SceneBase scene, float deltaTime, bool late)
        {
            if (!IsAlive(scene) || _startedThisFrame.Contains(scene)) return;
            if (late) scene.LateTick(deltaTime);
            else scene.Tick(deltaTime);
        }

        // ---- 道具 ----

        Scene ExpectedActiveScene() => _contentScene.IsValid() && _contentScene.isLoaded ? _contentScene : _root;

        static bool IsAlive(SceneBase scene) => scene != null; // 壊れた MonoBehaviour も null と比べて true になる

        static void SafeDispose(SceneBase scene)
        {
            if (!IsAlive(scene)) return; // もう壊れているシーン（外から読み込まれて消えたもの）には呼ばない
            try
            {
                scene.Dispose();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        static void Unload(Scene scene)
        {
            // 外し終わるまでの数フレームも見えたり押せたりしないよう、先にルートを非表示にする。
            var roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++) roots[i].SetActive(false);
            SceneManager.UnloadSceneAsync(scene);
        }

        /// <summary>読み込み終わったシーンを、新しいもの（後ろ）から探す。外している途中の同じ名前のシーンと取り違えないため。</summary>
        static Scene FindLoadedScene(string name)
        {
            for (int i = SceneManager.sceneCount - 1; i >= 0; i--)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.name == name && scene.isLoaded) return scene;
            }
            Debug.LogError("[SceneTransform] 読み込んだはずのシーン「" + name + "」が見つかりません。");
            return default;
        }

        static SceneBase FindSingleSceneBase(Scene scene, bool warnIfOutsideAssets)
        {
            SceneBase found = null;
            int count = 0;
            var roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                var candidate = roots[i].GetComponent<SceneBase>();
                if (candidate == null) continue;
                found = candidate;
                count++;
            }

            if (count == 0 && warnIfOutsideAssets && !scene.path.StartsWith("Assets/"))
            {
                // 保存していない無題のシーンや、テストの一時シーン。エラーにはしない。
                Debug.LogWarning("[SceneTransform] シーン「" + scene.name + "」は Assets の中に保存されておらず、SceneBase もないので、何も動きません。");
                return null;
            }
            if (count != 1)
            {
                Debug.LogError("[SceneTransform] シーン「" + scene.name + "」の一番上の階層に SceneBase が " + count + " 個あります。ちょうど1つにしてください。");
                return null;
            }
            return found;
        }
    }
}

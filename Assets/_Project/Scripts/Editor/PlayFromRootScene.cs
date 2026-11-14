using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PictoArcher.Editor
{
    /// <summary>
    /// エディタの ▶ を、どのシーンを開いていても Root（Build の 0 番）から始める。ビルドはもともと Build の 0 番から始まる。
    /// 例外は Sandbox のシーン（Scenes/Sandbox/）とテストの一時シーン。これらは開いているシーンのまま始め、Main が Root を加算で足す。
    /// 開いているシーンは再生中もエディタに残り、止めると元に戻る。
    /// </summary>
    [InitializeOnLoad]
    public static class PlayFromRootScene
    {
        public const string SandboxFolder = "Assets/_Project/Scenes/Sandbox/";
        const string TestSceneNamePrefix = "InitTestScene";

        static PlayFromRootScene()
        {
            EditorApplication.playModeStateChanged += change =>
            {
                if (change == PlayModeStateChange.ExitingEditMode) Refresh();
            };
            EditorSceneManager.activeSceneChangedInEditMode += (_, _) => Refresh();
            EditorSceneManager.sceneSaved += _ => Refresh();
            EditorApplication.delayCall += Refresh;
        }

        /// <summary>再生を始めるシーンのパス。null なら開いているシーンのまま始める。</summary>
        public static string StartScenePath(string activeScenePath, string activeSceneName, string rootScenePath)
        {
            if (string.IsNullOrEmpty(rootScenePath)) return null;
            if (activeSceneName.StartsWith(TestSceneNamePrefix, StringComparison.Ordinal)) return null;
            if (activeScenePath.StartsWith(SandboxFolder, StringComparison.Ordinal)) return null;
            return rootScenePath;
        }

        static void Refresh()
        {
            if (EditorApplication.isPlaying) return;
            var scenes = EditorBuildSettings.scenes;
            var root = scenes.Length > 0 ? scenes[0].path : "";
            var active = EditorSceneManager.GetActiveScene();
            // テストを batch mode で動かすときは触らない（テストが自分でシーンを用意する）。
            var start = Application.isBatchMode ? null : StartScenePath(active.path, active.name, root);
            EditorSceneManager.playModeStartScene = start == null ? null : AssetDatabase.LoadAssetAtPath<SceneAsset>(start);
        }
    }
}

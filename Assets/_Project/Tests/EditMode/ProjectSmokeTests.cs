using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace PictoArcher.Tests.EditMode
{
    public class ProjectSmokeTests
    {
        [Test]
        public void UnityVersion_IsPinned() =>
            Assert.AreEqual("6000.5.10f1", Application.unityVersion);

        [Test]
        public void Screen_IsPortraitWindow540x960()
        {
            // 設計書 第3版 §11：縦長 9:16 のウィンドウ（基準 270×480 の2倍）。全画面は整数倍で拡大し、余りは黒い帯。
            Assert.AreEqual(540, PlayerSettings.defaultScreenWidth);
            Assert.AreEqual(960, PlayerSettings.defaultScreenHeight);
            Assert.AreEqual(FullScreenMode.Windowed, PlayerSettings.fullScreenMode);
            Assert.AreEqual(UIOrientation.Portrait, PlayerSettings.defaultInterfaceOrientation);
        }

        [Test]
        public void Layers_AreDefined()
        {
            Assert.AreEqual(6, LayerMask.NameToLayer("Player"));
            Assert.AreEqual(7, LayerMask.NameToLayer("Enemy"));
            Assert.AreEqual(8, LayerMask.NameToLayer("PlayerShot"));
            Assert.AreEqual(9, LayerMask.NameToLayer("Wall"));
        }

        [Test]
        public void PlayerShot_CollidesOnlyWithEnemyAndWall()
        {
            int shot = LayerMask.NameToLayer("PlayerShot");
            Assert.IsFalse(Physics2D.GetIgnoreLayerCollision(shot, LayerMask.NameToLayer("Enemy")));
            Assert.IsFalse(Physics2D.GetIgnoreLayerCollision(shot, LayerMask.NameToLayer("Wall")));
            Assert.IsTrue(Physics2D.GetIgnoreLayerCollision(shot, LayerMask.NameToLayer("Player")));
            Assert.IsTrue(Physics2D.GetIgnoreLayerCollision(shot, shot));
        }

        [Test]
        public void MainCamera_IsPixelPerfect270x480()
        {
            var go = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/System/MainCamera.prefab");
            Assert.IsNotNull(go);
            Assert.IsTrue(go.GetComponent<Camera>().orthographic);
            var ppc = go.GetComponent<PixelPerfectCamera>();
            Assert.AreEqual(16, ppc.assetsPPU);
            Assert.AreEqual(270, ppc.refResolutionX);
            Assert.AreEqual(480, ppc.refResolutionY);
            Assert.AreEqual(15f, go.GetComponent<Camera>().orthographicSize, "480 ÷ 2 ÷ 16 = 15");
            Assert.AreEqual(PixelPerfectCamera.CropFrame.Windowbox, ppc.cropFrame, "全画面では整数倍で拡大し、余りを黒い帯にする");
            Assert.AreEqual(PixelPerfectCamera.GridSnapping.UpscaleRenderTexture, ppc.gridSnapping);
        }

        [Test]
        public void ClaudeMd_HasFourRules()
        {
            var text = File.ReadAllText("CLAUDE.md");
            StringAssert.Contains("## 文字ゼロのルール", text);
            StringAssert.Contains("## フォルダ構成", text);
            StringAssert.Contains("## テストの書き方", text);
            StringAssert.Contains("## シーンとブランチ", text);
        }
    }
}

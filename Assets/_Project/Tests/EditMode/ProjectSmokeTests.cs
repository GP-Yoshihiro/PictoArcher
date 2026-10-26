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
        public void DefaultResolution_Is1920x1080()
        {
            Assert.AreEqual(1920, PlayerSettings.defaultScreenWidth);
            Assert.AreEqual(1080, PlayerSettings.defaultScreenHeight);
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
        public void MainCamera_IsPixelPerfect480x270()
        {
            var go = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/MainCamera.prefab");
            Assert.IsNotNull(go);
            Assert.IsTrue(go.GetComponent<Camera>().orthographic);
            var ppc = go.GetComponent<PixelPerfectCamera>();
            Assert.AreEqual(16, ppc.assetsPPU);
            Assert.AreEqual(480, ppc.refResolutionX);
            Assert.AreEqual(270, ppc.refResolutionY);
            Assert.AreEqual(PixelPerfectCamera.CropFrame.StretchFill, ppc.cropFrame);
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

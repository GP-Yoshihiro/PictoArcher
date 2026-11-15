using System.IO;
using NUnit.Framework;
using PictoArcher.Editor;
using UnityEditor;
using UnityEngine;

namespace PictoArcher.Tests.EditMode
{
    /// <summary>
    /// ドット絵（キャラとフリー素材）の画像が、Point・圧縮なし・ミップマップなし、初回だけ Sprite・PPU 16 で取り込まれるか。
    /// UI の絵（Art/UI）はドット絵ではないので対象外。
    /// </summary>
    public class PixelArtImportTests
    {
        const string TestFolderName = "_ImportTest";

        [TestCase("Assets/_Project/Art/Characters")]
        [TestCase("Assets/_Project/Art/Tiles")]
        [TestCase("Assets/_Project/Art/Effects")]
        public void PixelArt_IsPointUncompressedSprite16Ppu(string artFolder) =>
            WithTestImage(artFolder, imp =>
            {
                Assert.AreEqual(TextureImporterType.Sprite, imp.textureType);
                Assert.AreEqual(FilterMode.Point, imp.filterMode);
                Assert.AreEqual(TextureImporterCompression.Uncompressed, imp.textureCompression);
                Assert.AreEqual(16f, imp.spritePixelsPerUnit);
                Assert.IsFalse(imp.mipmapEnabled);
            });

        // 切り分けや PPU を後から変えても、取り込み直しで消えない
        [Test]
        public void SpriteSettings_AreKeptOnReimport() =>
            WithTestImage("Assets/_Project/Art/Characters", imp =>
            {
                imp.spritePixelsPerUnit = 32f;
                imp.filterMode = FilterMode.Bilinear;
                imp.SaveAndReimport();
                Assert.AreEqual(32f, imp.spritePixelsPerUnit);
                Assert.AreEqual(FilterMode.Point, imp.filterMode, "Point は毎回そろえる");
            });

        [Test]
        public void UiArt_IsNotForcedToPoint() =>
            WithTestImage("Assets/_Project/Art/UI", imp =>
                Assert.AreNotEqual(FilterMode.Point, imp.filterMode));

        [TestCase("Assets/_Project/Art/Characters/a.png", true)]
        [TestCase("Assets/_Project/Art/Tiles/sub/b.png", true)]
        [TestCase("Assets/_Project/Art/Effects/c.png", true)]
        [TestCase("Assets/_Project/Art/UI/d.png", false)]
        [TestCase("Assets/_Project/Art/CharactersX/e.png", false)]
        [TestCase("Assets/_Project/Fonts/f.png", false)]
        public void IsPixelArtPath(string path, bool expected) =>
            Assert.AreEqual(expected, PixelArtImportPostprocessor.IsPixelArtPath(path));

        static void WithTestImage(string artFolder, System.Action<TextureImporter> check)
        {
            var dir = artFolder + "/" + TestFolderName;
            var path = dir + "/dot.png";
            Directory.CreateDirectory(dir);
            var tex = new Texture2D(2, 2);
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            try
            {
                AssetDatabase.ImportAsset(path);
                check((TextureImporter)AssetImporter.GetAtPath(path));
            }
            finally
            {
                AssetDatabase.DeleteAsset(dir);
            }
        }
    }
}

using System;
using UnityEditor;
using UnityEngine;

namespace PictoArcher.Editor
{
    /// <summary>
    /// ドット絵（キャラとフリー素材）の画像の取り込み設定をそろえる。
    /// 毎回：Filter Mode = Point、圧縮なし、ミップマップなし。
    /// 初めて取り込むときだけ：Texture Type = Sprite、Pixels Per Unit = 16（切り分けや PPU を後から変えても消えないように）。
    /// UI の絵（Art/UI）はドット絵ではない（Bilinear）ので触らない。
    /// </summary>
    public sealed class PixelArtImportPostprocessor : AssetPostprocessor
    {
        const float PixelsPerUnit = 16f;

        static readonly string[] PixelArtFolders =
        {
            "Assets/_Project/Art/Characters/",
            "Assets/_Project/Art/Tiles/",
            "Assets/_Project/Art/Effects/",
        };

        public static bool IsPixelArtPath(string assetPath)
        {
            foreach (var folder in PixelArtFolders)
                if (assetPath.StartsWith(folder, StringComparison.Ordinal)) return true;
            return false;
        }

        void OnPreprocessTexture()
        {
            if (!IsPixelArtPath(assetPath)) return;
            var importer = (TextureImporter)assetImporter;
            if (importer.importSettingsMissing)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spritePixelsPerUnit = PixelsPerUnit;
            }
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
        }
    }
}

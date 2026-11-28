using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace PictoArcher.Tests.EditMode
{
    /// <summary>
    /// 文字ゼロのルール（CLAUDE.md）を確かめる。フォントアセットには数字と記号だけを登録し、
    /// シーン・プレハブ・アセットに保存された文字列にも文字を残さない。
    /// </summary>
    public class NoTextGuardTests
    {
        const string FontPath = "Assets/_Project/Fonts/Digits.asset";
        const string Required = "0123456789+-×/%:";

        [Test]
        public void DigitFont_HasOnlyDigitsAndSymbols()
        {
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            Assert.IsNotNull(font, $"{FontPath} がありません");
            Assert.AreEqual(AtlasPopulationMode.Static, font.atlasPopulationMode, "実行中に文字が足されないよう Static にする");
            Assert.IsEmpty(font.fallbackFontAssetTable ?? new List<TMP_FontAsset>(), "フォールバックから文字が出ないよう空にする");
            foreach (var r in Required)
                Assert.IsTrue(font.characterLookupTable.ContainsKey(r), $"missing {r}");
            // UI は Bilinear の普通の2Dの絵なので、フォントも SDF で滑らかに出す（ピクセルフォントではない）
            Assert.AreEqual(GlyphRenderMode.SDFAA, font.atlasRenderMode);
            Assert.AreEqual(FilterMode.Bilinear, font.atlasTexture.filterMode);
        }

        // ほかのフォントアセット（TMP の基本リソースの LiberationSans など）が残っていると、そちらを選べてしまう
        [Test]
        public void AllFontAssets_HaveNoLetterGlyphs()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:TMP_FontAsset"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
                foreach (var c in font.characterTable)
                    Assert.IsFalse(IsLetter(c.unicode), $"{path}: letter glyph U+{c.unicode:X4}");
            }
        }

        [Test]
        public void TmpDefaultFont_IsDigitFont()
        {
            Assert.AreEqual(FontPath, AssetDatabase.GetAssetPath(TMP_Settings.defaultFontAsset));
            Assert.IsEmpty(TMP_Settings.fallbackFontAssets ?? new List<TMP_FontAsset>());
        }

        // 「New Text」などが保存されたまま残るのを防ぐ
        [Test]
        public void SerializedTexts_ContainNoLetters()
        {
            var files = Directory.GetFiles("Assets/_Project", "*.*", SearchOption.AllDirectories)
                .Where(p => p.EndsWith(".unity") || p.EndsWith(".prefab") || p.EndsWith(".asset"));
            foreach (var path in files)
            foreach (var line in File.ReadLines(path))
            {
                var t = line.TrimStart();
                if (!t.StartsWith("m_text:") && !t.StartsWith("m_Text:")) continue;
                var value = t.Substring(t.IndexOf(':') + 1);
                Assert.IsFalse(value.Any(char.IsLetter), $"{path}: {t}");
            }
        }

        [Test]
        public void Guard_DetectsLetters()
        {
            Assert.IsTrue(" New Text".Any(char.IsLetter));
            Assert.IsTrue(IsLetter('あ') && IsLetter('漢') && IsLetter('A'));
            Assert.IsFalse(" 23/50 ×2 +20%".Any(char.IsLetter));
        }

        static bool IsLetter(uint unicode) => unicode <= 0xFFFF
            ? char.IsLetter((char)unicode)
            : char.IsLetter(char.ConvertFromUtf32((int)unicode), 0);
    }
}

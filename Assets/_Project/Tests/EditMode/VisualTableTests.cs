using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using PictoArcher.Game;
using UnityEditor;
using UnityEngine;

namespace PictoArcher.Tests.EditMode
{
    /// <summary>
    /// 見た目の表（Data/Visuals）と、表を指す敵の定義（Data/Enemies）を確かめる。
    /// 空の枠は失敗にする。まだ仮の絵（Art/Placeholder）を指す枠は、一覧をテストの出力に出すだけ。
    /// </summary>
    public class VisualTableTests
    {
        const string PlaceholderFolder = "Assets/_Project/Art/Placeholder/";
        static readonly string[] TableFolders = { "Assets/_Project/Data/Visuals", "Assets/_Project/Data/Enemies" };

        [Test] public void Tables_Exist()
        {
            Assert.IsNotEmpty(FindTables<CharacterVisual>(), "Data/Visuals にキャラの表がありません");
            Assert.AreEqual(1, FindTables<PlayerVisual>().Count, "PlayerVisual はちょうど1つ置きます");
            Assert.AreEqual(1, FindTables<FieldVisual>().Count, "FieldVisual はちょうど1つ置きます");
            Assert.AreEqual(1, FindTables<UiIconVisual>().Count, "UiIconVisual はちょうど1つ置きます");
            Assert.IsNotEmpty(FindTables<SpriteVisual>(), "Data/Visuals に弾・障害物・エフェクトの表がありません");
        }

        [Test] public void Tables_HaveNoEmptySlots()
        {
            var problems = new List<string>();
            foreach (var table in FindTables<ScriptableObject>())
            {
                var path = AssetDatabase.GetAssetPath(table);
                var property = new SerializedObject(table).GetIterator();
                while (property.Next(true))
                {
                    if (property.propertyPath.StartsWith("m_")) continue;   // Unity が持つ項目は見ない
                    if (table is EnemyDefinition && property.propertyPath != "_visual") continue;   // 敵の定義は見た目の枠だけを見る
                    if (property.propertyType == SerializedPropertyType.ObjectReference && property.objectReferenceValue == null)
                        problems.Add($"{path}：{property.propertyPath} が空です。絵（まだなければ Art/Placeholder の仮の絵）をアタッチしてください");
                    else if (property.name == "_frames" && property.isArray && property.arraySize == 0)
                        problems.Add($"{path}：{property.propertyPath} にコマが1枚もありません");
                    else if (property.name == "_framesPerSecond" && property.floatValue <= 0f)
                        problems.Add($"{path}：{property.propertyPath} は 0 より大きくしてください");
                }
            }
            Assert.IsEmpty(problems, string.Join("\n", problems));
        }

        [Test] public void FieldVisual_HasOneLightingPerStage()
        {
            foreach (var field in FindTables<FieldVisual>())
            {
                var lighting = new SerializedObject(field).FindProperty("_lighting");
                Assert.AreEqual(FieldVisual.LightingStages, lighting.arraySize,
                    $"{AssetDatabase.GetAssetPath(field)}：照明は {FieldVisual.WavesPerLightingStage} ウェーブごとに1つ、{FieldVisual.LightingStages} つ並べます");
            }
        }

        // 失敗にはしない。絵の差し替えがどこまで進んだかを見るための一覧
        [Test] public void ListSlotsStillUsingPlaceholders()
        {
            var slots = new List<string>();
            foreach (var table in FindTables<ScriptableObject>())
            {
                var path = AssetDatabase.GetAssetPath(table);
                var property = new SerializedObject(table).GetIterator();
                while (property.Next(true))
                {
                    if (property.propertyType != SerializedPropertyType.ObjectReference || property.objectReferenceValue == null) continue;
                    if (AssetDatabase.GetAssetPath(property.objectReferenceValue).StartsWith(PlaceholderFolder))
                        slots.Add($"{path}：{property.propertyPath}");
                }
            }
            TestContext.WriteLine($"まだ仮の絵の枠：{slots.Count}");
            foreach (var slot in slots) TestContext.WriteLine(slot);
            Assert.Pass();
        }

        static List<T> FindTables<T>() where T : ScriptableObject
        {
            var folders = TableFolders.Where(AssetDatabase.IsValidFolder).ToArray();
            if (folders.Length == 0) return new List<T>();
            return AssetDatabase.FindAssets("t:ScriptableObject", folders)
                .Select(AssetDatabase.GUIDToAssetPath)
                .Distinct()
                .Select(AssetDatabase.LoadAssetAtPath<T>)
                .Where(asset => asset != null)
                .ToList();
        }
    }
}

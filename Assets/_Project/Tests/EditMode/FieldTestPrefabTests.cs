using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using PictoArcher.Game;

namespace PictoArcher.Tests.EditMode
{
    /// <summary>
    /// テスト用のフィールド（Field_Test.prefab）が縦長（270×480）に収まっているかを確かめる。
    /// 設計書 §11：壁を含めて 270×480 に収め、戦える広さは壁の内側で HUD の下、およそ 240×400。
    /// </summary>
    public class FieldTestPrefabTests
    {
        const string PrefabPath = "Assets/_Project/Prefabs/Field/Field_Test.prefab";

        // 画面は 270×480px（16.875×30 ユニット）。カメラは (0, 0) で動かない
        static readonly Rect ScreenArea = new Rect(-8.4375f, -15f, 16.875f, 30f);
        // 戦える広さ（壁の内側）は 240×400px（15×25 ユニット）。上の 64px は HUD の下の壁
        static readonly Rect InnerArea = new Rect(-7.5f, -14f, 15f, 25f);
        const float MinObstacleDistanceFromCenter = 4f;

        GameObject _instance;

        // Tilemap のセルの位置はアセットのままでは取れないので、一度シーンに出して確かめる
        GameObject LoadPrefab()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Assert.IsNotNull(prefab, $"{PrefabPath} がありません");
            _instance = Object.Instantiate(prefab);
            return _instance;
        }

        [TearDown] public void DestroyInstance()
        {
            if (_instance != null) Object.DestroyImmediate(_instance);
        }

        static Tilemap LoadWallTilemap(GameObject prefab)
        {
            var collider = prefab.GetComponentInChildren<TilemapCollider2D>(true);
            Assert.IsNotNull(collider, "壁の Tilemap に TilemapCollider2D がありません");
            return collider.GetComponent<Tilemap>();
        }

        [Test] public void Colliders_AreOnWallLayer()
        {
            var colliders = LoadPrefab().GetComponentsInChildren<Collider2D>(true);
            Assert.IsNotEmpty(colliders);
            var wrong = new List<string>();
            foreach (var c in colliders)
                if (c.gameObject.layer != Layers.Wall) wrong.Add(c.name);
            Assert.IsEmpty(wrong, "壁と障害物はレイヤー Wall にしてください：" + string.Join("、", wrong));
        }

        [Test] public void Walls_SurroundTheInnerArea()
        {
            var walls = LoadWallTilemap(LoadPrefab());
            var missing = new List<string>();
            for (float x = InnerArea.xMin + 0.5f; x < InnerArea.xMax; x++)
            {
                if (!HasTileAt(walls, x, InnerArea.yMin - 0.5f)) missing.Add($"下 x={x}");
                if (!HasTileAt(walls, x, InnerArea.yMax + 0.5f)) missing.Add($"上 x={x}");
            }
            for (float y = InnerArea.yMin + 0.5f; y < InnerArea.yMax; y++)
            {
                if (!HasTileAt(walls, InnerArea.xMin - 0.5f, y)) missing.Add($"左 y={y}");
                if (!HasTileAt(walls, InnerArea.xMax + 0.5f, y)) missing.Add($"右 y={y}");
            }
            Assert.IsEmpty(missing, "壁のすき間：" + string.Join("、", missing));
        }

        [Test] public void Walls_StayOutOfTheInnerAreaAndOnScreen()
        {
            var walls = LoadWallTilemap(LoadPrefab());
            var problems = new List<string>();
            foreach (var cell in walls.cellBounds.allPositionsWithin)
            {
                if (!walls.HasTile(cell)) continue;
                Vector2 center = walls.GetCellCenterWorld(cell);
                if (InnerArea.Contains(center)) problems.Add($"内側に壁 {center}");
                if (!ScreenArea.Contains(center)) problems.Add($"画面の外に壁 {center}");
            }
            Assert.IsEmpty(problems, string.Join("、", problems));
        }

        [Test] public void Obstacles_AreInsideTheInnerAreaAndAwayFromTheCenter()
        {
            var boxes = LoadPrefab().GetComponentsInChildren<BoxCollider2D>(true);
            Assert.That(boxes.Length, Is.InRange(3, 4), "障害物は3〜4個");
            var problems = new List<string>();
            foreach (var box in boxes)
            {
                Vector2 center = (Vector2)box.transform.position + box.offset;
                Vector2 size = Vector2.Scale(box.size, box.transform.lossyScale);
                var rect = new Rect(center - size / 2f, size);
                if (!InnerArea.Contains(rect.min) || !InnerArea.Contains(rect.max)) problems.Add($"{box.name} が壁の内側からはみ出す");
                if (Vector2.Distance(center, InnerArea.center) < MinObstacleDistanceFromCenter) problems.Add($"{box.name} が真ん中に近い");
            }
            Assert.IsEmpty(problems, string.Join("、", problems));
        }

        static bool HasTileAt(Tilemap tilemap, float x, float y) =>
            tilemap.HasTile(tilemap.WorldToCell(new Vector3(x, y, 0f)));
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;

namespace PictoArcher.Core
{
    /// <summary>
    /// 障害物の並びが袋小路（プレイヤーが入れても出られない場所、閉じ込められる場所）を作らないかの判定。
    /// プレイヤーの体（bodySize の正方形）が通れる場所が、すべて1つにつながっていれば true。データテストだけで使う。
    /// </summary>
    public static class ObstacleReachability
    {
        public static bool IsAllReachable(Rect area, IReadOnlyList<Rect> obstacles, float bodySize, float cellSize)
        {
            if (bodySize <= 0f) throw new ArgumentOutOfRangeException(nameof(bodySize), bodySize, "体の大きさは 0 より大きい");
            if (cellSize <= 0f) throw new ArgumentOutOfRangeException(nameof(cellSize), cellSize, "マスの大きさは 0 より大きい");
            int columns = Mathf.Max(1, Mathf.FloorToInt(area.width / cellSize));
            int rows = Mathf.Max(1, Mathf.FloorToInt(area.height / cellSize));
            var open = new bool[columns, rows];
            int openCount = 0;
            int start = -1;
            float half = bodySize * 0.5f;
            const float Epsilon = 1e-4f;
            for (int x = 0; x < columns; x++)
            {
                for (int y = 0; y < rows; y++)
                {
                    var center = new Vector2(area.xMin + (x + 0.5f) * cellSize, area.yMin + (y + 0.5f) * cellSize);
                    var body = new Rect(center.x - half, center.y - half, bodySize, bodySize);
                    if (body.xMin < area.xMin - Epsilon || body.xMax > area.xMax + Epsilon || body.yMin < area.yMin - Epsilon || body.yMax > area.yMax + Epsilon) continue;
                    bool blocked = false;
                    for (int i = 0; i < obstacles.Count && !blocked; i++) blocked = body.Overlaps(obstacles[i]);
                    if (blocked) continue;
                    open[x, y] = true;
                    openCount++;
                    if (start < 0) start = x * rows + y;
                }
            }
            if (openCount == 0) return false;

            var visited = new bool[columns, rows];
            var queue = new Queue<int>();
            queue.Enqueue(start);
            visited[start / rows, start % rows] = true;
            int reached = 0;
            while (queue.Count > 0)
            {
                int cell = queue.Dequeue();
                int cx = cell / rows;
                int cy = cell % rows;
                reached++;
                Visit(cx + 1, cy);
                Visit(cx - 1, cy);
                Visit(cx, cy + 1);
                Visit(cx, cy - 1);
            }
            return reached == openCount;

            void Visit(int x, int y)
            {
                if (x < 0 || y < 0 || x >= columns || y >= rows) return;
                if (!open[x, y] || visited[x, y]) return;
                visited[x, y] = true;
                queue.Enqueue(x * rows + y);
            }
        }
    }
}

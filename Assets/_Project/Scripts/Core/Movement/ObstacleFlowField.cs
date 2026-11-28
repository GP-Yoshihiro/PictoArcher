using System;
using System.Collections.Generic;
using UnityEngine;

namespace PictoArcher.Core
{
    /// <summary>
    /// 障害物を回り込んで目標へ向かうための格子の流れ場。体の半径（clearance）ごとに1つ作る。
    /// 障害物の矩形を clearance だけ広げ、中心がその中に入るマスと、戦える広さの縁から clearance 以内のマスをふさぐ。
    /// 目標のマスから全マスへの近い道（8方向、角はすり抜けない）を計算し、各マスに「次に進むマス」を持つ。
    /// 計算し直すのは、障害物が変わったあとと、目標が別のマスへ動いたときだけ。
    /// NextPoint は、まっすぐ行けるなら目標そのもの、行けなければ道をたどって見通せる一番遠いマスの中心を返す。
    /// 道がないとき（目標が囲まれている、自分が広さの外）は目標を返す（まっすぐ向かう）。
    /// </summary>
    public sealed class ObstacleFlowField
    {
        const float Diagonal = 1.41421356f;
        const float Unreached = float.PositiveInfinity;
        // 体が障害物の面に触れているとき（中心が広げた矩形の縁）を「中」に数えない幅
        const float EdgeTolerance = 1e-3f;
        // 自分のマスがふさがっているとき、まわり何マスまで出口を探すか
        const int EscapeSearchCells = 2;

        // 8方向（前の4つがたて・よこ、後ろの4つがななめ）
        readonly int[] _stepX = { 1, -1, 0, 0, 1, 1, -1, -1 };
        readonly int[] _stepY = { 0, 0, 1, -1, 1, -1, 1, -1 };

        readonly Rect _area;
        readonly float _cellSize;
        readonly float _clearance;
        readonly int _lookAheadCells;
        readonly int _columns;
        readonly int _rows;
        readonly bool[] _blocked;
        readonly float[] _distance;
        readonly int[] _next;
        readonly List<Rect> _inflated = new List<Rect>();
        readonly CellHeap _open;
        readonly Queue<int> _escape = new Queue<int>();
        readonly bool[] _escapeSeen;

        bool _dirty = true;
        bool _hasTarget;
        int _targetCell = -1;
        Vector2 _target;

        public ObstacleFlowField(Rect area, float cellSize, float clearance, int lookAheadCells)
        {
            if (area.width <= 0f || area.height <= 0f) throw new ArgumentOutOfRangeException(nameof(area), "Area must have a positive size.");
            if (cellSize <= 0f) throw new ArgumentOutOfRangeException(nameof(cellSize), "Cell size must be positive.");
            if (clearance < 0f) throw new ArgumentOutOfRangeException(nameof(clearance), "Clearance must not be negative.");
            if (lookAheadCells < 1) throw new ArgumentOutOfRangeException(nameof(lookAheadCells), "Look-ahead must be at least one cell.");
            _area = area;
            _cellSize = cellSize;
            _clearance = clearance;
            _lookAheadCells = lookAheadCells;
            _columns = Mathf.Max(1, Mathf.CeilToInt(area.width / cellSize));
            _rows = Mathf.Max(1, Mathf.CeilToInt(area.height / cellSize));
            int count = _columns * _rows;
            _blocked = new bool[count];
            _distance = new float[count];
            _next = new int[count];
            _escapeSeen = new bool[count];
            _open = new CellHeap(count * 8);
        }

        /// <summary>道を計算した回数（同じマスの間は計算し直さないことを確かめる）。</summary>
        public int BuildCount { get; private set; }

        /// <summary>今ある障害物（広げる前の矩形）。次の SetTarget で道を計算し直す。</summary>
        public void SetObstacles(IReadOnlyList<Rect> obstacles)
        {
            if (obstacles == null) throw new ArgumentNullException(nameof(obstacles));
            _inflated.Clear();
            for (int i = 0; i < obstacles.Count; i++)
            {
                var r = obstacles[i];
                _inflated.Add(Rect.MinMaxRect(r.xMin - _clearance, r.yMin - _clearance, r.xMax + _clearance, r.yMax + _clearance));
            }
            for (int cell = 0; cell < _blocked.Length; cell++) _blocked[cell] = IsBlocked(CellCenter(cell));
            _dirty = true;
        }

        /// <summary>目標（プレイヤーの足元）。別のマスへ動いたか、障害物が変わったあとなら道を計算し直す。</summary>
        public void SetTarget(Vector2 target)
        {
            _target = target;
            _hasTarget = true;
            int cell = CellAt(target);
            if (!_dirty && cell == _targetCell) return;
            _targetCell = cell;
            _dirty = false;
            Build();
        }

        /// <summary>self から目標へ近づくために、今まっすぐ向かう点。目標がまだなければ self。</summary>
        public Vector2 NextPoint(Vector2 self)
        {
            if (!_hasTarget) return self;
            if (IsClear(self, _target)) return _target;
            int cell = CellAt(self);
            if (cell < 0) return _target;
            if (float.IsPositiveInfinity(_distance[cell]))
            {
                int exit = NearestReachedCell(self, cell);
                return exit < 0 ? _target : CellCenter(exit);
            }

            if (_next[cell] < 0) return _target;   // 目標のマス（か、そのすぐそば）。あとはまっすぐ

            // 道をたどり、見通せる一番遠いマスへ向かう（マスの中心を1つずつ追うとガタガタ歩くため）。
            // 自分のマスの中心も候補に入れる。角のすぐそばでは、道の先が角の陰になって見えないことがある
            int best = IsClear(self, CellCenter(cell)) ? cell : -1;
            int current = cell;
            for (int i = 0; i < _lookAheadCells && _next[current] >= 0; i++)
            {
                current = _next[current];
                if (IsClear(self, CellCenter(current))) best = current;
            }
            if (best < 0) best = NearestReachedCell(self, cell);
            if (best < 0) return _target;
            var point = CellCenter(best);
            // 自分のマスの中心に立っていて先が見えないときは、道の次のマスへ進む
            return (point - self).sqrMagnitude < 1e-8f ? CellCenter(_next[cell]) : point;
        }

        void Build()
        {
            BuildCount++;
            for (int i = 0; i < _distance.Length; i++)
            {
                _distance[i] = Unreached;
                _next[i] = -1;
            }
            _open.Clear();
            if (_targetCell < 0) return;   // 目標が広さの外。道はなく、NextPoint はまっすぐ向かう

            if (!_blocked[_targetCell]) Seed(_targetCell);
            else SeedAroundBlockedTarget();

            while (_open.TryPop(out int cell, out float distance))
            {
                if (distance > _distance[cell]) continue;   // もっと近い道で先に決まっている
                int cx = cell % _columns;
                int cy = cell / _columns;
                for (int d = 0; d < _stepX.Length; d++)
                {
                    int nx = cx + _stepX[d];
                    int ny = cy + _stepY[d];
                    if (!InGrid(nx, ny)) continue;
                    int neighbor = ny * _columns + nx;
                    if (_blocked[neighbor]) continue;
                    // ななめは、両どなりが空いているときだけ（障害物の角をすり抜けない）
                    if (d >= 4 && (_blocked[cy * _columns + nx] || _blocked[ny * _columns + cx])) continue;
                    float candidate = distance + (d >= 4 ? Diagonal : 1f);
                    if (candidate >= _distance[neighbor]) continue;
                    _distance[neighbor] = candidate;
                    _next[neighbor] = cell;
                    _open.Push(neighbor, candidate);
                }
            }
        }

        void Seed(int cell)
        {
            _distance[cell] = 0f;
            _next[cell] = -1;
            _open.Push(cell, 0f);
        }

        // 目標が広げた障害物の中（プレイヤーが障害物に背中をつけている）。ふさがったマスを目標から広げ、最初に出会う空きマスを始まりにする
        void SeedAroundBlockedTarget()
        {
            for (int i = 0; i < _escapeSeen.Length; i++) _escapeSeen[i] = false;
            _escape.Clear();
            _escape.Enqueue(_targetCell);
            _escapeSeen[_targetCell] = true;
            while (_escape.Count > 0)
            {
                int cell = _escape.Dequeue();
                int cx = cell % _columns;
                int cy = cell / _columns;
                for (int d = 0; d < _stepX.Length; d++)
                {
                    int nx = cx + _stepX[d];
                    int ny = cy + _stepY[d];
                    if (!InGrid(nx, ny)) continue;
                    int neighbor = ny * _columns + nx;
                    if (_escapeSeen[neighbor]) continue;
                    _escapeSeen[neighbor] = true;
                    if (_blocked[neighbor])
                    {
                        _escape.Enqueue(neighbor);
                        continue;
                    }
                    float distance = (CellCenter(neighbor) - _target).magnitude / _cellSize;
                    if (distance >= _distance[neighbor]) continue;
                    _distance[neighbor] = distance;
                    _next[neighbor] = -1;   // ここからは目標へまっすぐ
                    _open.Push(neighbor, distance);
                }
            }
        }

        // 自分のマスがふさがっている（障害物に押しつけられている）。まわりの、道のある近いマスを探す
        int NearestReachedCell(Vector2 self, int cell)
        {
            int cx = cell % _columns;
            int cy = cell / _columns;
            int best = -1;
            float bestCost = Unreached;
            for (int y = cy - EscapeSearchCells; y <= cy + EscapeSearchCells; y++)
            {
                for (int x = cx - EscapeSearchCells; x <= cx + EscapeSearchCells; x++)
                {
                    if (!InGrid(x, y)) continue;
                    int candidate = y * _columns + x;
                    if (float.IsPositiveInfinity(_distance[candidate])) continue;
                    var center = CellCenter(candidate);
                    if (!IsClear(self, center)) continue;
                    float cost = _distance[candidate] + (center - self).magnitude / _cellSize;
                    if (cost >= bestCost) continue;
                    bestCost = cost;
                    best = candidate;
                }
            }
            return best;
        }

        bool IsBlocked(Vector2 point)
        {
            if (point.x - _area.xMin < _clearance || _area.xMax - point.x < _clearance) return true;
            if (point.y - _area.yMin < _clearance || _area.yMax - point.y < _clearance) return true;
            for (int i = 0; i < _inflated.Count; i++)
            {
                var r = _inflated[i];
                if (point.x > r.xMin + EdgeTolerance && point.x < r.xMax - EdgeTolerance
                    && point.y > r.yMin + EdgeTolerance && point.y < r.yMax - EdgeTolerance) return true;
            }
            return false;
        }

        // from から to への線分が、広げた障害物の中を通らない（縁をなぞるのはよい）
        bool IsClear(Vector2 from, Vector2 to)
        {
            for (int i = 0; i < _inflated.Count; i++)
            {
                if (SegmentEntersInterior(from, to, _inflated[i])) return false;
            }
            return true;
        }

        static bool SegmentEntersInterior(Vector2 from, Vector2 to, Rect rect)
        {
            float xMin = rect.xMin + EdgeTolerance;
            float xMax = rect.xMax - EdgeTolerance;
            float yMin = rect.yMin + EdgeTolerance;
            float yMax = rect.yMax - EdgeTolerance;
            if (xMin >= xMax || yMin >= yMax) return false;
            float enter = 0f;
            float exit = 1f;
            var delta = to - from;
            return Clip(-delta.x, from.x - xMin, ref enter, ref exit)
                && Clip(delta.x, xMax - from.x, ref enter, ref exit)
                && Clip(-delta.y, from.y - yMin, ref enter, ref exit)
                && Clip(delta.y, yMax - from.y, ref enter, ref exit)
                && enter < exit;
        }

        // Liang–Barsky の1辺ぶん。線分が辺の外へ出ていく範囲を削る。全部外なら false
        static bool Clip(float p, float q, ref float enter, ref float exit)
        {
            if (Mathf.Abs(p) < 1e-12f) return q > 0f;
            float t = q / p;
            if (p < 0f)
            {
                if (t > exit) return false;
                if (t > enter) enter = t;
            }
            else
            {
                if (t < enter) return false;
                if (t < exit) exit = t;
            }
            return true;
        }

        int CellAt(Vector2 point)
        {
            if (!_area.Contains(point)) return -1;
            int x = Mathf.Min(_columns - 1, Mathf.FloorToInt((point.x - _area.xMin) / _cellSize));
            int y = Mathf.Min(_rows - 1, Mathf.FloorToInt((point.y - _area.yMin) / _cellSize));
            return y * _columns + x;
        }

        Vector2 CellCenter(int cell)
        {
            int x = cell % _columns;
            int y = cell / _columns;
            return new Vector2(_area.xMin + (x + 0.5f) * _cellSize, _area.yMin + (y + 0.5f) * _cellSize);
        }

        bool InGrid(int x, int y) => x >= 0 && y >= 0 && x < _columns && y < _rows;

        /// <summary>道の計算に使う、距離の小さい順に取り出す入れ物（同じマスが何度入ってもよい）。</summary>
        sealed class CellHeap
        {
            readonly int[] _cells;
            readonly float[] _keys;
            int _count;

            public CellHeap(int capacity)
            {
                _cells = new int[capacity];
                _keys = new float[capacity];
            }

            public void Clear() => _count = 0;

            public void Push(int cell, float key)
            {
                if (_count == _cells.Length) return;   // 8方向×マス数を超えることはない
                int i = _count++;
                while (i > 0)
                {
                    int parent = (i - 1) / 2;
                    if (_keys[parent] <= key) break;
                    _cells[i] = _cells[parent];
                    _keys[i] = _keys[parent];
                    i = parent;
                }
                _cells[i] = cell;
                _keys[i] = key;
            }

            public bool TryPop(out int cell, out float key)
            {
                if (_count == 0)
                {
                    cell = -1;
                    key = 0f;
                    return false;
                }
                cell = _cells[0];
                key = _keys[0];
                int lastCell = _cells[--_count];
                float lastKey = _keys[_count];
                int i = 0;
                while (true)
                {
                    int child = i * 2 + 1;
                    if (child >= _count) break;
                    if (child + 1 < _count && _keys[child + 1] < _keys[child]) child++;
                    if (_keys[child] >= lastKey) break;
                    _cells[i] = _cells[child];
                    _keys[i] = _keys[child];
                    i = child;
                }
                _cells[i] = lastCell;
                _keys[i] = lastKey;
                return true;
            }
        }
    }
}

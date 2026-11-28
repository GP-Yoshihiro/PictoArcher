using System;
using System.Collections.Generic;
using PictoArcher.Core;
using UnityEngine;

namespace PictoArcher.Game
{
    /// <summary>
    /// 敵がプレイヤーを追う道を作る。当たりのある障害物とプレイヤーの位置から、体の大きさごとに流れ場（ObstacleFlowField）を持つ。
    /// BattleScene が ObstacleSystem の Tick のあと、EnemySystem の Tick の前に呼ぶ。
    /// </summary>
    public sealed class EnemyPathSystem : ITickable, IDisposable, IChaseRoute
    {
        // 体の半径をこの細かさで丸めて、同じ大きさの敵は同じ流れ場を使う
        const float RadiusStep = 0.1f;

        readonly EnemyPathSettings _settings;
        readonly ObstacleSystem _obstacles;
        readonly ITarget _player;
        readonly List<Rect> _solidRects = new List<Rect>();
        readonly List<int> _fieldKeys = new List<int>();
        readonly List<ObstacleFlowField> _fields = new List<ObstacleFlowField>();
        int _seenVersion = -1;

        public EnemyPathSystem(EnemyPathSettings settings, ObstacleSystem obstacles, ITarget player)
        {
            _settings = settings;
            _obstacles = obstacles;
            _player = player;
        }

        public void Tick(float deltaTime)
        {
            SyncObstacles();
            var target = _player.Position;
            for (int i = 0; i < _fields.Count; i++) _fields[i].SetTarget(target);
        }

        public Vector2 Waypoint(Vector2 self, float radius) => FieldFor(radius).NextPoint(self);

        public void Dispose()
        {
            _fields.Clear();
            _fieldKeys.Clear();
            _solidRects.Clear();
            _seenVersion = -1;
        }

        ObstacleFlowField FieldFor(float radius)
        {
            int key = Mathf.CeilToInt(Mathf.Max(0f, radius) / RadiusStep);
            for (int i = 0; i < _fieldKeys.Count; i++)
            {
                if (_fieldKeys[i] == key) return _fields[i];
            }
            // 初めての大きさ。今の障害物と目標で作る（このフレームの Tick はもう終わっている）
            var field = new ObstacleFlowField(_settings.Area, _settings.CellSize, key * RadiusStep + _settings.ClearanceMargin, _settings.LookAheadCells);
            SyncObstacles();
            field.SetObstacles(_solidRects);
            field.SetTarget(_player.Position);
            _fieldKeys.Add(key);
            _fields.Add(field);
            return field;
        }

        void SyncObstacles()
        {
            if (_obstacles.SolidVersion == _seenVersion) return;
            _seenVersion = _obstacles.SolidVersion;
            _obstacles.CollectSolidRects(_solidRects);
            for (int i = 0; i < _fields.Count; i++) _fields[i].SetObstacles(_solidRects);
        }
    }
}

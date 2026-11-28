using System;
using System.Collections.Generic;
using UnityEngine;

namespace PictoArcher.Core
{
    /// <summary>
    /// ウェーブの休みに障害物を入れ替えるときの、影から出現までの進み（設計書 §6「障害物のパターン」）。
    /// 影を shadowSeconds 出したあと、プレイヤーと重ならない障害物から現れる。重なる物はプレイヤーが離れるまで待つ（閉じ込めない）。
    /// </summary>
    public sealed class ObstacleSwap
    {
        readonly Rect[] _obstacles;
        readonly ObstacleSlotState[] _states;
        readonly float _shadowSeconds;
        float _elapsed;

        public ObstacleSwap(IReadOnlyList<Rect> obstacles, float shadowSeconds)
        {
            if (obstacles == null) throw new ArgumentNullException(nameof(obstacles));
            if (shadowSeconds < 0f) throw new ArgumentOutOfRangeException(nameof(shadowSeconds), shadowSeconds, "影の秒数は 0 以上");
            _obstacles = new Rect[obstacles.Count];
            for (int i = 0; i < _obstacles.Length; i++) _obstacles[i] = obstacles[i];
            _states = new ObstacleSlotState[_obstacles.Length];
            _shadowSeconds = shadowSeconds;
        }

        public int Count => _obstacles.Length;
        public ObstacleSlotState StateAt(int index) => _states[index];

        /// <summary>すべて現れた。0個なら最初から true。</summary>
        public bool IsFinished
        {
            get
            {
                for (int i = 0; i < _states.Length; i++)
                    if (_states[i] != ObstacleSlotState.Shown) return false;
                return true;
            }
        }

        /// <summary>影の時間の進み（0〜1）。影の秒数が 0 なら 1。</summary>
        public float ShadowProgress => _shadowSeconds <= 0f ? 1f : Mathf.Clamp01(_elapsed / _shadowSeconds);

        public void Tick(float deltaTime, Vector2 playerPosition, float playerRadius)
        {
            if (deltaTime < 0f) throw new ArgumentOutOfRangeException(nameof(deltaTime), deltaTime, "経過は 0 以上");
            _elapsed += deltaTime;
            if (_elapsed < _shadowSeconds) return;
            for (int i = 0; i < _states.Length; i++)
            {
                if (_states[i] == ObstacleSlotState.Shown) continue;
                _states[i] = ObstacleOverlap.Overlaps(_obstacles[i], playerPosition, playerRadius) ? ObstacleSlotState.Blocked : ObstacleSlotState.Shown;
            }
        }
    }
}

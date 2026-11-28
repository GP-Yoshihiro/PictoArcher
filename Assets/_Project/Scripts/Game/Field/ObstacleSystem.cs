using System;
using System.Collections.Generic;
using PictoArcher.Core;
using UnityEngine;
using Object = UnityEngine.Object;

namespace PictoArcher.Game
{
    /// <summary>
    /// 障害物の並びを出し、ウェーブの休みに切り替える（設計書 §6「障害物のパターン」）。
    /// 切り替えは「今の障害物を消す → 薄い影を ShadowSeconds 出す → 現れる」。プレイヤーと重なる物は離れるまで影のまま待つ（閉じ込めない）。
    /// </summary>
    public sealed class ObstacleSystem : ITickable, IDisposable
    {
        readonly ObstacleView _prefab;
        readonly Transform _parent;
        readonly ObstacleSwitchSettings _settings;
        readonly ITarget _player;
        readonly List<ObstacleView> _views = new List<ObstacleView>();
        readonly List<Rect> _rects = new List<Rect>();
        ObstacleSlotState[] _shownStates = Array.Empty<ObstacleSlotState>();
        ObstacleSwap _swap;

        public ObstacleSystem(ObstacleView prefab, Transform parent, ObstacleSwitchSettings settings, ITarget player)
        {
            _prefab = prefab;
            _parent = parent;
            _settings = settings;
            _player = player;
        }

        public ObstacleLayout Current { get; private set; }

        /// <summary>当たりのある障害物の並びが変わるたびに増える。敵の道を作り直すかの目印。</summary>
        public int SolidVersion { get; private set; }

        /// <summary>当たりのある（現れた）障害物の範囲を list に入れる。list は先に空にする。</summary>
        public void CollectSolidRects(List<Rect> list)
        {
            list.Clear();
            for (int i = 0; i < _views.Count; i++)
            {
                if (_shownStates[i] == ObstacleSlotState.Shown) list.Add(_rects[i]);
            }
        }

        /// <summary>現れていない障害物がある間 true。</summary>
        public bool IsSwitching => _swap != null && !_swap.IsFinished;

        /// <summary>戦いの始めに、影を出さずにすぐ出す。プレイヤーと重なる物だけは影で待つ。null なら何もしない。</summary>
        public void ShowImmediately(ObstacleLayout layout)
        {
            if (layout == null) return;
            Place(layout, 0f);
            Tick(0f);
        }

        /// <summary>次のウェーブの並びへ切り替える。null か今と同じなら何もしない。切り替えの途中でも古い影を消して始め直す。</summary>
        public void SwitchTo(ObstacleLayout layout)
        {
            if (layout == null || layout == Current) return;
            Place(layout, _settings.ShadowSeconds);
        }

        public void Tick(float deltaTime)
        {
            if (_swap == null) return;
            _swap.Tick(deltaTime, _player.Position, _settings.PlayerRadius);
            for (int i = 0; i < _views.Count; i++)
            {
                var state = _swap.StateAt(i);
                if (state == _shownStates[i]) continue;
                // Shadow と Blocked はどちらも影の見た目。現れたときだけ当たりを入れる
                if (state == ObstacleSlotState.Shown)
                {
                    _views[i].ShowSolid();
                    SolidVersion++;
                }
                _shownStates[i] = state;
            }
        }

        public void Dispose()
        {
            Clear();
            _swap = null;
            Current = null;
        }

        void Place(ObstacleLayout layout, float shadowSeconds)
        {
            Clear();
            Current = layout;
            var placements = layout.Placements;
            for (int i = 0; i < placements.Count; i++)
            {
                var view = Object.Instantiate(_prefab, _parent);
                view.Init(placements[i]);
                view.ShowShadow(_settings.ShadowColor);
                _views.Add(view);
                _rects.Add(placements[i].Bounds);
            }
            if (_shownStates.Length < _views.Count) _shownStates = new ObstacleSlotState[_views.Count];
            for (int i = 0; i < _shownStates.Length; i++) _shownStates[i] = ObstacleSlotState.Shadow;
            _swap = new ObstacleSwap(_rects, shadowSeconds);
        }

        void Clear()
        {
            for (int i = 0; i < _views.Count; i++)
            {
                if (_views[i] == null) continue;
                // Destroy はフレームの終わりまで遅れるので、先に隠して同じフレームの当たり判定を切る
                _views[i].gameObject.SetActive(false);
                Object.Destroy(_views[i].gameObject);
            }
            if (_views.Count > 0) SolidVersion++;
            _views.Clear();
            _rects.Clear();
        }
    }
}

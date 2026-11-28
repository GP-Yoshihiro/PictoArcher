using System;
using UnityEngine;

namespace PictoArcher.Game
{
    /// <summary>パターンの中の障害物1つ（見た目の表と、フィールド上の中心の位置）。</summary>
    [Serializable]
    public struct ObstaclePlacement
    {
        [SerializeField] SpriteVisual _visual;
        [SerializeField, Tooltip("フィールド上の位置（ユニット）")] Vector2 _position;

        public ObstaclePlacement(SpriteVisual visual, Vector2 position)
        {
            _visual = visual;
            _position = position;
        }

        public SpriteVisual Visual => _visual;
        public Vector2 Position => _position;

        /// <summary>出す絵（見た目の表の1コマ目）。表かコマが空なら null。</summary>
        public Sprite Sprite => _visual != null && _visual.Clip.Frames.Count > 0 ? _visual.Clip.Frames[0] : null;

        /// <summary>フィールド上の範囲。絵の大きさを表示の倍率で広げ、Position に置く。絵がなければ大きさ 0。</summary>
        public Rect Bounds
        {
            get
            {
                var sprite = Sprite;
                if (sprite == null) return new Rect(_position, Vector2.zero);
                var bounds = sprite.bounds;
                float scale = _visual.Scale;
                return new Rect(_position + (Vector2)bounds.min * scale, (Vector2)bounds.size * scale);
            }
        }
    }
}

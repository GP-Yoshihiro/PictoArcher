using System;
using System.Collections.Generic;
using UnityEngine;

namespace PictoArcher.Game
{
    /// <summary>フィールドの見た目の表（床、壁、飾り、10ウェーブごとの照明5段）。</summary>
    [CreateAssetMenu(menuName = "PictoArcher/Visuals/FieldVisual")]
    public sealed class FieldVisual : ScriptableObject
    {
        public const int LightingStages = 5;
        public const int WavesPerLightingStage = 10;

        [SerializeField, Tooltip("床のタイル（16×16）")] Sprite _floor;
        [SerializeField, Tooltip("壁のタイル（16×16）")] Sprite _wall;
        [SerializeField, Tooltip("床に置く飾り")] Sprite[] _decorations = Array.Empty<Sprite>();
        [SerializeField, Tooltip("照明の色。ウェーブ 1–10、11–20、21–30、31–40、41–50 の順に5つ")]
        Color[] _lighting = { Color.white, Color.white, Color.white, Color.white, Color.white };

        public Sprite Floor => _floor;
        public Sprite Wall => _wall;
        public IReadOnlyList<Sprite> Decorations => _decorations;
        public IReadOnlyList<Color> Lighting => _lighting;

        /// <summary>ウェーブ番号（1〜50）の照明の色。範囲の外は一番近い段にする。</summary>
        public Color LightingFor(int wave)
        {
            if (_lighting.Length == 0) return Color.white;
            int stage = Mathf.Clamp((wave - 1) / WavesPerLightingStage, 0, _lighting.Length - 1);
            return _lighting[stage];
        }
    }
}

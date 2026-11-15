using System;
using System.Collections.Generic;
using UnityEngine;

namespace PictoArcher.Game
{
    /// <summary>コマ送りのアニメ1つ分（コマの並び、速さ、ループするか）。見た目の表の中で使う。</summary>
    [Serializable]
    public sealed class SpriteClip
    {
        [SerializeField, Tooltip("右向きのコマを順に並べる。左向きは反転して出す")] Sprite[] _frames = Array.Empty<Sprite>();
        [SerializeField, Tooltip("1秒に進むコマの数")] float _framesPerSecond = 8f;
        [SerializeField, Tooltip("最後のコマのあと頭に戻るか。戻らないものは最後のコマで止まる")] bool _loops = true;

        public SpriteClip() { }
        public SpriteClip(bool loops) => _loops = loops;

        public IReadOnlyList<Sprite> Frames => _frames;
        public float FramesPerSecond => _framesPerSecond;
        public bool Loops => _loops;
    }
}

using System.Collections.Generic;
using UnityEngine;

namespace PictoArcher.Game
{
    /// <summary>1回のプレイ（ラン）の調整値。どのウェーブを出すか、休みの長さ、終わってから結果へ移るまでの時間。</summary>
    [CreateAssetMenu(menuName = "PictoArcher/RunDefinition")]
    public sealed class RunDefinition : ScriptableObject
    {
        [SerializeField, Tooltip("ウェーブ n には (n - 1) % 個数 番目を使う")] List<WaveDefinition> _waves = new List<WaveDefinition>();
        [SerializeField, Min(1), Tooltip("確かめるときだけ変える（49 にすると 49/50 から始まる）")] int _startWave = 1;
        [SerializeField, Min(0f)] float _restSeconds = 1.5f;
        [SerializeField, Min(0f), Tooltip("死んだ・全部クリアしたあと、結果の画面へ移るまでの秒数")] float _endDelaySeconds = 1.2f;
        [SerializeField] Vector2 _playerStart = new Vector2(0f, -2f);

        public IReadOnlyList<WaveDefinition> Waves => _waves;
        public int StartWave => _startWave;
        public float RestSeconds => _restSeconds;
        public float EndDelaySeconds => _endDelaySeconds;
        public Vector2 PlayerStart => _playerStart;

        public WaveDefinition WaveFor(int wave) => _waves[(wave - 1) % _waves.Count];
    }
}

using UnityEngine;

namespace PictoArcher.Game
{
    /// <summary>出現予兆（赤い円と絵）の調整値。予兆を出す System が持ち、見た目の部品に値を渡す。</summary>
    [CreateAssetMenu(menuName = "PictoArcher/SpawnTelegraphSettings")]
    public sealed class SpawnTelegraphSettings : ScriptableObject
    {
        [SerializeField, Tooltip("1秒に点滅する回数")] float _blinksPerSecond = 4f;
        [SerializeField, Tooltip("予兆の終わりでの大きさ（始めを 1 とした倍率）")] float _endScale = 1.3f;

        public float BlinksPerSecond => _blinksPerSecond;
        public float EndScale => _endScale;
    }
}

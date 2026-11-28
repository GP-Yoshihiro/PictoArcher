using PictoArcher.Core;
using UnityEngine;

namespace PictoArcher.Game
{
    /// <summary>経験値とコインの落ち方・吸い寄せの調整値と見た目。</summary>
    [CreateAssetMenu(menuName = "PictoArcher/PickupSettings")]
    public sealed class PickupSettings : ScriptableObject
    {
        [SerializeField, Min(0f), Tooltip("この距離まで近づくと吸い寄せ始める（ユニット）")] float _magnetRadius = 2.5f;
        [SerializeField, Min(0f), Tooltip("この距離まで来たら拾う（ユニット）")] float _collectRadius = 0.5f;
        [SerializeField, Min(0f), Tooltip("吸い寄せ始めの速さ（ユニット毎秒）")] float _startSpeed = 4f;
        [SerializeField, Min(0f), Tooltip("吸い寄せの間、毎秒これだけ速くなる")] float _acceleration = 30f;
        [SerializeField, Min(0f), Tooltip("吸い寄せの速さの上限（ユニット毎秒）")] float _maxSpeed = 20f;
        [SerializeField, Min(0f), Tooltip("死んだ所から、経験値を左・コインを右にずらす幅（ユニット）")] float _dropSpread = 0.3f;
        [SerializeField] SpriteVisual _experienceVisual;
        [SerializeField] SpriteVisual _coinVisual;

        public float MagnetRadius => _magnetRadius;
        public float CollectRadius => _collectRadius;
        public float StartSpeed => _startSpeed;
        public float Acceleration => _acceleration;
        public float MaxSpeed => _maxSpeed;
        public float DropSpread => _dropSpread;
        public SpriteVisual ExperienceVisual => _experienceVisual;
        public SpriteVisual CoinVisual => _coinVisual;

        public PickupTuning Tuning => new PickupTuning(_magnetRadius, _collectRadius, _startSpeed, _acceleration, _maxSpeed);
        public SpriteVisual VisualFor(PickupKind kind) => kind == PickupKind.Coin ? _coinVisual : _experienceVisual;
    }
}

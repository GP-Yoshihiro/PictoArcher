using System;
using PictoArcher.Core;
using UnityEngine;

namespace PictoArcher.Game
{
    /// <summary>HUD に今の値を写す。上の帯（ウェーブ）はシーンの Canvas に置いたもの、頭上の HP はプレハブから作る。</summary>
    public sealed class HudSystem : ITickable, IDisposable
    {
        readonly HudView _hud;
        readonly HeadHpView _headHp;
        readonly IPlayerStatus _player;
        readonly ITarget _playerBody;
        readonly IWaveStatus _waves;

        public HudSystem(HudView hud, HeadHpView headHpPrefab, IPlayerStatus player, ITarget playerBody, IWaveStatus waves)
        {
            _hud = hud;
            _player = player;
            _playerBody = playerBody;
            _waves = waves;
            _hud.Init();
            _headHp = UnityEngine.Object.Instantiate(headHpPrefab);
            _headHp.Init();
        }

        public void Tick(float deltaTime)
        {
            _hud.SetWave(_waves.CurrentWave, _waves.TotalWaves);
            _headHp.Show(_playerBody.Position, _player.Hp, _player.MaxHp);
        }

        public void Dispose()
        {
            if (_headHp != null) UnityEngine.Object.Destroy(_headHp.gameObject);
        }
    }
}

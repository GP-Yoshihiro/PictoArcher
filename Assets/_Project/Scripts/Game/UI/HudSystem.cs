using System;
using System.Collections.Generic;
using PictoArcher.Core;
using UnityEngine;

namespace PictoArcher.Game
{
    /// <summary>HUD に今の値を写す。上の帯（ウェーブ、ボスの HP）と育ち（経験値、★n、コイン、強化の絵）はシーンの Canvas に置いたもの、頭上の HP はプレハブから作る。</summary>
    public sealed class HudSystem : ITickable, IDisposable
    {
        readonly HudView _hud;
        readonly HudStatusView _status;
        readonly HeadHpView _headHp;
        readonly IRunStatus _run;
        readonly SkillCatalog _skills;
        readonly List<int> _shownCounts = new List<int>();
        readonly IPlayerStatus _player;
        readonly ITarget _playerBody;
        readonly IWaveStatus _waves;
        readonly IBossStatus _boss;

        public HudSystem(HudView hud, HudStatusView status, HeadHpView headHpPrefab, IPlayerStatus player, ITarget playerBody, IWaveStatus waves, IBossStatus boss,
            IRunStatus run, SkillCatalog skills, UiIconVisual icons, WeaponId weapon)
        {
            _hud = hud;
            _status = status;
            _run = run;
            _skills = skills;
            _player = player;
            _playerBody = playerBody;
            _waves = waves;
            _boss = boss;
            _hud.Init();
            _status.Init(icons, icons.WeaponIconOf(weapon));
            _headHp = UnityEngine.Object.Instantiate(headHpPrefab);
            _headHp.Init();
        }

        public void Tick(float deltaTime)
        {
            _hud.SetWave(_waves.CurrentWave, _waves.TotalWaves);
            _hud.SetBoss(_boss.HasBoss, _boss.BossHp, _boss.BossMaxHp);
            _status.SetLevel(_run.Level);
            _status.SetCoins(_run.Coins);
            _status.SetExperience(_run.Experience, _run.ExperienceToNextLevel);
            ShowSkills();
            _headHp.Show(_playerBody.Position, _player.Hp, _player.MaxHp);
        }

        // 取った強化の絵。数が変わったマスだけ渡す（新しく取った強化は新しいマス、重ね取りは ×n）。
        void ShowSkills()
        {
            var taken = _run.TakenSkills;
            for (int i = 0; i < taken.Count; i++)
            {
                int count = _run.TakenCountOf(taken[i]);
                if (i < _shownCounts.Count && _shownCounts[i] == count) continue;
                var definition = _skills.DefinitionOf(taken[i]);
                _status.SetSkill(i, taken[i], definition != null ? definition.Icon : null, count);
                if (i < _shownCounts.Count) _shownCounts[i] = count;
                else _shownCounts.Add(count);
            }
        }

        public void Dispose()
        {
            if (_headHp != null) UnityEngine.Object.Destroy(_headHp.gameObject);
        }
    }
}

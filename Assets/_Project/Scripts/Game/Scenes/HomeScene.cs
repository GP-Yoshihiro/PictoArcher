using UnityEngine;

namespace PictoArcher.Game
{
    /// <summary>
    /// ホームのシーン。▶ と最高記録を出す。▶ が押されたら武器を選ぶ画面を出し、武器が選ばれたらその武器で Battle へ進む。
    /// 武器を選ぶ画面の 🏠 で、選ばずにホームへ戻る。
    /// </summary>
    public sealed class HomeScene : SceneBase
    {
        [SerializeField] UiButtonRelay _startButton;
        [SerializeField] UiIconVisual _icons;
        [SerializeField] HomeView _view;
        [SerializeField] WeaponSelectView _weaponSelect;
        [SerializeField] UiButtonRelay _gunButton;
        [SerializeField] UiButtonRelay _laserButton;
        [SerializeField] UiButtonRelay _launcherButton;
        [SerializeField] UiButtonRelay _backButton;

        GameContext _context;
        bool _starting;

        public override void Init(GameContext context)
        {
            _context = context;
            _startButton.Init();
            _gunButton.Init();
            _laserButton.Init();
            _launcherButton.Init();
            _backButton.Init();
            _view.Show(context.Records, _icons);
            _weaponSelect.Init(_icons);
        }

        public override void Tick(float deltaTime)
        {
            // 選んだあと Battle が読み込まれるまでに別の武器を押しても、選び直さない
            if (_starting) return;
            if (!_weaponSelect.IsOpen)
            {
                if (_startButton.ConsumeClicked()) OpenWeaponSelect();
                return;
            }
            if (_backButton.ConsumeClicked())
            {
                _weaponSelect.Close();
                _startButton.ConsumeClicked();   // 開いている間の ▶ の記録は捨てる
                return;
            }
            if (_gunButton.ConsumeClicked()) Begin(WeaponId.Gun);
            else if (_laserButton.ConsumeClicked()) Begin(WeaponId.Laser);
            else if (_launcherButton.ConsumeClicked()) Begin(WeaponId.Launcher);
        }

        void OpenWeaponSelect()
        {
            // 隠れている間の記録が残っていても、開いた直後に選ばれないよう捨てる
            _gunButton.ConsumeClicked();
            _laserButton.ConsumeClicked();
            _launcherButton.ConsumeClicked();
            _backButton.ConsumeClicked();
            _weaponSelect.Open(_context.Weapon);
        }

        void Begin(WeaponId weapon)
        {
            _context.SelectWeapon(weapon);
            _starting = _context.RequestScene(SceneId.Battle);
        }

        public override void Dispose()
        {
            _backButton.Dispose();
            _launcherButton.Dispose();
            _laserButton.Dispose();
            _gunButton.Dispose();
            _startButton.Dispose();
        }
    }
}

using UnityEngine;
using UnityEngine.UI;

namespace PictoArcher.Game
{
    /// <summary>
    /// 武器を選ぶ画面。ホームで ▶ を押したあとに出す。3つの武器の絵を並べ、前回選んだ武器に印を付ける。
    /// ボタンの押された記録は HomeScene が UiButtonRelay で見る。この View は見せる・隠すだけ。
    /// </summary>
    public sealed class WeaponSelectView : MonoBehaviour
    {
        [SerializeField, Tooltip("画面全体の板。下の ▶ を押せないようにする")] GameObject _panel;
        [SerializeField] Image _gunIcon;
        [SerializeField] Image _laserIcon;
        [SerializeField] Image _launcherIcon;
        [SerializeField, Tooltip("前回選んだ武器の印")] GameObject _gunMark;
        [SerializeField] GameObject _laserMark;
        [SerializeField] GameObject _launcherMark;
        [SerializeField, Tooltip("閉じてホームへ戻るボタンの絵")] Image _backIcon;

        public bool IsOpen => _panel.activeSelf;

        /// <summary>画面を開いたときに1回だけ呼ぶ。はじめは隠しておく。</summary>
        public void Init(UiIconVisual icons)
        {
            _gunIcon.sprite = icons.WeaponGun;
            _laserIcon.sprite = icons.WeaponLaser;
            _launcherIcon.sprite = icons.WeaponLauncher;
            _backIcon.sprite = icons.Home;
            _panel.SetActive(false);
        }

        public void Open(WeaponId last)
        {
            _gunMark.SetActive(last == WeaponId.Gun);
            _laserMark.SetActive(last == WeaponId.Laser);
            _launcherMark.SetActive(last == WeaponId.Launcher);
            _panel.SetActive(true);
        }

        public void Close() => _panel.SetActive(false);
    }
}

using UnityEngine;

namespace PictoArcher.Game
{
    /// <summary>
    /// UI の絵の表（ボタン、HUD のピクトグラム、武器のアイコン、カードの枠）。ドット絵ではない（Bilinear）。
    /// 強化のアイコンは SkillDefinition が持つ。
    /// </summary>
    [CreateAssetMenu(menuName = "PictoArcher/Visuals/UiIconVisual")]
    public sealed class UiIconVisual : ScriptableObject
    {
        [Header("ボタン")]
        [SerializeField, Tooltip("▶ 開始")] Sprite _play;
        [SerializeField, Tooltip("⏸ 一時停止")] Sprite _pause;
        [SerializeField, Tooltip("↻ やり直し")] Sprite _retry;
        [SerializeField, Tooltip("🏠 ホーム")] Sprite _home;
        [SerializeField, Tooltip("⚙ 設定")] Sprite _settings;
        [SerializeField, Tooltip("🔒 ロック中")] Sprite _locked;
        [SerializeField, Tooltip("🔊 音量")] Sprite _volume;

        [Header("HUD")]
        [SerializeField, Tooltip("HP")] Sprite _health;
        [SerializeField, Tooltip("コイン")] Sprite _coin;
        [SerializeField, Tooltip("経験値")] Sprite _experience;
        [SerializeField, Tooltip("★ レベル")] Sprite _level;
        [SerializeField, Tooltip("⏱ 時間")] Sprite _timer;
        [SerializeField, Tooltip("💀 ボスの HP バー")] Sprite _boss;
        [SerializeField, Tooltip("⚠ 中ボス・危険")] Sprite _warning;

        [Header("武器")]
        [SerializeField] Sprite _weaponGun;
        [SerializeField] Sprite _weaponLaser;
        [SerializeField] Sprite _weaponLauncher;

        [Header("カード")]
        [SerializeField, Tooltip("カードの枠（9スライス）")] Sprite _cardFrame;

        [Header("結果")]
        [SerializeField, Tooltip("🏆 クリア")] Sprite _trophy;
        [SerializeField, Tooltip("💀 死亡")] Sprite _defeat;

        [Header("操作案内")]
        [SerializeField, Tooltip("矢印キー（上向き。回して4方向に使う。キーに英字を描かない）")] Sprite _guideArrowKey;
        [SerializeField, Tooltip("マウス（左ボタンを押した形）")] Sprite _guideMouse;
        [SerializeField, Tooltip("手（ドラッグを見せる）")] Sprite _guideHand;

        public Sprite Play => _play;
        public Sprite Pause => _pause;
        public Sprite Retry => _retry;
        public Sprite Home => _home;
        public Sprite Settings => _settings;
        public Sprite Locked => _locked;
        public Sprite Volume => _volume;
        public Sprite Health => _health;
        public Sprite Coin => _coin;
        public Sprite Experience => _experience;
        public Sprite Level => _level;
        public Sprite Timer => _timer;
        public Sprite Boss => _boss;
        public Sprite Warning => _warning;
        public Sprite WeaponGun => _weaponGun;
        public Sprite WeaponLaser => _weaponLaser;
        public Sprite WeaponLauncher => _weaponLauncher;
        public Sprite CardFrame => _cardFrame;

        /// <summary>武器の種類から、その武器の絵を引く。</summary>
        public Sprite WeaponIconOf(WeaponId id)
        {
            switch (id)
            {
                case WeaponId.Laser: return _weaponLaser;
                case WeaponId.Launcher: return _weaponLauncher;
                default: return _weaponGun;
            }
        }
        public Sprite Trophy => _trophy;
        public Sprite Defeat => _defeat;
        public Sprite GuideArrowKey => _guideArrowKey;
        public Sprite GuideMouse => _guideMouse;
        public Sprite GuideHand => _guideHand;
    }
}

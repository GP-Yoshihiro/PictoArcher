using System;
using PictoArcher.Core;

namespace PictoArcher.Game
{
    /// <summary>Main が作ってシーンに渡す、全体で使うもの。必要になったら足す。</summary>
    public sealed class GameContext
    {
        readonly SceneFlow<SceneId, OverlayId> _flow;
        readonly ISaveStore _saves;

        /// <summary>saved は起動のときに読み込んだ保存。saves が null なら保存しない（テスト用）。</summary>
        public GameContext(SceneFlow<SceneId, OverlayId> flow, GameClock clock, SaveData saved, ISaveStore saves = null)
        {
            if (saved == null) throw new ArgumentNullException(nameof(saved));
            _flow = flow;
            _saves = saves;
            Clock = clock;
            Records = saved.ToRecord();
            Coins = saved.Coins;
            Weapon = WeaponFromName(saved.Weapon);
        }

        public GameClock Clock { get; }

        /// <summary>最高記録。Home が表示し、FinishRun で更新する。</summary>
        public BestRecord Records { get; }

        /// <summary>これまでに拾ったコインの合計。FinishRun で足す。</summary>
        public int Coins { get; private set; }

        /// <summary>次のプレイで使う武器。保存がなければ銃。武器を選ぶ画面が SelectWeapon で変える。</summary>
        public WeaponId Weapon { get; private set; }

        /// <summary>次のプレイで使う武器を決める。変わったら保存する（次に起動したときも選ばれている）。</summary>
        public void SelectWeapon(WeaponId id)
        {
            if (id == Weapon) return;
            Weapon = id;
            Save();
        }

        /// <summary>最後に終わったプレイの結果。まだなければ null。Result が表示する。</summary>
        public RunResult LastRun { get; private set; }

        /// <summary>プレイを終えて Result へ切り替え、結果と最高記録とコインを残して保存する。切り替えを受け付けなかったら false で、何も残さない。</summary>
        public bool FinishRun(RunResult result)
        {
            if (result == null) throw new System.ArgumentNullException(nameof(result));
            if (!_flow.Request(SceneId.Result)) return false;
            LastRun = result;
            Records.Submit(result.ReachedWave, result.IsCleared, result.ClearSeconds);
            Coins = (int)Math.Min((long)Coins + Math.Max(result.Coins, 0), int.MaxValue);
            Save();
            return true;
        }

        /// <summary>中身のシーンを入れ替える。開いている重ねシーンは全部閉じる。受け付けなかったら false（同じフレームの2回目、読み込み中）。</summary>
        public bool RequestScene(SceneId id) => _flow.Request(id);

        /// <summary>重ねシーンを開く。受け付けなかったら false（同じものがもう開いている、同じフレームの2回目、読み込み中）。</summary>
        public bool OpenOverlay(OverlayId id) => _flow.RequestOpenOverlay(id);

        /// <summary>一番上の重ねシーンを閉じ、すぐ下のシーンの OnOverlayClosed に result を渡す。重ねシーンがなければ false。</summary>
        public bool CloseOverlay(int result = 0) => _flow.RequestCloseOverlay(result);

        void Save() => _saves?.Save(SaveData.From(Records, Coins, Weapon.ToString()));

        // 名前が武器の種類にない（空、古い名前、壊れている）なら銃
        static WeaponId WeaponFromName(string name) =>
            Enum.TryParse(name, false, out WeaponId id) && Enum.IsDefined(typeof(WeaponId), id) ? id : WeaponId.Gun;
    }
}

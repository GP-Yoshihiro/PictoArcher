using NUnit.Framework;
using PictoArcher.Core;
using PictoArcher.Game;

namespace PictoArcher.Tests.EditMode
{
    public class GameContextTests
    {
        sealed class MemoryStore : ISaveStore
        {
            public SaveData Saved;
            public int SaveCount;
            public SaveData Load() => Saved ?? SaveData.Empty();
            public void Save(SaveData data) { Saved = data; SaveCount++; }
        }

        static (SceneFlow<SceneId, OverlayId> flow, GameContext context) Running(SaveData saved = null, ISaveStore store = null)
        {
            var flow = new SceneFlow<SceneId, OverlayId>();
            var context = new GameContext(flow, new GameClock(), saved ?? SaveData.Empty(), store);
            flow.EnterRunning();
            return (flow, context);
        }

        [Test] public void Weapon_StartsAsGun_AndChangesWhenSelected()
        {
            var (_, context) = Running();
            Assert.AreEqual(WeaponId.Gun, context.Weapon);
            context.SelectWeapon(WeaponId.Launcher);
            Assert.AreEqual(WeaponId.Launcher, context.Weapon);
        }

        [Test] public void RequestScene_ForwardsToFlow()
        {
            var (flow, context) = Running();
            Assert.IsTrue(context.RequestScene(SceneId.Home));
            Assert.IsTrue(flow.TryBegin(out var request));
            Assert.AreEqual(SceneRequestKind.Change, request.Kind);
            Assert.AreEqual(SceneId.Home, request.Scene);
        }

        [Test] public void OpenOverlay_ForwardsToFlow()
        {
            var (flow, context) = Running();
            Assert.IsTrue(context.OpenOverlay(OverlayId.Pause));
            Assert.IsTrue(flow.TryBegin(out var request));
            Assert.AreEqual(SceneRequestKind.OpenOverlay, request.Kind);
            Assert.AreEqual(OverlayId.Pause, request.Overlay);
        }

        [Test] public void CloseOverlay_ForwardsResult()
        {
            var (flow, context) = Running();
            context.OpenOverlay(OverlayId.Pause);
            flow.TryBegin(out _);
            flow.EnterRunning();
            Assert.IsTrue(context.CloseOverlay(3));
            Assert.IsTrue(flow.TryBegin(out var request));
            Assert.AreEqual(SceneRequestKind.CloseOverlay, request.Kind);
            Assert.AreEqual(OverlayId.Pause, request.Overlay);
            Assert.AreEqual(3, request.Result);
        }

        [Test] public void CloseOverlay_WithoutOverlay_ReturnsFalse() =>
            Assert.IsFalse(Running().context.CloseOverlay());

        [Test] public void FinishRun_RequestsResult_AndRecords()
        {
            var (flow, context) = Running();
            var result = new RunResult(RunOutcome.Cleared, 50, 1500f, 12, WeaponId.Gun, null);
            Assert.IsTrue(context.FinishRun(result));
            Assert.IsTrue(flow.TryBegin(out var request));
            Assert.AreEqual(SceneRequestKind.Change, request.Kind);
            Assert.AreEqual(SceneId.Result, request.Scene);
            Assert.AreSame(result, context.LastRun);
            Assert.IsTrue(context.Records.HasCleared);
            Assert.AreEqual(1500f, context.Records.BestClearSeconds);
        }

        [Test] public void FinishRun_SecondInSameFrame_IsRejectedAndNotRecorded()
        {
            var (_, context) = Running();
            Assert.IsTrue(context.FinishRun(new RunResult(RunOutcome.Died, 23, 0f, 0, WeaponId.Gun, null)));
            Assert.IsFalse(context.FinishRun(new RunResult(RunOutcome.Died, 40, 0f, 0, WeaponId.Gun, null)));
            Assert.AreEqual(23, context.LastRun.ReachedWave);
            Assert.AreEqual(23, context.Records.BestWave);
        }

        [Test] public void LastRun_IsNullBeforeAnyRun() => Assert.IsNull(Running().context.LastRun);

        [Test] public void Ctor_StartsFromTheSave()
        {
            var (_, context) = Running(new SaveData(31, false, 0f, 120, "Laser"));
            Assert.AreEqual(31, context.Records.BestWave);
            Assert.AreEqual(120, context.Coins);
            Assert.AreEqual(WeaponId.Laser, context.Weapon);
        }

        [TestCase("")]
        [TestCase("Sword")]
        [TestCase("laser")]
        [TestCase("99")]
        public void Ctor_UnknownWeaponName_FallsBackToGun(string name)
            => Assert.AreEqual(WeaponId.Gun, Running(new SaveData(0, false, 0f, 0, name)).context.Weapon);

        [Test] public void SelectWeapon_SavesOnlyWhenItChanges()
        {
            var store = new MemoryStore();
            var (_, context) = Running(store: store);
            context.SelectWeapon(WeaponId.Gun);
            Assert.AreEqual(0, store.SaveCount, "同じ武器なら保存しない");
            context.SelectWeapon(WeaponId.Launcher);
            Assert.AreEqual(1, store.SaveCount);
            Assert.AreEqual("Launcher", store.Saved.Weapon);
        }

        [Test] public void FinishRun_AddsCoins_AndSavesTheRecord()
        {
            var store = new MemoryStore();
            var (_, context) = Running(new SaveData(10, false, 0f, 30, "Gun"), store);
            Assert.IsTrue(context.FinishRun(new RunResult(RunOutcome.Died, 23, 0f, 12, WeaponId.Gun, null)));
            Assert.AreEqual(42, context.Coins);
            Assert.AreEqual(1, store.SaveCount);
            Assert.AreEqual(23, store.Saved.BestWave);
            Assert.AreEqual(42, store.Saved.Coins);
        }

        [Test] public void FinishRun_Rejected_DoesNotSave()
        {
            var store = new MemoryStore();
            var (_, context) = Running(store: store);
            context.FinishRun(new RunResult(RunOutcome.Died, 3, 0f, 5, WeaponId.Gun, null));
            context.FinishRun(new RunResult(RunOutcome.Died, 9, 0f, 5, WeaponId.Gun, null));
            Assert.AreEqual(1, store.SaveCount);
            Assert.AreEqual(5, context.Coins);
        }

        [Test] public void FinishRun_CoinsStopAtIntMax()
        {
            var (_, context) = Running(new SaveData(0, false, 0f, int.MaxValue - 1, "Gun"));
            context.FinishRun(new RunResult(RunOutcome.Died, 1, 0f, 10, WeaponId.Gun, null));
            Assert.AreEqual(int.MaxValue, context.Coins);
        }

        [Test] public void RestartFromTheSameStore_KeepsWeaponAndRecord()
        {
            var store = new MemoryStore();
            var (_, first) = Running(store: store);
            first.SelectWeapon(WeaponId.Laser);
            first.FinishRun(new RunResult(RunOutcome.Died, 17, 0f, 8, WeaponId.Laser, null));

            var (_, second) = Running(store.Load(), store);
            Assert.AreEqual(WeaponId.Laser, second.Weapon);
            Assert.AreEqual(17, second.Records.BestWave);
            Assert.AreEqual(8, second.Coins);
        }
    }
}

using System;
using NUnit.Framework;
using PictoArcher.Core;

namespace PictoArcher.Tests.EditMode
{
    public class SceneFlowTests
    {
        enum TestId { A, B }
        enum TestOverlay { X, Y }

        static SceneFlow<TestId, TestOverlay> Running()
        {
            var flow = new SceneFlow<TestId, TestOverlay>();
            flow.EnterRunning();
            return flow;
        }

        static SceneFlow<TestId, TestOverlay> WithOverlays(params TestOverlay[] overlays)
        {
            var flow = Running();
            foreach (var overlay in overlays)
            {
                Assert.IsTrue(flow.RequestOpenOverlay(overlay));
                Assert.IsTrue(flow.TryBegin(out _));
                flow.EnterRunning();
            }
            return flow;
        }

        [Test] public void NewFlow_IsEmptyWithoutPending()
        {
            var flow = new SceneFlow<TestId, TestOverlay>();
            Assert.AreEqual(SceneFlowPhase.Empty, flow.Phase);
            Assert.IsFalse(flow.HasPending);
            Assert.AreEqual(0, flow.OverlayCount);
        }

        [Test] public void Request_WhenEmpty_ReturnsFalse() =>
            Assert.IsFalse(new SceneFlow<TestId, TestOverlay>().Request(TestId.A));

        [Test] public void Request_AfterEnterRunning_IsAccepted()
        {
            var flow = Running();
            Assert.IsTrue(flow.Request(TestId.A));
            Assert.IsTrue(flow.HasPending);
        }

        [Test] public void SecondRequestSameFrame_ReturnsFalseAndKeepsFirst()
        {
            var flow = Running();
            Assert.IsTrue(flow.Request(TestId.A));
            Assert.IsFalse(flow.Request(TestId.B));
            Assert.IsFalse(flow.RequestOpenOverlay(TestOverlay.X));
            Assert.IsTrue(flow.TryBegin(out var request));
            Assert.AreEqual(SceneRequestKind.Change, request.Kind);
            Assert.AreEqual(TestId.A, request.Scene);
        }

        [Test] public void TryBegin_Change_EntersLoading()
        {
            var flow = Running();
            flow.Request(TestId.B);
            Assert.IsTrue(flow.TryBegin(out _));
            Assert.AreEqual(SceneFlowPhase.Loading, flow.Phase);
            Assert.IsFalse(flow.HasPending);
        }

        [Test] public void TryBegin_WithoutPending_ReturnsFalse()
        {
            var flow = Running();
            Assert.IsFalse(flow.TryBegin(out _));
            Assert.AreEqual(SceneFlowPhase.Running, flow.Phase);
        }

        [Test] public void Request_WhileLoading_ReturnsFalse()
        {
            var flow = Running();
            flow.Request(TestId.A);
            flow.TryBegin(out _);
            Assert.IsFalse(flow.Request(TestId.B));
            Assert.IsFalse(flow.RequestOpenOverlay(TestOverlay.X));
        }

        [Test] public void EnterRunning_FromLoading_AcceptsRequestsAgain()
        {
            var flow = Running();
            flow.Request(TestId.A);
            flow.TryBegin(out _);
            flow.EnterRunning();
            Assert.AreEqual(SceneFlowPhase.Running, flow.Phase);
            Assert.IsTrue(flow.Request(TestId.B));
        }

        [Test] public void EnterRunning_WhenRunning_Throws() =>
            Assert.Throws<InvalidOperationException>(() => Running().EnterRunning());

        [Test] public void OpenOverlay_WhenEmpty_ReturnsFalse() =>
            Assert.IsFalse(new SceneFlow<TestId, TestOverlay>().RequestOpenOverlay(TestOverlay.X));

        [Test] public void OpenOverlay_WhenRunning_IsAccepted()
        {
            var flow = Running();
            Assert.IsTrue(flow.RequestOpenOverlay(TestOverlay.X));
            Assert.IsTrue(flow.HasPending);
        }

        [Test] public void OpenOverlay_WhilePending_ReturnsFalse()
        {
            var flow = Running();
            Assert.IsTrue(flow.RequestOpenOverlay(TestOverlay.X));
            Assert.IsFalse(flow.RequestOpenOverlay(TestOverlay.Y));
        }

        [Test] public void OpenSameOverlayTwice_SecondReturnsFalse()
        {
            var flow = WithOverlays(TestOverlay.X);
            Assert.IsFalse(flow.RequestOpenOverlay(TestOverlay.X));
            Assert.IsTrue(flow.RequestOpenOverlay(TestOverlay.Y));
        }

        [Test] public void TryBegin_Open_PushesOverlayAndEntersLoading()
        {
            var flow = Running();
            flow.RequestOpenOverlay(TestOverlay.Y);
            Assert.IsTrue(flow.TryBegin(out var request));
            Assert.AreEqual(SceneRequestKind.OpenOverlay, request.Kind);
            Assert.AreEqual(TestOverlay.Y, request.Overlay);
            Assert.AreEqual(1, flow.OverlayCount);
            Assert.IsTrue(flow.IsOverlayOpen(TestOverlay.Y));
            Assert.AreEqual(SceneFlowPhase.Loading, flow.Phase);
        }

        [Test] public void CloseOverlay_WithoutOverlay_ReturnsFalse() =>
            Assert.IsFalse(Running().RequestCloseOverlay(0));

        [Test] public void TryBegin_Close_PopsAndReturnsResultAndStaysRunning()
        {
            var flow = WithOverlays(TestOverlay.X, TestOverlay.Y);
            Assert.IsTrue(flow.RequestCloseOverlay(7));
            Assert.IsTrue(flow.TryBegin(out var request));
            Assert.AreEqual(SceneRequestKind.CloseOverlay, request.Kind);
            Assert.AreEqual(TestOverlay.Y, request.Overlay);
            Assert.AreEqual(7, request.Result);
            Assert.AreEqual(1, flow.OverlayCount);
            Assert.IsFalse(flow.IsOverlayOpen(TestOverlay.Y));
            Assert.AreEqual(SceneFlowPhase.Running, flow.Phase);
            Assert.IsTrue(flow.Request(TestId.A));
        }

        [Test] public void TryBegin_Change_ClearsOverlays()
        {
            var flow = WithOverlays(TestOverlay.X, TestOverlay.Y);
            flow.Request(TestId.B);
            Assert.IsTrue(flow.TryBegin(out var request));
            Assert.AreEqual(SceneRequestKind.Change, request.Kind);
            Assert.AreEqual(TestId.B, request.Scene);
            Assert.AreEqual(0, flow.OverlayCount);
            Assert.AreEqual(SceneFlowPhase.Loading, flow.Phase);
        }

        [Test] public void CancelOverlayOpen_PopsAndReturnsToRunning()
        {
            var flow = WithOverlays(TestOverlay.X);
            flow.RequestOpenOverlay(TestOverlay.Y);
            flow.TryBegin(out _);
            flow.CancelOverlayOpen();
            Assert.AreEqual(1, flow.OverlayCount);
            Assert.IsFalse(flow.IsOverlayOpen(TestOverlay.Y));
            Assert.AreEqual(SceneFlowPhase.Running, flow.Phase);
        }

        [Test] public void CancelOverlayOpen_WhenNotLoadingOverlay_Throws()
        {
            var flow = WithOverlays(TestOverlay.X);
            Assert.Throws<InvalidOperationException>(() => flow.CancelOverlayOpen());
            flow.Request(TestId.A);
            flow.TryBegin(out _);
            Assert.Throws<InvalidOperationException>(() => flow.CancelOverlayOpen());
        }

        [Test] public void Restart_ClearsOverlaysAndPending()
        {
            var flow = WithOverlays(TestOverlay.X);
            flow.Request(TestId.A);
            flow.Restart();
            Assert.AreEqual(0, flow.OverlayCount);
            Assert.IsFalse(flow.HasPending);
            Assert.AreEqual(SceneFlowPhase.Running, flow.Phase);
        }

        [Test] public void OverlayAt_IsBottomFirst()
        {
            var flow = WithOverlays(TestOverlay.Y, TestOverlay.X);
            Assert.AreEqual(TestOverlay.Y, flow.OverlayAt(0));
            Assert.AreEqual(TestOverlay.X, flow.OverlayAt(1));
        }
    }
}

using System;
using NUnit.Framework;
using PictoArcher.Core;

namespace PictoArcher.Tests.EditMode
{
    public class SceneFlowTests
    {
        enum TestId { A, B }

        static SceneFlow<TestId> Running()
        {
            var flow = new SceneFlow<TestId>();
            flow.EnterRunning();
            return flow;
        }

        [Test] public void NewFlow_IsEmptyWithoutPending()
        {
            var flow = new SceneFlow<TestId>();
            Assert.AreEqual(SceneFlowPhase.Empty, flow.Phase);
            Assert.IsFalse(flow.HasPending);
        }

        [Test] public void Request_WhenEmpty_ReturnsFalse() =>
            Assert.IsFalse(new SceneFlow<TestId>().Request(TestId.A));

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
            Assert.IsTrue(flow.TryBeginLoad(out var id));
            Assert.AreEqual(TestId.A, id);
        }

        [Test] public void TryBeginLoad_WithPending_EntersLoading()
        {
            var flow = Running();
            flow.Request(TestId.B);
            Assert.IsTrue(flow.TryBeginLoad(out _));
            Assert.AreEqual(SceneFlowPhase.Loading, flow.Phase);
            Assert.IsFalse(flow.HasPending);
        }

        [Test] public void TryBeginLoad_WithoutPending_ReturnsFalse()
        {
            var flow = Running();
            Assert.IsFalse(flow.TryBeginLoad(out _));
            Assert.AreEqual(SceneFlowPhase.Running, flow.Phase);
        }

        [Test] public void Request_WhileLoading_ReturnsFalse()
        {
            var flow = Running();
            flow.Request(TestId.A);
            flow.TryBeginLoad(out _);
            Assert.IsFalse(flow.Request(TestId.B));
        }

        [Test] public void EnterRunning_FromLoading_AcceptsRequestsAgain()
        {
            var flow = Running();
            flow.Request(TestId.A);
            flow.TryBeginLoad(out _);
            flow.EnterRunning();
            Assert.AreEqual(SceneFlowPhase.Running, flow.Phase);
            Assert.IsTrue(flow.Request(TestId.B));
        }

        [Test] public void EnterRunning_WhenRunning_Throws() =>
            Assert.Throws<InvalidOperationException>(() => Running().EnterRunning());
    }
}

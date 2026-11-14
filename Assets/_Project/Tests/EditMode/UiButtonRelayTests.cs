using NUnit.Framework;
using PictoArcher.Game;
using UnityEngine;
using UnityEngine.UI;

namespace PictoArcher.Tests.EditMode
{
    public class UiButtonRelayTests
    {
        GameObject _go;

        [TearDown]
        public void TearDown()
        {
            if (_go != null) Object.DestroyImmediate(_go);
        }

        UiButtonRelay NewRelay()
        {
            _go = new GameObject("button", typeof(Button));
            return _go.AddComponent<UiButtonRelay>();
        }

        [Test] public void Click_IsConsumedOnce()
        {
            var relay = NewRelay();
            relay.Init();
            _go.GetComponent<Button>().onClick.Invoke();
            Assert.IsTrue(relay.ConsumeClicked());
            Assert.IsFalse(relay.ConsumeClicked());
        }

        [Test] public void AfterDispose_ClicksAreIgnored()
        {
            var relay = NewRelay();
            relay.Init();
            relay.Dispose();
            _go.GetComponent<Button>().onClick.Invoke();
            Assert.IsFalse(relay.ConsumeClicked());
        }
    }
}

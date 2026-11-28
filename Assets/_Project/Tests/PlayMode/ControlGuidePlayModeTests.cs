using System.Collections;
using NUnit.Framework;
using PictoArcher.Game;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace PictoArcher.Tests.PlayMode
{
    public class ControlGuidePlayModeTests
    {
        sealed class FakeTarget : ITarget
        {
            public Vector2 Position { get; set; }
            public bool IsAlive => true;
        }

        GameObject _canvas;
        ControlGuideView _view;
        FakeTarget _player;
        ControlGuideSystem _system;

        [SetUp]
        public void SetUp()
        {
            _canvas = new GameObject("Canvas", typeof(Canvas));
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/UI/ControlGuide.prefab");
            _view = Object.Instantiate(prefab, _canvas.transform).GetComponent<ControlGuideView>();
            var icons = AssetDatabase.LoadAssetAtPath<UiIconVisual>("Assets/_Project/Data/Visuals/UiIconVisual.asset");
            _player = new FakeTarget();
            _system = new ControlGuideSystem(_view, icons, _player);
        }

        [TearDown]
        public void TearDown() => Object.Destroy(_canvas);

        [UnityTest]
        public IEnumerator AtStart_GuideIsOnScreenWithPictures()
        {
            _system.Tick(0.1f);
            yield return null;
            Assert.IsTrue(_view.gameObject.activeInHierarchy, "戦いの始めに案内が出ている");
            foreach (var graphic in _view.GetComponentsInChildren<Graphic>(true))
            {
                Assert.IsInstanceOf<Image>(graphic, graphic.name + " は絵だけ（案内に文字を出さない）");
                if (graphic.gameObject == _view.gameObject) continue;   // 根は半透明の下地
                Assert.IsNotNull(((Image)graphic).sprite, graphic.name + " に絵が入っている");
            }
        }

        [UnityTest]
        public IEnumerator PlayerMoves_GuideDisappears()
        {
            _system.Tick(0.1f);
            yield return null;
            _player.Position = new Vector2(1f, 0f);
            _system.Tick(0.1f);
            yield return null;
            Assert.IsFalse(_view.gameObject.activeSelf, "動いたら消える");
            _player.Position = Vector2.zero;
            _system.Tick(0.1f);
            Assert.IsFalse(_view.gameObject.activeSelf, "戻っても二度と出ない");
        }
    }
}

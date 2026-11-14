using NUnit.Framework;
using PictoArcher.Game;
using UnityEngine;

namespace PictoArcher.Tests.EditMode
{
    public class LayersTests
    {
        [Test] public void Layers_MatchProjectSettings()
        {
            Assert.AreEqual(LayerMask.NameToLayer("Player"), Layers.Player);
            Assert.AreEqual(LayerMask.NameToLayer("Enemy"), Layers.Enemy);
            Assert.AreEqual(LayerMask.NameToLayer("PlayerShot"), Layers.PlayerShot);
            Assert.AreEqual(LayerMask.NameToLayer("Wall"), Layers.Wall);
        }

        [Test] public void Masks_AreOneBitPerLayer()
        {
            Assert.AreEqual(1 << 6, Layers.PlayerMask);
            Assert.AreEqual(1 << 7, Layers.EnemyMask);
            Assert.AreEqual(1 << 8, Layers.PlayerShotMask);
            Assert.AreEqual(1 << 9, Layers.WallMask);
        }
    }
}

using NUnit.Framework;
using UnityEditor;
using PictoArcher.Editor;

namespace PictoArcher.Tests.EditMode
{
    public class PlayFromRootSceneTests
    {
        const string Root = "Assets/_Project/Scenes/Root.unity";

        [TestCase("Assets/_Project/Scenes/Home.unity", "Home")]
        [TestCase("Assets/_Project/Scenes/Battle.unity", "Battle")]
        [TestCase("Assets/_Project/Scenes/Root.unity", "Root")]
        [TestCase("", "")]
        public void AnySceneOutsideSandbox_StartsFromRoot(string path, string name) =>
            Assert.AreEqual(Root, PlayFromRootScene.StartScenePath(path, name, Root));

        [Test]
        public void SandboxScene_StartsAsItIs() =>
            Assert.IsNull(PlayFromRootScene.StartScenePath("Assets/_Project/Scenes/Sandbox/Sandbox_A.unity", "Sandbox_A", Root));

        [TestCase("", "InitTestScene638123")]
        [TestCase("Assets/InitTestScene638123.unity", "InitTestScene638123")]
        public void TestRunnerScene_StartsAsItIs(string path, string name) =>
            Assert.IsNull(PlayFromRootScene.StartScenePath(path, name, Root), "テストの一時シーンを Root に替えると、PlayMode テストが動きません");

        [Test]
        public void NoBuildScenes_StartsAsItIs() =>
            Assert.IsNull(PlayFromRootScene.StartScenePath("Assets/_Project/Scenes/Home.unity", "Home", ""));

        [Test]
        public void BuildSceneZero_IsRoot()
        {
            Assert.Greater(EditorBuildSettings.scenes.Length, 0);
            Assert.AreEqual(Root, EditorBuildSettings.scenes[0].path, "ビルドも ▶ も Build の 0 番から始まります。0 番は Root にしてください");
        }
    }
}

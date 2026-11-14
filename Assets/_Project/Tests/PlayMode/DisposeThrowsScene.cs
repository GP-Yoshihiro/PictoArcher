using System;
using PictoArcher.Game;

namespace PictoArcher.Tests.PlayMode
{
    /// <summary>テスト用：Init で Home を依頼し、Dispose で例外を投げるシーン。</summary>
    public sealed class DisposeThrowsScene : SceneBase
    {
        public const string Message = "Dispose failed on purpose";

        public override void Init(GameContext context) => context.RequestScene(SceneId.Home);

        public override void Tick(float deltaTime) { }

        public override void Dispose() => throw new InvalidOperationException(Message);
    }
}

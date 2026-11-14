namespace PictoArcher.Game
{
    /// <summary>起動のシーン。全体の準備は Main.Awake で済んでいるので、すぐ Home へ進む。</summary>
    public sealed class BootScene : SceneBase
    {
        public override void Init(GameContext context)
        {
            // 起動したときだけのこと（セーブの読み込みなど）はここに足す。
            context.RequestScene(SceneId.Home);
        }

        public override void Tick(float deltaTime) { }

        public override void Dispose() { }
    }
}

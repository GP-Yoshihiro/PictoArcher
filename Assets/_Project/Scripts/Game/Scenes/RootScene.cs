using UnityEngine;

namespace PictoArcher.Game
{
    /// <summary>
    /// 初期化用のシーン。どこから再生しても、ほかのどのシーンより先に Init される。
    /// ゲーム全体の準備（設定やセーブの読み込みなど）はここに書く。Root と一緒にずっと動き、最後に Dispose される。
    /// </summary>
    public sealed class RootScene : SceneBase
    {
        [SerializeField] SceneId _firstScene = SceneId.Home;

        /// <summary>Root から再生したとき（ビルドも同じ）に最初に開く中身のシーン。</summary>
        public SceneId FirstScene => _firstScene;

        /// <summary>Init が済んだら true。テストで「Root が先に始まる」ことを確かめるためのもの。</summary>
        public bool IsInitialized { get; private set; }

        public override void Init(GameContext context)
        {
            // 起動したときだけのこと（セーブの読み込みなど）はここに足す。
            IsInitialized = true;
        }

        public override void Tick(float deltaTime) { }

        public override void Dispose()
        {
            IsInitialized = false;
        }
    }
}

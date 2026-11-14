using UnityEngine;

namespace PictoArcher.Game
{
    /// <summary>
    /// シーンのクラスの骨組み。シーンの一番上の階層にちょうど1つ置く。
    /// SceneTransform が Init → 毎フレーム Tick → 外すとき Dispose の順に呼ぶ。
    /// </summary>
    public abstract class SceneBase : MonoBehaviour
    {
        public abstract void Init(GameContext context);
        public abstract void Tick(float deltaTime);
        public virtual void FixedTick(float deltaTime) { }
        public virtual void LateTick(float deltaTime) { }
        public abstract void Dispose();

        /// <summary>重ねシーンだけが使う。true なら、開いている間も下のシーンを止めない。</summary>
        public virtual bool KeepsBelowRunning => false;

        /// <summary>すぐ上の重ねシーンが CloseOverlay で閉じたときに呼ばれる。</summary>
        public virtual void OnOverlayClosed(OverlayId id, int result) { }
    }
}

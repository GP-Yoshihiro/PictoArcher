using UnityEngine;

namespace PictoArcher.Game
{
    /// <summary>
    /// シーンのクラスの骨組み。シーンの一番上の階層にちょうど1つ置く。
    /// Main が Init → 毎フレーム Tick → 切り替えのとき Dispose の順に呼ぶ。
    /// </summary>
    public abstract class SceneBase : MonoBehaviour
    {
        public abstract void Init(GameContext context);
        public abstract void Tick(float deltaTime);
        public virtual void FixedTick(float deltaTime) { }
        public virtual void LateTick(float deltaTime) { }
        public abstract void Dispose();
    }
}

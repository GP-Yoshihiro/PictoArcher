using UnityEngine;

namespace PictoArcher.Game
{
    /// <summary>
    /// 「データから実行時の部品を作る」ScriptableObject の親。敵の動き・攻撃などの種類はこれを継承して足す。
    /// 継承してよいのは、このクラスと、このクラスを直接継承した抽象クラスだけ。
    /// </summary>
    public abstract class DefinitionAsset<TRuntime> : ScriptableObject
    {
        public abstract TRuntime Create();
    }
}

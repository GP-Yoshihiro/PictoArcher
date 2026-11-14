namespace PictoArcher.Core
{
    /// <summary>シーンの Tick から毎フレーム呼ばれるもの。</summary>
    public interface ITickable
    {
        void Tick(float deltaTime);
    }
}

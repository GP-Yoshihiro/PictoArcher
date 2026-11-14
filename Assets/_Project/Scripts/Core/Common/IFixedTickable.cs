namespace PictoArcher.Core
{
    /// <summary>シーンの FixedTick から物理の間隔で呼ばれるもの。</summary>
    public interface IFixedTickable
    {
        void FixedTick(float deltaTime);
    }
}

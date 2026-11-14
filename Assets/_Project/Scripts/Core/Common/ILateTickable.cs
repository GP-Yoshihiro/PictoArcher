namespace PictoArcher.Core
{
    /// <summary>シーンの LateTick から、全部の Tick のあとに呼ばれるもの。</summary>
    public interface ILateTickable
    {
        void LateTick(float deltaTime);
    }
}

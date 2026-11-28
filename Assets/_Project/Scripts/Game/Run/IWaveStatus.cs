namespace PictoArcher.Game
{
    /// <summary>HUD が読むウェーブの進み（上の帯の n/50）。RunSystem が実装し、HUD は毎フレーム読むだけ。</summary>
    public interface IWaveStatus
    {
        int CurrentWave { get; }
        int TotalWaves { get; }
    }
}

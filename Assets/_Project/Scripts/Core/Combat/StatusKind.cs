namespace PictoArcher.Core
{
    /// <summary>状態異常の種類。雷は状態異常ではなく、その場の連鎖ダメージ（DamageKind.Lightning）。</summary>
    public enum StatusKind
    {
        Burn,
        Freeze,
        Poison,
    }
}

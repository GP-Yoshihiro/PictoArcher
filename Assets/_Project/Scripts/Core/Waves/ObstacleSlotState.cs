namespace PictoArcher.Core
{
    /// <summary>切り替え中の障害物1つの状態。</summary>
    public enum ObstacleSlotState
    {
        /// <summary>薄い影を出している（当たり判定なし）。</summary>
        Shadow,
        /// <summary>影の時間は終わったが、プレイヤーと重なっているので現れるのを待っている。</summary>
        Blocked,
        /// <summary>現れた（当たり判定あり）。</summary>
        Shown,
    }
}

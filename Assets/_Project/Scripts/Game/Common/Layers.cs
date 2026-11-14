namespace PictoArcher.Game
{
    /// <summary>レイヤーの番号とマスク。ProjectSettings の TagManager と同じ番号にする（LayersTests が確かめる）。</summary>
    public static class Layers
    {
        public const int Player = 6;
        public const int Enemy = 7;
        public const int PlayerShot = 8;
        public const int Wall = 9;

        public const int PlayerMask = 1 << Player;
        public const int EnemyMask = 1 << Enemy;
        public const int PlayerShotMask = 1 << PlayerShot;
        public const int WallMask = 1 << Wall;
    }
}

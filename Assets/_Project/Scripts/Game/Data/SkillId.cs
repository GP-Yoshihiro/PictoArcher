namespace PictoArcher.Game
{
    // 設計書 §9 の26枚（ロードマップの表）。並びを変えるとアセットの値がずれるので、足すときは末尾に足す
    public enum SkillId
    {
        Heal, MaxHpUp, AttackUp, MoveSpeedUp, AttackSpeedUp,
        FrontShot, DiagonalShot, SideShot, RearShot, MultiShot,
        WallBounce, EnemyBounce, Pierce,
        Fire, Ice, Lightning, Poison,
        AreaUp, Shrink,
        DeathBlastVertical, DeathBlastHorizontal, DeathBlastRing,
        DeathPull, ExpGainUp, Orbiter, Wildcard,
    }
}

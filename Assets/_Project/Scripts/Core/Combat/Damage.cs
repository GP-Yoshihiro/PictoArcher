using System;

namespace PictoArcher.Core
{
    /// <summary>1回のダメージ。当てる側が作り、IHittable.TakeHit に渡す。量は強化を掛けたあとの値。</summary>
    public readonly struct Damage
    {
        public Damage(int amount, DamageKind kind = DamageKind.Direct)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount), "Damage must not be negative.");
            Amount = amount;
            Kind = kind;
        }

        public int Amount { get; }
        public DamageKind Kind { get; }
    }
}

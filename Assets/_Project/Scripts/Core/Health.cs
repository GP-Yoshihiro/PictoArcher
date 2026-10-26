using System;

namespace PictoArcher.Core
{
    public sealed class Health
    {
        public int Max { get; }
        public int Current { get; private set; }
        public bool IsDead => Current <= 0;
        public event Action Died;

        public Health(int max)
        {
            Max = max;
            Current = max;
        }

        public int ApplyDamage(int amount)
        {
            if (amount <= 0 || IsDead) return 0;
            int dealt = Math.Min(amount, Current);
            Current -= dealt;
            if (IsDead) Died?.Invoke();
            return dealt;
        }

        public int Heal(int amount)
        {
            if (amount <= 0 || IsDead) return 0;
            int healed = Math.Min(amount, Max - Current);
            Current += healed;
            return healed;
        }
    }
}

namespace PictoArcher.Core
{
    public sealed class FireTimer
    {
        public float Interval { get; set; }
        public float MoveFireRateMultiplier { get; set; }

        float _elapsed;

        public FireTimer(float interval, float moveFireRateMultiplier)
        {
            Interval = interval;
            MoveFireRateMultiplier = moveFireRateMultiplier;
            _elapsed = float.PositiveInfinity;
        }

        public bool Tick(float deltaTime, bool hasTarget, bool isMoving)
        {
            float effective = Interval / (isMoving ? MoveFireRateMultiplier : 1f);
            _elapsed = System.Math.Min(_elapsed + deltaTime, effective);
            if (!hasTarget || _elapsed < effective)
                return false;
            _elapsed = 0f;
            return true;
        }
    }
}

using UnityEngine;

namespace PictoArcher.Core
{
    /// <summary>
    /// 操作案内の最小版の動き。矢印キーを上→右→下→左の順に1つずつ光らせ、手（ドラッグ）も同じ向きへ動かして戻す。
    /// プレイヤーが始めの位置から dismissDistance 以上動いたら消え、二度と出ない。
    /// </summary>
    public sealed class ControlGuide
    {
        const float MinStepSeconds = 0.01f;
        const int KeyCount = 4;

        readonly Vector2 _start;
        readonly float _dismissDistanceSqr;
        readonly float _stepSeconds;
        float _elapsed;

        public ControlGuide(Vector2 start, float dismissDistance, float stepSeconds)
        {
            _start = start;
            _dismissDistanceSqr = dismissDistance * dismissDistance;
            _stepSeconds = Mathf.Max(MinStepSeconds, stepSeconds);
            IsVisible = true;
        }

        public bool IsVisible { get; private set; }

        /// <summary>光らせる矢印キー。0 上、1 右、2 下、3 左。</summary>
        public int HighlightedKey => (int)(_elapsed / _stepSeconds) % KeyCount;

        /// <summary>手の位置（長さ 0〜1）。光っているキーの向きへ出て、そのキーの間に真ん中へ戻る。</summary>
        public Vector2 DragOffset
        {
            get
            {
                float inStep = (_elapsed % _stepSeconds) / _stepSeconds;
                return KeyDirection(HighlightedKey) * Mathf.Sin(Mathf.PI * inStep);
            }
        }

        static Vector2 KeyDirection(int key)
        {
            switch (key)
            {
                case 0: return Vector2.up;
                case 1: return Vector2.right;
                case 2: return Vector2.down;
                default: return Vector2.left;
            }
        }

        public void Tick(float deltaTime, Vector2 playerPosition)
        {
            if (!IsVisible) return;
            // 1周（キー4つ分）で巻き戻し、長く置いても小数の誤差がたまらないようにする
            _elapsed = (_elapsed + deltaTime) % (_stepSeconds * KeyCount);
            if ((playerPosition - _start).sqrMagnitude >= _dismissDistanceSqr) IsVisible = false;
        }
    }
}

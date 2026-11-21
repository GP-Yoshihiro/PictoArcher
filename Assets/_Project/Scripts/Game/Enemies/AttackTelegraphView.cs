using UnityEngine;

namespace PictoArcher.Game
{
    /// <summary>
    /// 攻撃の予兆（赤い線、赤い円）のプレハブに付く部品。自分では動かない（EnemyUnit が毎 Tick に ShowLine / ShowCircle を呼ぶ）。
    /// 絵は線と円の2枚の SpriteRenderer（ベタ塗りの仮の絵を引き伸ばす）。文字は出さない。
    /// </summary>
    public sealed class AttackTelegraphView : MonoBehaviour
    {
        const float MinWidthRatio = 0.4f;

        [SerializeField] SpriteRenderer _line;
        [SerializeField] SpriteRenderer _circle;

        Transform _lineTransform;
        Transform _circleTransform;
        Vector2 _lineSize;
        Vector2 _circleSize;

        public void Init()
        {
            _lineTransform = _line.transform;
            _circleTransform = _circle.transform;
            _lineSize = _line.sprite.bounds.size;
            _circleSize = _circle.sprite.bounds.size;
            Hide();
        }

        /// <summary>origin から direction へ length の赤い線。progress（0〜1）で少しずつ太くなる。</summary>
        public void ShowLine(Vector2 origin, Vector2 direction, float length, float width, float progress)
        {
            _circle.enabled = false;
            _line.enabled = true;
            float thickness = width * Mathf.Lerp(MinWidthRatio, 1f, progress);
            _lineTransform.position = origin + direction.normalized * (length * 0.5f);
            _lineTransform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
            _lineTransform.localScale = new Vector3(length / _lineSize.x, thickness / _lineSize.y, 1f);
        }

        /// <summary>center に半径 radius の赤い円。progress（0〜1）で 0 から radius まで広がる。</summary>
        public void ShowCircle(Vector2 center, float radius, float progress)
        {
            _line.enabled = false;
            _circle.enabled = true;
            float diameter = radius * 2f * progress;
            _circleTransform.position = center;
            _circleTransform.rotation = Quaternion.identity;
            _circleTransform.localScale = new Vector3(diameter / _circleSize.x, diameter / _circleSize.y, 1f);
        }

        public void Hide()
        {
            _line.enabled = false;
            _circle.enabled = false;
        }
    }
}

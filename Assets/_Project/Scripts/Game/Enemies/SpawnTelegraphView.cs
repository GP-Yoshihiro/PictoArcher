using UnityEngine;

namespace PictoArcher.Game
{
    /// <summary>
    /// 出現予兆（赤い円と ! の絵）のプレハブに付く部品。自分では動かない（EnemySystem が TelegraphTimer を見て Apply を呼ぶ）。
    /// 文字は出さない。! は文字ではなく絵（スプライト）で用意する。
    /// </summary>
    public sealed class SpawnTelegraphView : MonoBehaviour
    {
        [SerializeField] SpriteRenderer _circle;
        [SerializeField] SpriteRenderer _mark;

        Transform _transform;
        float _endScale;

        /// <summary>endScale は予兆の終わりでの大きさ（始めを 1 とした倍率。SpawnTelegraphSettings の値）。</summary>
        public void Init(Vector2 position, float endScale)
        {
            _transform = transform;
            _transform.position = position;
            _endScale = endScale;
        }

        /// <summary>progress は 0〜1。点滅しながら少しずつ大きくなる。</summary>
        public void Apply(float progress, bool visible)
        {
            float scale = Mathf.Lerp(1f, _endScale, progress);
            _transform.localScale = new Vector3(scale, scale, 1f);
            _circle.enabled = visible;
            _mark.enabled = visible;
        }
    }
}

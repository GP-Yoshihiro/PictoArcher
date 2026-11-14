using System.Collections.Generic;
using UnityEngine;

namespace PictoArcher.Game
{
    /// <summary>
    /// 当たり判定の通知を受け取って溜めておくだけの部品。当たり判定の通知を書いてよいのはこのクラスだけ。
    /// 使う側（System）は Tick の中で DrainEntered と Touching を見て処理する。
    /// Destroy された相手が Touching に残ることがある（Exit が来ないため）。使う側で <c>== null</c> を確かめる。
    /// </summary>
    public sealed class CollisionRelay2D : MonoBehaviour
    {
        readonly List<Collider2D> _entered = new List<Collider2D>();
        readonly HashSet<Collider2D> _touching = new HashSet<Collider2D>();

        /// <summary>今触れている相手。</summary>
        public IReadOnlyCollection<Collider2D> Touching => _touching;

        /// <summary>前に呼んでから入ってきた相手を <paramref name="into"/> に足して数を返し、記録を消す。</summary>
        public int DrainEntered(List<Collider2D> into)
        {
            int count = _entered.Count;
            into.AddRange(_entered);
            _entered.Clear();
            return count;
        }

        void OnTriggerEnter2D(Collider2D other) => Enter(other);
        void OnTriggerExit2D(Collider2D other) => _touching.Remove(other);
        void OnCollisionEnter2D(Collision2D collision) => Enter(collision.collider);
        void OnCollisionExit2D(Collision2D collision) => _touching.Remove(collision.collider);

        void Enter(Collider2D other)
        {
            _entered.Add(other);
            _touching.Add(other);
        }
    }
}

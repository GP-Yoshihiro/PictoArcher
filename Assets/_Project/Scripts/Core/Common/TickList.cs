using System.Collections.Generic;

namespace PictoArcher.Core
{
    /// <summary>
    /// 足す・消すを予約しておき、ApplyPending で反映する一覧。
    /// for で回している最中に Add/Remove しても壊れない（反映は次の ApplyPending）。
    /// </summary>
    public sealed class TickList<T> where T : class
    {
        readonly List<T> _items = new List<T>();
        readonly List<T> _toAdd = new List<T>();
        readonly List<T> _toRemove = new List<T>();

        public int Count => _items.Count;

        public T this[int index] => _items[index];

        public void Add(T item)
        {
            if (_toRemove.Remove(item)) return;
            if (_items.Contains(item) || _toAdd.Contains(item)) return;
            _toAdd.Add(item);
        }

        public void Remove(T item)
        {
            if (_toAdd.Remove(item)) return;
            if (!_items.Contains(item) || _toRemove.Contains(item)) return;
            _toRemove.Add(item);
        }

        public void ApplyPending()
        {
            for (int i = 0; i < _toRemove.Count; i++) _items.Remove(_toRemove[i]);
            _toRemove.Clear();
            for (int i = 0; i < _toAdd.Count; i++) _items.Add(_toAdd[i]);
            _toAdd.Clear();
        }

        public void Clear()
        {
            _items.Clear();
            _toAdd.Clear();
            _toRemove.Clear();
        }
    }
}

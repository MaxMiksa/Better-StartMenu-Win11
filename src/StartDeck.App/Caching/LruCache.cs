using System;
using System.Collections.Generic;

namespace StartDeck.Caching;

/// <summary>
/// Simple LRU cache with soft limit eviction.
/// </summary>
public sealed class LruCache<TKey, TValue> where TKey : notnull
{
    private readonly int _capacity;
    private readonly Dictionary<TKey, LinkedListNode<(TKey Key, TValue Value)>> _map;
    private readonly LinkedList<(TKey Key, TValue Value)> _list;

    public LruCache(int capacity)
    {
        if (capacity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity));
        }

        _capacity = capacity;
        _map = new Dictionary<TKey, LinkedListNode<(TKey, TValue)>>(capacity);
        _list = new LinkedList<(TKey, TValue)>();
    }

    public bool TryGet(TKey key, out TValue value)
    {
        if (_map.TryGetValue(key, out var node))
        {
            _list.Remove(node);
            _list.AddFirst(node);
            value = node.Value.Value;
            return true;
        }

        value = default!;
        return false;
    }

    public void AddOrUpdate(TKey key, TValue value)
    {
        if (_map.TryGetValue(key, out var node))
        {
            node.Value = (key, value);
            _list.Remove(node);
            _list.AddFirst(node);
            return;
        }

        var newNode = new LinkedListNode<(TKey, TValue)>((key, value));
        _list.AddFirst(newNode);
        _map[key] = newNode;

        if (_map.Count > _capacity)
        {
            Evict( Math.Max(1, _capacity / 10) ); // evict 10% oldest
        }
    }

    public void Clear()
    {
        _map.Clear();
        _list.Clear();
    }

    public void TrimToCapacity()
    {
        while (_map.Count > _capacity)
        {
            Evict(1);
        }
    }

    private void Evict(int count)
    {
        for (var i = 0; i < count; i++)
        {
            var last = _list.Last;
            if (last == null)
            {
                return;
            }

            _map.Remove(last.Value.Key);
            _list.RemoveLast();
        }
    }
}

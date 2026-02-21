using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Shared.Infra.Module.Observability.Logging.Sinks.Postgres;

/// <summary>
/// Dictionary que preserva a ordem de inserção
/// Necessário para garantir que as colunas do PostgreSQL sejam criadas na ordem do JSON
/// </summary>
public class OrderedDictionary<TKey, TValue> : IDictionary<TKey, TValue> where TKey : notnull
{
    private readonly List<KeyValuePair<TKey, TValue>> _list = new();
    private readonly Dictionary<TKey, int> _indexMap = new();

    public TValue this[TKey key]
    {
        get => _list[_indexMap[key]].Value;
        set
        {
            if (_indexMap.TryGetValue(key, out var index))
            {
                _list[index] = new KeyValuePair<TKey, TValue>(key, value);
            }
            else
            {
                Add(key, value);
            }
        }
    }

    public ICollection<TKey> Keys => _list.Select(x => x.Key).ToList();
    public ICollection<TValue> Values => _list.Select(x => x.Value).ToList();
    public int Count => _list.Count;
    public bool IsReadOnly => false;

    public void Add(TKey key, TValue value)
    {
        if (_indexMap.ContainsKey(key))
            throw new ArgumentException($"Key '{key}' already exists");

        _indexMap[key] = _list.Count;
        _list.Add(new KeyValuePair<TKey, TValue>(key, value));
    }

    public void Add(KeyValuePair<TKey, TValue> item) => Add(item.Key, item.Value);

    public void Clear()
    {
        _list.Clear();
        _indexMap.Clear();
    }

    public bool Contains(KeyValuePair<TKey, TValue> item) => 
        _indexMap.TryGetValue(item.Key, out var index) && 
        EqualityComparer<TValue>.Default.Equals(_list[index].Value, item.Value);

    public bool ContainsKey(TKey key) => _indexMap.ContainsKey(key);

    public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) => 
        _list.CopyTo(array, arrayIndex);

    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => _list.GetEnumerator();

    public bool Remove(TKey key)
    {
        if (!_indexMap.TryGetValue(key, out var index))
            return false;

        _list.RemoveAt(index);
        _indexMap.Remove(key);

        // Reindex
        for (int i = index; i < _list.Count; i++)
        {
            _indexMap[_list[i].Key] = i;
        }

        return true;
    }

    public bool Remove(KeyValuePair<TKey, TValue> item) => 
        Contains(item) && Remove(item.Key);

    public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        if (_indexMap.TryGetValue(key, out var index))
        {
            value = _list[index].Value;
            return true;
        }
        value = default;
        return false;
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}


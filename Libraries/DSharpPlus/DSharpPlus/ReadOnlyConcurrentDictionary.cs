using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Threading;

namespace DSharpPlus
{
    /// <summary>
    /// Read-only view of a given <see cref="ConcurrentDictionary{TKey,TValue}"/>.
    /// </summary>
    /// <remarks>
    /// This type exists because <see cref="ConcurrentDictionary{TKey,TValue}"/> is not an
    /// <see cref="IReadOnlyDictionary{TKey,TValue}"/> in .NET Standard 1.1.
    /// </remarks>
    /// <typeparam name="TKey">The type of keys in the dictionary.</typeparam>
    /// <typeparam name="TValue">The type of values in the dictionary.</typeparam>
    internal readonly struct ReadOnlyConcurrentDictionary<TKey, TValue> : IReadOnlyDictionary<TKey, TValue>
    {
        private readonly ConcurrentDictionary<TKey, TValue> _underlyingDict;

        /// <summary>
        /// Creates a new read-only view of the given dictionary.
        /// </summary>
        /// <param name="underlyingDict">Dictionary to create a view over.</param>
        public ReadOnlyConcurrentDictionary(ConcurrentDictionary<TKey, TValue> underlyingDict)
        {
            _underlyingDict = underlyingDict;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => _underlyingDict.GetEnumerator();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable) _underlyingDict).GetEnumerator();

        public int Count => _underlyingDict.Count;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ContainsKey(TKey key) => _underlyingDict.ContainsKey(key);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetValue(TKey key, out TValue value) => _underlyingDict.TryGetValue(key, out value);

        public TValue this[TKey key] => _underlyingDict[key];

        public IEnumerable<TKey> Keys => _underlyingDict.Keys;

        public IEnumerable<TValue> Values => _underlyingDict.Values;
    }

    internal readonly struct ReadOnlyList<T> : IReadOnlyList<T>
    {
        private readonly List<T> _underlyingList;
        
        public ReadOnlyList(List<T> list)
        {
            _underlyingList = list;
        }

        public T this[int index] => _underlyingList[index];        
        public int Count => _underlyingList.Count;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IEnumerator<T> GetEnumerator() => ((IReadOnlyList<T>)_underlyingList).GetEnumerator();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        IEnumerator IEnumerable.GetEnumerator() => ((IReadOnlyList<T>)_underlyingList).GetEnumerator();
    }
}
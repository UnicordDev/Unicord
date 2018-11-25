using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace WamWooWam.Core.Serialisation
{
    public class JsonDictionary<TKey, TValue> : IDictionary<TKey, TValue>
    {
        private IDictionary<TKey, TValue> _internalDictionary = new Dictionary<TKey, TValue>();
        private string _filePath = null;
        private bool _autoSave = true;

        public TValue this[TKey key] { get => _internalDictionary[key]; set => _internalDictionary[key] = value; }

        public ICollection<TKey> Keys => _internalDictionary.Keys;

        public ICollection<TValue> Values => _internalDictionary.Values;

        public int Count => _internalDictionary.Count;

        public bool IsReadOnly => _internalDictionary.IsReadOnly;

        /// <summary>
        /// Creates a new JsonDictionary
        /// </summary>
        /// <param name="path">The file on disk to back the dictionary</param>
        /// <param name="autosave">Enables/Disables autosave after any modification. May cause performance issues.</param>
        public JsonDictionary(string path, bool autosave = true)
        {
            if (File.Exists(path))
            {
                _internalDictionary = JsonConvert.DeserializeObject<IDictionary<TKey, TValue>>(File.ReadAllText(path));
            }
            else
            {
                File.WriteAllText(path, JsonConvert.SerializeObject(_internalDictionary));
            }

            _filePath = path;
            _autoSave = autosave;
        }

        public void Save()
        {
            File.WriteAllText(_filePath, JsonConvert.SerializeObject(_internalDictionary));
        }

        public void Add(TKey key, TValue value)
        {
            _internalDictionary.Add(key, value);
            if (_autoSave)
            {
                Save();
            }
        }

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            _internalDictionary.Add(item);
            if (_autoSave)
            {
                Save();
            }
        }

        public void Clear()
        {
            _internalDictionary.Clear();
            if (_autoSave)
            {
                Save();
            }
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            return _internalDictionary.Contains(item);
        }

        public bool ContainsKey(TKey key)
        {
            return _internalDictionary.ContainsKey(key);
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            _internalDictionary.CopyTo(array, arrayIndex);
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return _internalDictionary.GetEnumerator();
        }

        public bool Remove(TKey key)
        {
            bool result = _internalDictionary.Remove(key);
            if (_autoSave)
            {
                Save();
            }

            return result;
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            bool result = _internalDictionary.Remove(item);
            if (_autoSave)
            {
                Save();
            }

            return result;
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            return _internalDictionary.TryGetValue(key, out value);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _internalDictionary.GetEnumerator();
        }
    }
}

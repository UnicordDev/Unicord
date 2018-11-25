using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace WamWooWam.Core.Collections
{
    public class JsonList<T> : IList<T>
    {
        public JsonListOptions Options { get; set; }
        public List<T> BaseList { get; set; }
        public string BasePath { get; set; }
        public int Count => BaseList.Count;
        public bool IsReadOnly => false;
        public T this[int index] { get => BaseList[index]; set => BaseList[index] = value; }

        /// <summary>
        /// Initialises a new JSON backed list.
        /// </summary>
        /// <param name="FilePath">The path to the file backing the list.</param>
        public JsonList(string FilePath, JsonListOptions Options)
        {
            BasePath = FilePath;
            if (File.Exists(FilePath))
            {
                BaseList = JsonConvert.DeserializeObject<List<T>>(File.ReadAllText(FilePath));
            }
            else
            {
                if (Options?.CreateIfMissing == true)
                {
                    BaseList = new List<T>();
                    File.WriteAllText(FilePath, JsonConvert.SerializeObject(BaseList));
                }
                else
                {
                    throw new FileNotFoundException("The file backing the JsonList was not found.", FilePath);
                }
            }
        }

        public JsonList(string FilePath, JsonListOptions Options, IEnumerable<T> OriginalList)
        {
            BasePath = FilePath;
            BaseList = OriginalList.ToList();
            File.WriteAllText(FilePath, JsonConvert.SerializeObject(BaseList));
        }

        public static JsonList<T> ToJsonList(IEnumerable<T> List, JsonList<T> OriginalList)
        {
            JsonList<T> NewList = new JsonList<T>(OriginalList.BasePath, OriginalList.Options, List);
            NewList.SaveList();
            return NewList;
        }

        private void FileChanged(object sender, FileSystemEventArgs e)
        {
            RefreshList();
        }

        public void SaveList()
        {
            File.WriteAllText(BasePath, JsonConvert.SerializeObject(BaseList));
        }

        public async Task SaveListAsync()
        {
            File.WriteAllText(BasePath, await JsonConvertAsync.SerializeObjectAsync(BaseList));
        }

        public void RefreshList()
        {
            BaseList = JsonConvert.DeserializeObject<List<T>>(File.ReadAllText(BasePath));
        }

        #region IList
        public IEnumerator<T> GetEnumerator()
        {
            return BaseList.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return BaseList.GetEnumerator();
        }

        public int IndexOf(T item)
        {
            return BaseList.IndexOf(item);
        }

        public void Insert(int index, T item)
        {
            BaseList.Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            BaseList.RemoveAt(index);
        }

        public void RemoveAll(Predicate<T> match)
        {
            BaseList.RemoveAll(match);
        }

        public void Add(T item)
        {
            BaseList.Add(item);
        }

        public void Clear()
        {
            BaseList.Clear();
        }

        public bool Contains(T item)
        {
            return BaseList.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            BaseList.CopyTo(array, arrayIndex);
        }

        public bool Remove(T item)
        {
            return BaseList.Remove(item);
        }
        #endregion
    }

    public class JsonListOptions
    {
        public JsonListOptions()
        {
            CreateIfMissing = true;
            ReCreateOnError = true;
            ReloadOnChange = true;
        }

        public bool CreateIfMissing { get; set; }
        public bool ReCreateOnError { get; set; }
        public bool ReloadOnChange { get; set; }
    }

}

using System;
using System.Collections;
using System.Collections.Generic;

namespace QuantConnect.Data.Market
{
    /// <summary>
    /// Provides a base class for types holding base data instances keyed by symbol
    /// </summary>
    public class DataDictionary<T> : IDictionary<string, T>
    {
        // storage for the data
        private readonly IDictionary<string, T> _data = new Dictionary<string, T>();

        public DataDictionary()
        {
        }

        public DataDictionary(DateTime frontier)
        {
            Time = frontier;
        }

        /// <summary>
        /// Gets or sets the time associated with this collection of data
        /// </summary>
        public DateTime Time { get; set; }

        public IEnumerator<KeyValuePair<string, T>> GetEnumerator()
        {
            return _data.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable) _data).GetEnumerator();
        }

        public void Add(KeyValuePair<string, T> item)
        {
            _data.Add(item);
        }

        public void Clear()
        {
            _data.Clear();
        }

        public bool Contains(KeyValuePair<string, T> item)
        {
            return _data.Contains(item);
        }

        public void CopyTo(KeyValuePair<string, T>[] array, int arrayIndex)
        {
            _data.CopyTo(array, arrayIndex);
        }

        public bool Remove(KeyValuePair<string, T> item)
        {
            return _data.Remove(item);
        }

        public int Count
        {
            get { return _data.Count; }
        }

        public bool IsReadOnly
        {
            get { return _data.IsReadOnly; }
        }

        public bool ContainsKey(string key)
        {
            return _data.ContainsKey(key);
        }

        public void Add(string key, T value)
        {
            _data.Add(key, value);
        }

        public bool Remove(string key)
        {
            return _data.Remove(key);
        }

        public bool TryGetValue(string key, out T value)
        {
            return _data.TryGetValue(key, out value);
        }

        public T this[string key]
        {
            get
            {
                T data;
                if (TryGetValue(key, out data))
                {
                    return data;
                }
                throw new KeyNotFoundException(string.Format("'{0}' wasn't found in the {1} object, likely because there was no-data at this moment in time and it wasn't possible to fillforward historical data. Please check the data exists before accessing it with data.ContainsKey(\"{0}\")", key, GetType().Name));
            }
            set
            {
                if (!_data.ContainsKey(key))
                {
                    throw new KeyNotFoundException(string.Format("'{0}' wasn't found in the {1} object, likely because there was no-data at this moment in time and it wasn't possible to fillforward historical data. Please check the data exists before accessing it with data.ContainsKey(\"{0}\")", key, GetType().Name));
                }
                _data[key] = value;
            }
        }

        public ICollection<string> Keys
        {
            get { return _data.Keys; }
        }

        public ICollection<T> Values
        {
            get { return _data.Values; }
        }
    }

    /// <summary>
    /// Provides extension methods for the DataDictionary class
    /// </summary>
    public static class DataDictionaryExtensions
    {
        /// <summary>
        /// Provides a convenience method for adding a base data instance to our data dictionary
        /// </summary>
        public static void Add<T>(this DataDictionary<T> dictionary, T data)
            where T : BaseData
        {
            dictionary.Add(data.Symbol, data);
        }
    }
}
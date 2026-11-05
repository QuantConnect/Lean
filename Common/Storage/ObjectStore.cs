/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
*/

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using Newtonsoft.Json;
using QuantConnect.Interfaces;
using QuantConnect.Logging;
using QuantConnect.Packets;

namespace QuantConnect.Storage
{
    /// <summary>
    /// Helper class for easier access to <see cref="IObjectStore"/> methods. A key that does not follow the key
    /// format (english letters, numbers, spaces and the characters '.', '/', '\', '_', '#', '-', '$' and '=', never
    /// two '.' in a row, e.g. 'folder/trade_log-2024.v2.csv') is saved under its sanitized version, then found and
    /// listed by either name, with a warning the first time it happens. An object already stored under an unsupported name
    /// is read as is until a save through that name creates the sanitized one
    /// </summary>
    public class ObjectStore : IObjectStore
    {
        // The single source of the key charset rules, also enforced by the LocalObjectStore implementation
        private static readonly Regex UnsupportedKeyCharactersRegex = new(@"[^a-zA-Z0-9\\/_#\-\$= .]", RegexOptions.Compiled);
        // '..' is never allowed: it would leave the storage root
        private static readonly Regex ConsecutiveDotsRegex = new(@"\.{2,}", RegexOptions.Compiled);

        private readonly IObjectStore _store;
        private readonly Action<string> _warn;
        // The unsupported keys saved in this run and the sanitized name each one is stored under
        private readonly ConcurrentDictionary<string, string> _sanitizedKeys = new();
        private int _warned;

        /// <summary>
        /// Gets the maximum storage limit in bytes
        /// </summary>
        public long MaxSize => _store.MaxSize;

        /// <summary>
        /// Gets the maximum number of files allowed
        /// </summary>
        public int MaxFiles => _store.MaxFiles;

        /// <summary>
        /// Event raised each time there's an error
        /// </summary>
        public event EventHandler<ObjectStoreErrorRaisedEventArgs> ErrorRaised
        {
            add { _store.ErrorRaised += value; }
            remove { _store.ErrorRaised -= value; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectStore"/> class
        /// </summary>
        /// <param name="store">The <see cref="IObjectStore"/> instance to wrap</param>
        /// <param name="warn">Receives the warnings about sanitized keys, the engine log by default</param>
        public ObjectStore(IObjectStore store, Action<string> warn = null)
        {
            _store = store;
            _warn = warn ?? (message => Log.Trace(message));
        }

        /// <summary>
        /// Initializes the object store
        /// </summary>
        /// <param name="userId">The user id</param>
        /// <param name="projectId">The project id</param>
        /// <param name="userToken">The user token</param>
        /// <param name="controls">The job controls instance</param>
        /// <param name="algorithmMode">The algorithm mode</param>
        public void Initialize(int userId, int projectId, string userToken, Controls controls, AlgorithmMode algorithmMode)
        {
            _store.Initialize(userId, projectId, userToken, controls, algorithmMode);
        }

        /// <summary>
        /// Returns the file paths present in the object store. This is specially useful not to load the object store into memory.
        /// An unsupported key saved in this run is listed next to the sanitized name its object is stored under.
        /// The store is listed on first use, and Contains finds a key the way <see cref="ContainsKey"/> does, without listing it
        /// </summary>
        public ICollection<string> Keys => new KeyCollection(ListKeys, ContainsKey);

        /// <summary>
        /// Will clear the object store state cache. This is useful when the object store is used concurrently by nodes which want to share information
        /// </summary>
        public void Clear() => _store.Clear();

        /// <summary>
        /// Determines whether the store contains data for the specified path
        /// </summary>
        /// <param name="path">The object path</param>
        /// <returns>True if the key was found</returns>
        public bool ContainsKey(string path)
        {
            return _store.ContainsKey(KeyToRead(path));
        }

        /// <summary>
        /// Returns the object data for the specified path
        /// </summary>
        /// <param name="path">The object path</param>
        /// <returns>A byte array containing the data</returns>
        public byte[] ReadBytes(string path)
        {
            return _store.ReadBytes(KeyToRead(path));
        }

        /// <summary>
        /// Saves the object data for the specified path
        /// </summary>
        /// <param name="path">The object key: english letters, numbers, spaces and the characters '.', '/', '\', '_', '#', '-', '$' and '=', never '..', e.g. 'folder/trade_log-2024.v2.csv'. Other characters are replaced by '_'</param>
        /// <param name="contents">The object data</param>
        /// <returns>True if the save operation was successful</returns>
        public bool SaveBytes(string path, byte[] contents)
        {
            return _store.SaveBytes(KeyToSave(path), contents);
        }

        /// <summary>
        /// Deletes the object data for the specified path
        /// </summary>
        /// <param name="path">The object path</param>
        /// <returns>True if the delete operation was successful</returns>
        public bool Delete(string path)
        {
            return _store.Delete(KeyToRead(path));
        }

        /// <summary>
        /// Returns the file path for the specified path
        /// </summary>
        /// <param name="path">The object path</param>
        /// <returns>The path for the file</returns>
        public string GetFilePath(string path)
        {
            return _store.GetFilePath(KeyToRead(path));
        }

        /// <summary>
        /// Returns the string object data for the specified path
        /// </summary>
        /// <param name="path">The object path</param>
        /// <param name="encoding">The string encoding used</param>
        /// <returns>A string containing the data</returns>
        public string Read(string path, Encoding encoding = null)
        {
            encoding = encoding ?? Encoding.UTF8;

            var data = ReadBytes(path);
            return data != null ? encoding.GetString(data) : null;
        }

        /// <summary>
        /// Returns the string object data for the specified path
        /// </summary>
        /// <param name="path">The object path</param>
        /// <param name="encoding">The string encoding used</param>
        /// <returns>A string containing the data</returns>
        public string ReadString(string path, Encoding encoding = null)
        {
            return Read(path, encoding);
        }

        /// <summary>
        /// Returns the object data for the specified path as a string. Alias of <see cref="Read(string, Encoding)"/>
        /// </summary>
        /// <param name="path">The object path</param>
        /// <param name="encoding">The string encoding used, <see cref="Encoding.UTF8"/> by default</param>
        /// <returns>A string containing the data</returns>
        public string ReadText(string path, Encoding encoding = null)
        {
            return Read(path, encoding);
        }

        /// <summary>
        /// Returns the JSON deserialized object data for the specified path
        /// </summary>
        /// <param name="path">The object path</param>
        /// <param name="encoding">The string encoding used</param>
        /// <param name="settings">The settings used by the JSON deserializer</param>
        /// <returns>An object containing the data</returns>
        public T ReadJson<T>(string path, Encoding encoding = null, JsonSerializerSettings settings = null)
        {
            encoding = encoding ?? Encoding.UTF8;

            var json = Read(path, encoding);
            return JsonConvert.DeserializeObject<T>(json, settings);
        }

        /// <summary>
        /// Returns the XML deserialized object data for the specified path
        /// </summary>
        /// <param name="path">The object path</param>
        /// <param name="encoding">The string encoding used</param>
        /// <returns>An object containing the data</returns>
        public T ReadXml<T>(string path, Encoding encoding = null)
        {
            encoding = encoding ?? Encoding.UTF8;

            var xml = Read(path, encoding);

            var serializer = new XmlSerializer(typeof(T));
            using (var reader = new StringReader(xml))
            {
                return (T)serializer.Deserialize(reader);
            }
        }

        /// <summary>
        /// Saves the data from a local file path associated with the specified path
        /// </summary>
        /// <remarks>If the file does not exist it will throw an exception</remarks>
        /// <param name="path">The object key: english letters, numbers, spaces and the characters '.', '/', '\', '_', '#', '-', '$' and '=', never '..', e.g. 'folder/trade_log-2024.v2.csv'. Other characters are replaced by '_'</param>
        /// <returns>True if the object was saved successfully</returns>
        public bool Save(string path)
        {
            // Check the file exists
            var filePath = GetFilePath(path);
            if (!File.Exists(filePath))
            {
                throw new ArgumentException($"There is no file associated with path {path} in '{filePath}'");
            }
            var bytes = File.ReadAllBytes(filePath);

            return SaveBytes(path, bytes);
        }

        /// <summary>
        /// Saves the object data in text format for the specified path
        /// </summary>
        /// <param name="path">The object key: english letters, numbers, spaces and the characters '.', '/', '\', '_', '#', '-', '$' and '=', never '..', e.g. 'folder/trade_log-2024.v2.csv'. Other characters are replaced by '_'</param>
        /// <param name="text">The string object to be saved</param>
        /// <param name="encoding">The string encoding used, <see cref="Encoding.UTF8"/> by default</param>
        /// <returns>True if the object was saved successfully</returns>
        public bool Save(string path, string text, Encoding encoding = null)
        {
            encoding ??= Encoding.UTF8;
            return SaveBytes(path, encoding.GetBytes(text));
        }

        /// <summary>
        /// Saves the object data in text format for the specified path
        /// </summary>
        /// <param name="path">The object key: english letters, numbers, spaces and the characters '.', '/', '\', '_', '#', '-', '$' and '=', never '..', e.g. 'folder/trade_log-2024.v2.csv'. Other characters are replaced by '_'</param>
        /// <param name="text">The string object to be saved</param>
        /// <param name="encoding">The string encoding used</param>
        /// <returns>True if the object was saved successfully</returns>
        public bool SaveString(string path, string text, Encoding encoding = null)
        {
            encoding = encoding ?? Encoding.UTF8;

            return SaveBytes(path, encoding.GetBytes(text));
        }

        /// <summary>
        /// Saves the object data in text format for the specified path.
        /// Alias of <see cref="Save(string, string, Encoding)"/>
        /// </summary>
        /// <param name="path">The object key: english letters, numbers, spaces and the characters '.', '/', '\', '_', '#', '-', '$' and '=', never '..', e.g. 'folder/trade_log-2024.v2.csv'. Other characters are replaced by '_'</param>
        /// <param name="text">The string object to be saved</param>
        /// <param name="encoding">The string encoding used, <see cref="Encoding.UTF8"/> by default</param>
        /// <returns>True if the object was saved successfully</returns>
        public bool SaveText(string path, string text, Encoding encoding = null)
        {
            return Save(path, text, encoding);
        }

        /// <summary>
        /// Saves the object data in JSON format for the specified path
        /// </summary>
        /// <param name="path">The object key: english letters, numbers, spaces and the characters '.', '/', '\', '_', '#', '-', '$' and '=', never '..', e.g. 'folder/trade_log-2024.v2.csv'. Other characters are replaced by '_'</param>
        /// <param name="obj">The object to be saved</param>
        /// <param name="encoding">The string encoding used</param>
        /// <param name="settings">The settings used by the JSON serializer</param>
        /// <returns>True if the object was saved successfully</returns>
        public bool SaveJson<T>(string path, T obj, Encoding encoding = null, JsonSerializerSettings settings = null)
        {
            encoding = encoding ?? Encoding.UTF8;

            var json = JsonConvert.SerializeObject(obj, settings);
            return SaveString(path, json, encoding);
        }

        /// <summary>
        /// Saves the object data in XML format for the specified path
        /// </summary>
        /// <param name="path">The object key: english letters, numbers, spaces and the characters '.', '/', '\', '_', '#', '-', '$' and '=', never '..', e.g. 'folder/trade_log-2024.v2.csv'. Other characters are replaced by '_'</param>
        /// <param name="obj">The object to be saved</param>
        /// <param name="encoding">The string encoding used</param>
        /// <returns>True if the object was saved successfully</returns>
        public bool SaveXml<T>(string path, T obj, Encoding encoding = null)
        {
            encoding = encoding ?? Encoding.UTF8;

            using (var writer = new StringWriter())
            {
                var serializer = new XmlSerializer(typeof(T));
                serializer.Serialize(writer, obj);

                var xml = writer.ToString();
                return SaveString(path, xml, encoding);
            }
        }

        /// <summary>Returns an enumerator that iterates through the collection.</summary>
        /// <returns>A <see cref="T:System.Collections.Generic.IEnumerator`1" /> that can be used to iterate through the collection.</returns>
        /// <filterpriority>1</filterpriority>
        public IEnumerator<KeyValuePair<string, byte[]>> GetEnumerator()
        {
            return _store.GetEnumerator();
        }

        /// <summary>Returns an enumerator that iterates through a collection.</summary>
        /// <returns>An <see cref="T:System.Collections.IEnumerator" /> object that can be used to iterate through the collection.</returns>
        /// <filterpriority>2</filterpriority>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)_store).GetEnumerator();
        }

        /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
        /// <filterpriority>2</filterpriority>
        public void Dispose()
        {
            _store.Dispose();
        }

        /// <summary>
        /// Throws if the given key does not follow the key format, stating every problem found and the rules.
        /// The last line of defense of the store implementations, as this class only hands them supported keys
        /// </summary>
        /// <param name="key">The object store key to validate</param>
        /// <exception cref="ArgumentException">The key is not supported</exception>
        internal static void ValidateKey(string key)
        {
            var issues = GetKeyIssues(key);
            if (issues != null)
            {
                throw new ArgumentException($"ObjectStore: path is not supported: '{key}': {issues}");
            }
        }

        /// <summary>
        /// The names the objects are stored under, then the unsupported keys saved in this run whose object exists
        /// </summary>
        private ICollection<string> ListKeys()
        {
            var keys = _store.Keys;
            if (_sanitizedKeys.IsEmpty)
            {
                return keys;
            }
            var unsupportedKeys = _sanitizedKeys.Where(pair => _store.ContainsKey(pair.Value)).Select(pair => pair.Key).ToList();
            return unsupportedKeys.Count == 0 ? keys : keys.Union(unsupportedKeys).ToList();
        }

        /// <summary>
        /// The key a save goes to: the given one when it follows the key format, else its sanitized version, replacing
        /// the object stored there. Null and empty keys are left for the store to reject
        /// </summary>
        private string KeyToSave(string path)
        {
            if (string.IsNullOrEmpty(path) || IsSupportedKey(path))
            {
                return path;
            }

            var key = SanitizeKey(path);
            _sanitizedKeys[path] = key;
            WarnOnce(path, key);
            return key;
        }

        /// <summary>
        /// The key the other operations use: the given one when it follows the key format, else its sanitized version,
        /// so an object is found by the key it was saved with. An object stored under the name as given, before the
        /// key format applied, is used while the sanitized one does not exist
        /// </summary>
        private string KeyToRead(string path)
        {
            if (string.IsNullOrEmpty(path) || IsSupportedKey(path))
            {
                return path;
            }

            var key = SanitizeKey(path);
            if (!_store.ContainsKey(key) && _store.ContainsKey(path))
            {
                return path;
            }
            WarnOnce(path, key);
            return key;
        }

        private void WarnOnce(string path, string key)
        {
            if (Interlocked.Exchange(ref _warned, 1) == 0)
            {
                _warn($"ObjectStore: key '{path}' is not supported: {GetKeyIssues(path)}. Using '{key}' instead." +
                    " Any other unsupported key is sanitized the same way");
            }
        }

        private static bool IsSupportedKey(string key)
        {
            return !string.IsNullOrEmpty(key) && !UnsupportedKeyCharactersRegex.IsMatch(key) && !key.Contains("..", StringComparison.Ordinal) && HasName(key)
                // just in case
                && key.Count(c => c == '/') <= 100 && key.Count(c => c == '\\') <= 100;
        }

        /// <summary>
        /// Whether the key has something besides dots and separators, so it names an object and not a folder
        /// </summary>
        private static bool HasName(string key)
        {
            return key.Any(c => c != '.' && c != '/' && c != '\\');
        }

        /// <summary>
        /// Describes why a key does not follow the key format: the problems found and the rules. Null for a supported key
        /// </summary>
        private static string GetKeyIssues(string key)
        {
            if (IsSupportedKey(key))
            {
                return null;
            }
            return string.Join("; ", FindKeyIssues(key)) +
                ". Keys may only contain english letters, numbers, spaces and the characters" +
                " '.', '/', '\\', '_', '#', '-', '$' and '=', never two '.' in a row, e.g. 'folder/trade_log-2024.v2.csv'";
        }

        /// <summary>
        /// Lists why a key fails <see cref="IsSupportedKey"/>, one entry per problem found
        /// </summary>
        private static List<string> FindKeyIssues(string key)
        {
            var issues = new List<string>();
            if (string.IsNullOrEmpty(key))
            {
                issues.Add("the key is empty");
                return issues;
            }

            var unsupported = UnsupportedKeyCharactersRegex.Matches(key).Select(match => $"'{match.Value}'").Distinct().ToList();
            if (unsupported.Count > 0)
            {
                issues.Add($"unsupported character{(unsupported.Count > 1 ? "s" : "")} {string.Join(", ", unsupported)}");
            }

            if (key.Contains("..", StringComparison.Ordinal))
            {
                issues.Add("it contains '..'");
            }
            if (!HasName(key))
            {
                issues.Add("it has no name, only '.' and separators");
            }

            if (key.Count(c => c == '/') > 100 || key.Count(c => c == '\\') > 100)
            {
                issues.Add("it has more than 100 '/' or '\\' separators");
            }

            if (issues.Count == 0)
            {
                issues.Add("it does not follow the key format");
            }
            return issues;
        }

        /// <summary>
        /// Converts an unsupported key into a supported one by replacing every unsupported character, and every '.'
        /// of a '..' run, with '_': 'trade log: AI &amp; Cloud.csv' becomes 'trade log_ AI _ Cloud.csv'.
        /// The mapping is lossy, distinct keys can sanitize to the same one. Throws when the key cannot be fixed
        /// </summary>
        private static string SanitizeKey(string key)
        {
            var sanitized = UnsupportedKeyCharactersRegex.Replace(key, "_");
            sanitized = ConsecutiveDotsRegex.Replace(sanitized, match => new string('_', match.Length));

            ValidateKey(sanitized);
            return sanitized;
        }

        /// <summary>
        /// The keys of the object store, listed on first use. Its Contains finds a key the way
        /// <see cref="ContainsKey"/> does, without listing the store
        /// </summary>
        private sealed class KeyCollection : ICollection<string>
        {
            private readonly Lazy<ICollection<string>> _keys;
            private readonly Func<string, bool> _containsKey;

            public int Count => _keys.Value.Count;

            public bool IsReadOnly => _keys.Value.IsReadOnly;

            public KeyCollection(Func<ICollection<string>> listKeys, Func<string, bool> containsKey)
            {
                _keys = new Lazy<ICollection<string>>(listKeys);
                _containsKey = containsKey;
            }

            public bool Contains(string item)
            {
                return !string.IsNullOrEmpty(item) && _containsKey(item);
            }

            /// <summary>
            /// Python's 'in' operator calls this on a type that also has a Count
            /// </summary>
            public bool ContainsKey(string key)
            {
                return Contains(key);
            }

            public void Add(string item)
            {
                _keys.Value.Add(item);
            }

            public void Clear()
            {
                _keys.Value.Clear();
            }

            public bool Remove(string item)
            {
                return _keys.Value.Remove(item);
            }

            public void CopyTo(string[] array, int arrayIndex)
            {
                _keys.Value.CopyTo(array, arrayIndex);
            }

            public IEnumerator<string> GetEnumerator()
            {
                return _keys.Value.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }
    }
}

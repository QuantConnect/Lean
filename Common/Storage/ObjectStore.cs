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
    /// format (english letters, numbers, spaces and the characters '/', '\', '_', '#', '-', '$' and '=', plus at
    /// most one '.' followed by a letters-and-numbers-only extension, e.g. 'folder/trade_log-2024.csv') is saved
    /// under its sanitized version and found by either name, with a warning the first time it happens. A save whose
    /// sanitized name already holds an object this store did not write from that key throws, since nothing records who
    /// wrote that object. An object already stored under an unsupported name is read as is; a save to that name goes
    /// to the sanitized name, warning every time
    /// </summary>
    public class ObjectStore : IObjectStore
    {
        // The single source of the key charset/extension rules, also enforced by the LocalObjectStore implementation
        private static readonly Regex SupportedKeyRegex = new(@"^\.?[a-zA-Z0-9\\/_#\-\$= ]+\.?[a-zA-Z0-9]*$", RegexOptions.Compiled);
        private static readonly Regex UnsupportedKeyCharactersRegex = new(@"[^a-zA-Z0-9\\/_#\-\$= .]", RegexOptions.Compiled);
        private static readonly Regex KeyExtensionRegex = new(@"^[a-zA-Z0-9]+$", RegexOptions.Compiled);

        private readonly IObjectStore _store;
        private readonly Action<string> _warn;
        private int _warned;
        // the unsupported keys this store last wrote each sanitized name from: their saves are updates, and reads follow them
        private readonly ConcurrentDictionary<string, string> _savedKeys = new();

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
        /// Returns the file paths present in the object store. This is specially useful not to load the object store into memory
        /// </summary>
        public ICollection<string> Keys => _store.Keys;

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
        /// <param name="path">The object key: english letters, numbers, spaces, '/', '\', '_', '#', '-', '$', '=' and at most one '.extension', e.g. 'folder/trade_log-2024.csv'. Other characters are replaced by '_'</param>
        /// <param name="contents">The object data</param>
        /// <returns>True if the save operation was successful</returns>
        public bool SaveBytes(string path, byte[] contents)
        {
            var key = KeyToSave(path);
            if (!_store.SaveBytes(key, contents))
            {
                return false;
            }
            ForgetSavesTo(key, exceptFrom: path);
            if (key != path)
            {
                _savedKeys[path] = key;
            }
            return true;
        }

        /// <summary>
        /// Deletes the object data for the specified path
        /// </summary>
        /// <param name="path">The object path</param>
        /// <returns>True if the delete operation was successful</returns>
        public bool Delete(string path)
        {
            var key = KeyToRead(path);
            if (!_store.Delete(key))
            {
                return false;
            }
            ForgetSavesTo(key);
            return true;
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
        /// <param name="path">The object key: english letters, numbers, spaces, '/', '\', '_', '#', '-', '$', '=' and at most one '.extension', e.g. 'folder/trade_log-2024.csv'. Other characters are replaced by '_'</param>
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
        /// <param name="path">The object key: english letters, numbers, spaces, '/', '\', '_', '#', '-', '$', '=' and at most one '.extension', e.g. 'folder/trade_log-2024.csv'. Other characters are replaced by '_'</param>
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
        /// <param name="path">The object key: english letters, numbers, spaces, '/', '\', '_', '#', '-', '$', '=' and at most one '.extension', e.g. 'folder/trade_log-2024.csv'. Other characters are replaced by '_'</param>
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
        /// <param name="path">The object path</param>
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
        /// <param name="path">The object key: english letters, numbers, spaces, '/', '\', '_', '#', '-', '$', '=' and at most one '.extension', e.g. 'folder/trade_log-2024.csv'. Other characters are replaced by '_'</param>
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
        /// <param name="path">The object key: english letters, numbers, spaces, '/', '\', '_', '#', '-', '$', '=' and at most one '.extension', e.g. 'folder/trade_log-2024.csv'. Other characters are replaced by '_'</param>
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
        /// The key a save goes to: the given one when it follows the key format, else its sanitized version. Throws when
        /// that already holds an object this store did not last write from the given key. Null and empty keys are left for the store to reject
        /// </summary>
        private string KeyToSave(string path)
        {
            if (string.IsNullOrEmpty(path) || IsSupportedKey(path))
            {
                return path;
            }

            var key = SanitizeKey(path);
            WarnOnce(path, key);
            if (!_savedKeys.ContainsKey(path) && _store.ContainsKey(key))
            {
                // nothing records who wrote that object, a previous run, another algorithm or an upload: it is never replaced through another key
                throw new ArgumentException($"ObjectStore: key '{path}' is not supported and its sanitized name '{key}' already holds" +
                    $" another object. Delete '{key}' or save to it directly to replace it");
            }
            if (_store.ContainsKey(path))
            {
                // the object under the name as given stays: reads of it from another store still return it
                _warn($"ObjectStore: key '{path}' is not supported: the object stored under that name is left as is, this save goes to '{key}'");
            }
            return key;
        }

        /// <summary>
        /// Forgets the unsupported keys whose object under the given key is gone or replaced from another key
        /// </summary>
        private void ForgetSavesTo(string key, string exceptFrom = null)
        {
            foreach (var entry in _savedKeys)
            {
                if (entry.Value == key && entry.Key != exceptFrom)
                {
                    _savedKeys.TryRemove(entry.Key, out _);
                }
            }
        }

        /// <summary>
        /// The key the other operations use: where this store saved it while that object exists, else the given one when
        /// it follows the key format or the store already has it, else its sanitized version, so an object is found by the key it was saved with
        /// </summary>
        private string KeyToRead(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return path;
            }
            if (_savedKeys.TryGetValue(path, out var key) && _store.ContainsKey(key))
            {
                return key;
            }
            if (IsSupportedKey(path) || _store.ContainsKey(path))
            {
                return path;
            }

            key = SanitizeKey(path);
            WarnOnce(path, key);
            return key;
        }

        private void WarnOnce(string path, string key)
        {
            if (Interlocked.Exchange(ref _warned, 1) == 0)
            {
                _warn($"ObjectStore: key '{path}' is not supported: {GetKeyIssues(path)}. Using '{key}' instead. Any other unsupported" +
                    " key is sanitized the same way; a save throws when its sanitized name already holds an object not written from that key in this run");
            }
        }

        private static bool IsSupportedKey(string key)
        {
            return !string.IsNullOrEmpty(key) && SupportedKeyRegex.IsMatch(key)
                // just in case
                && key.Count(c => c == '/') <= 100 && key.Count(c => c == '\\') <= 100;
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
                " '/', '\\', '_', '#', '-', '$' and '=', plus at most one '.' followed by a letters-and-numbers-only extension," +
                " e.g. 'folder/trade_log-2024.csv'";
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

            // a single leading '.' is allowed, every other '.' must introduce the final extension
            var name = key.TrimStart('.');
            var dots = name.Count(c => c == '.');
            if (dots > 1)
            {
                issues.Add($"it has {dots} '.' but at most one '.extension' is allowed");
            }
            else if (dots == 1)
            {
                var extension = name[(name.IndexOf('.', StringComparison.Ordinal) + 1)..];
                if (extension.Length > 0 && !KeyExtensionRegex.IsMatch(extension))
                {
                    issues.Add($"the extension '{extension}' may only contain letters and numbers");
                }
            }
            if (name.Length == 0)
            {
                issues.Add("it has no name before the '.'");
            }
            else if (key.StartsWith("..", StringComparison.Ordinal))
            {
                issues.Add("it starts with more than one '.'");
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
        /// Converts an unsupported key into a supported one by replacing every unsupported character with '_',
        /// keeping at most the final '.extension': 'trade log: AI &amp; Cloud.csv' becomes 'trade log_ AI _ Cloud.csv'.
        /// The mapping is lossy, distinct keys can sanitize to the same one. Throws when the key cannot be fixed
        /// </summary>
        private static string SanitizeKey(string key)
        {
            var sanitized = UnsupportedKeyCharactersRegex.Replace(key, "_");

            // a single trailing '.extension' of letters and numbers is allowed: keep the last dot if it
            // introduces a valid extension and replace every other dot
            var lastDot = sanitized.LastIndexOf('.');
            if (lastDot > 0 && KeyExtensionRegex.IsMatch(sanitized.AsSpan(lastDot + 1)))
            {
                sanitized = string.Concat(sanitized[..lastDot].Replace('.', '_'), sanitized.AsSpan(lastDot));
            }
            else if (lastDot >= 0)
            {
                sanitized = sanitized.Replace('.', '_');
            }

            ValidateKey(sanitized);
            return sanitized;
        }
    }
}

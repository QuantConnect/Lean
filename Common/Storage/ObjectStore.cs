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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using Newtonsoft.Json;
using Python.Runtime;
using QuantConnect.Interfaces;
using QuantConnect.Packets;

namespace QuantConnect.Storage
{
    /// <summary>
    /// Helper class for easier access to <see cref="IObjectStore"/> methods
    /// </summary>
    public class ObjectStore : IObjectStore
    {
        // The single source of the key charset/extension rules, also enforced by the LocalObjectStore implementation
        private static readonly Regex SupportedKeyRegex = new(@"^\.?[a-zA-Z0-9\\/_#\-\$= ]+\.?[a-zA-Z0-9]*$", RegexOptions.Compiled);
        private static readonly Regex UnsupportedKeyCharactersRegex = new(@"[^a-zA-Z0-9\\/_#\-\$= .]", RegexOptions.Compiled);
        private static readonly Regex KeyExtensionRegex = new(@"^[a-zA-Z0-9]+$", RegexOptions.Compiled);

        // Python helpers for the PyObject APIs, lazily created under the GIL
        private static PyObject _jsonSerializeMethod;
        private static PyObject _jsonDeserializeMethod;
        private static PyObject _dataFrameSerializeMethod;

        /// <summary>
        /// Human readable description of the format an object store key must follow, stated in key validation errors
        /// </summary>
        public static string SupportedKeyRules { get; } = "keys may only contain english letters, numbers, spaces and the characters" +
            " '/', '\\', '_', '#', '-', '$' and '=', plus at most one '.' followed by a letters-and-numbers-only extension," +
            " e.g. 'folder/trade_log-2024.csv'";

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

        private readonly IObjectStore _store;

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectStore"/> class
        /// </summary>
        /// <param name="store">The <see cref="IObjectStore"/> instance to wrap</param>
        public ObjectStore(IObjectStore store)
        {
            _store = store;
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
            return _store.ContainsKey(path);
        }

        /// <summary>
        /// Returns the object data for the specified path
        /// </summary>
        /// <param name="path">The object path</param>
        /// <returns>A byte array containing the data</returns>
        public byte[] ReadBytes(string path)
        {
            return _store.ReadBytes(path);
        }

        /// <summary>
        /// Saves the object data for the specified path
        /// </summary>
        /// <param name="path">The object path</param>
        /// <param name="contents">The object data</param>
        /// <returns>True if the save operation was successful</returns>
        public bool SaveBytes(string path, byte[] contents)
        {
            return _store.SaveBytes(path, contents);
        }

        /// <summary>
        /// Deletes the object data for the specified path
        /// </summary>
        /// <param name="path">The object path</param>
        /// <returns>True if the delete operation was successful</returns>
        public bool Delete(string path)
        {
            return _store.Delete(path);
        }

        /// <summary>
        /// Returns the file path for the specified path
        /// </summary>
        /// <param name="path">The object path</param>
        /// <returns>The path for the file</returns>
        public string GetFilePath(string path)
        {
            return _store.GetFilePath(path);
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

            var data = _store.ReadBytes(path);
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
        /// Determines whether the given key follows the object store key format, see <see cref="SupportedKeyRules"/>
        /// </summary>
        /// <param name="key">The object store key to validate</param>
        /// <returns>True if the key is supported</returns>
        public static bool IsSupportedKey(string key)
        {
            return !string.IsNullOrEmpty(key) && SupportedKeyRegex.IsMatch(key)
                // just in case
                && key.Count(c => c == '/') <= 100 && key.Count(c => c == '\\') <= 100;
        }

        /// <summary>
        /// Converts an arbitrary name into a supported object store key by replacing every unsupported character
        /// with '_', keeping at most the final '.extension'. Useful for programmatically-built keys, for instance
        /// from user-facing names: 'trade log: AI &amp; Cloud.csv' becomes 'trade log_ AI _ Cloud.csv'
        /// </summary>
        /// <param name="key">The arbitrary name to sanitize</param>
        /// <returns>A key following <see cref="SupportedKeyRules"/></returns>
        public static string SanitizeKey(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("ObjectStore.SanitizeKey(): key cannot be null or empty", nameof(key));
            }
            if (IsSupportedKey(key))
            {
                return key;
            }

            var sanitized = UnsupportedKeyCharactersRegex.Replace(key, "_");

            // a single trailing '.extension' of letters and numbers is allowed: keep the last dot if it
            // introduces a valid extension and replace every other dot
            var lastDot = sanitized.LastIndexOf('.');
            if (lastDot > 0 && KeyExtensionRegex.IsMatch(sanitized.Substring(lastDot + 1)))
            {
                sanitized = sanitized.Substring(0, lastDot).Replace('.', '_') + sanitized.Substring(lastDot);
            }
            else if (lastDot >= 0)
            {
                sanitized = sanitized.Replace('.', '_');
            }

            if (!IsSupportedKey(sanitized))
            {
                throw new ArgumentException($"ObjectStore.SanitizeKey(): unable to sanitize key '{key}': object store {SupportedKeyRules}");
            }
            return sanitized;
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
        /// Returns the JSON deserialized object data for the specified path as Python objects,
        /// or the given default value if the key is not present
        /// </summary>
        /// <param name="path">The object path</param>
        /// <param name="defaultValue">Value to return when the key is not present. Defaults to None</param>
        /// <returns>The deserialized Python object, or <paramref name="defaultValue"/> if the key is not present</returns>
        public PyObject ReadJson(string path, PyObject defaultValue = null)
        {
            if (!ContainsKey(path))
            {
                return defaultValue;
            }
            var json = Read(path);
            using (Py.GIL())
            {
                EnsurePythonHelpers();
                using var pyJson = json.ToPython();
                return _jsonDeserializeMethod.Invoke(pyJson);
            }
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
        /// <param name="path">The object path</param>
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

            return _store.SaveBytes(path, bytes);
        }

        /// <summary>
        /// Saves the object data in text format for the specified path
        /// </summary>
        /// <param name="path">The object path</param>
        /// <param name="text">The string object to be saved</param>
        /// <param name="encoding">The string encoding used, <see cref="Encoding.UTF8"/> by default</param>
        /// <returns>True if the object was saved successfully</returns>
        public bool Save(string path, string text, Encoding encoding = null)
        {
            encoding ??= Encoding.UTF8;
            return _store.SaveBytes(path, encoding.GetBytes(text));
        }

        /// <summary>
        /// Saves the object data in text format for the specified path
        /// </summary>
        /// <param name="path">The object path</param>
        /// <param name="text">The string object to be saved</param>
        /// <param name="encoding">The string encoding used</param>
        /// <returns>True if the object was saved successfully</returns>
        public bool SaveString(string path, string text, Encoding encoding = null)
        {
            encoding = encoding ?? Encoding.UTF8;

            return _store.SaveBytes(path, encoding.GetBytes(text));
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
        /// <param name="path">The object path</param>
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
        /// Saves the given Python object in JSON format for the specified path, tolerating types the standard
        /// json module rejects: datetime/date/time are stored in ISO-8601 format, Decimal and numpy scalars as
        /// numbers and any other unsupported type (e.g. Symbol) as its string representation. Non-string
        /// dictionary keys are stringified
        /// </summary>
        /// <param name="path">The object path</param>
        /// <param name="obj">The Python object to be saved</param>
        /// <returns>True if the object was saved successfully</returns>
        public bool SaveJson(string path, PyObject obj)
        {
            string json;
            using (Py.GIL())
            {
                EnsurePythonHelpers();
                using var result = _jsonSerializeMethod.Invoke(obj);
                json = result.As<string>();
            }
            return Save(path, json);
        }

        /// <summary>
        /// Saves the given pandas DataFrame (or Series) for the specified path, in JSON format if the
        /// path has a '.json' extension and as CSV otherwise
        /// </summary>
        /// <param name="path">The object path</param>
        /// <param name="dataFrame">The pandas DataFrame or Series to be saved</param>
        /// <returns>True if the object was saved successfully</returns>
        public bool SaveDataframe(string path, PyObject dataFrame)
        {
            string serialized;
            using (Py.GIL())
            {
                EnsurePythonHelpers();
                using var pyPath = (path ?? string.Empty).ToPython();
                using var result = _dataFrameSerializeMethod.Invoke(dataFrame, pyPath);
                serialized = result.As<string>();
            }
            return Save(path, serialized);
        }

        /// <summary>
        /// Saves the object data in XML format for the specified path
        /// </summary>
        /// <param name="path">The object path</param>
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
        /// Lazily creates the Python helper methods backing the PyObject APIs. Must be called under the GIL
        /// </summary>
        private static void EnsurePythonHelpers()
        {
            if (_jsonSerializeMethod == null)
            {
                var module = PyModule.FromString("object_store_helpers", @"from json import dumps, loads
from datetime import datetime, date, time
from decimal import Decimal
try:
    import numpy
except ImportError:
    numpy = None

def _default(value):
    if isinstance(value, (datetime, date, time)):
        return value.isoformat()
    if isinstance(value, Decimal):
        return float(value)
    if numpy is not None and isinstance(value, numpy.generic):
        return value.item()
    # Symbol and any other type json does not handle
    return str(value)

def _normalize(value):
    if isinstance(value, dict):
        # json requires str/int/float/bool/None keys: stringify anything else (Symbol, datetime, ...)
        return { (k if isinstance(k, (str, int, float, bool)) or k is None else str(_default(k))): _normalize(v)
            for k, v in value.items() }
    if isinstance(value, (list, tuple, set)):
        return [_normalize(v) for v in value]
    return value

def serialize(value):
    return dumps(_normalize(value), default=_default)

def deserialize(json_string):
    return loads(json_string)

def serialize_dataframe(value, key):
    if not hasattr(value, 'to_csv'):
        raise TypeError(f'save_dataframe() expects a pandas DataFrame or Series but received {type(value).__name__}')
    if key.lower().endswith('.json'):
        return value.to_json()
    return value.to_csv()
");
                _jsonDeserializeMethod = module.GetAttr("deserialize");
                _dataFrameSerializeMethod = module.GetAttr("serialize_dataframe");
                // last so partial initialization is never observed
                _jsonSerializeMethod = module.GetAttr("serialize");
            }
        }
    }
}
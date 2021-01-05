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
using System.Text;
using System.Xml.Serialization;
using Newtonsoft.Json;
using QuantConnect.Interfaces;
using QuantConnect.Packets;

namespace QuantConnect.Storage
{
    /// <summary>
    /// Helper class for easier access to <see cref="IObjectStore"/> methods
    /// </summary>
    public class ObjectStore : IObjectStore
    {
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
        /// <param name="algorithmName">The algorithm name</param>
        /// <param name="userId">The user id</param>
        /// <param name="projectId">The project id</param>
        /// <param name="userToken">The user token</param>
        /// <param name="controls">The job controls instance</param>
        public void Initialize(string algorithmName, int userId, int projectId, string userToken, Controls controls)
        {
            _store.Initialize(algorithmName, userId, projectId, userToken, controls);
        }

        /// <summary>
        /// Determines whether the store contains data for the specified key
        /// </summary>
        /// <param name="key">The object key</param>
        /// <returns>True if the key was found</returns>
        public bool ContainsKey(string key)
        {
            return _store.ContainsKey(key);
        }

        /// <summary>
        /// Returns the object data for the specified key
        /// </summary>
        /// <param name="key">The object key</param>
        /// <returns>A byte array containing the data</returns>
        public byte[] ReadBytes(string key)
        {
            return _store.ReadBytes(key);
        }

        /// <summary>
        /// Saves the object data for the specified key
        /// </summary>
        /// <param name="key">The object key</param>
        /// <param name="contents">The object data</param>
        /// <returns>True if the save operation was successful</returns>
        public bool SaveBytes(string key, byte[] contents)
        {
            return _store.SaveBytes(key, contents);
        }

        /// <summary>
        /// Deletes the object data for the specified key
        /// </summary>
        /// <param name="key">The object key</param>
        /// <returns>True if the delete operation was successful</returns>
        public bool Delete(string key)
        {
            return _store.Delete(key);
        }

        /// <summary>
        /// Returns the file path for the specified key
        /// </summary>
        /// <param name="key">The object key</param>
        /// <returns>The path for the file</returns>
        public string GetFilePath(string key)
        {
            return _store.GetFilePath(key);
        }

        /// <summary>
        /// Returns the string object data for the specified key
        /// </summary>
        /// <param name="key">The object key</param>
        /// <param name="encoding">The string encoding used</param>
        /// <returns>A string containing the data</returns>
        public string Read(string key, Encoding encoding = null)
        {
            encoding = encoding ?? Encoding.UTF8;

            return encoding.GetString(_store.ReadBytes(key));
        }

        /// <summary>
        /// Returns the string object data for the specified key
        /// </summary>
        /// <param name="key">The object key</param>
        /// <param name="encoding">The string encoding used</param>
        /// <returns>A string containing the data</returns>
        public string ReadString(string key, Encoding encoding = null)
        {
            return Read(key, encoding);
        }

        /// <summary>
        /// Returns the JSON deserialized object data for the specified key
        /// </summary>
        /// <param name="key">The object key</param>
        /// <param name="encoding">The string encoding used</param>
        /// <param name="settings">The settings used by the JSON deserializer</param>
        /// <returns>An object containing the data</returns>
        public T ReadJson<T>(string key, Encoding encoding = null, JsonSerializerSettings settings = null)
        {
            encoding = encoding ?? Encoding.UTF8;

            var json = Read(key, encoding);
            return JsonConvert.DeserializeObject<T>(json, settings);
        }

        /// <summary>
        /// Returns the XML deserialized object data for the specified key
        /// </summary>
        /// <param name="key">The object key</param>
        /// <param name="encoding">The string encoding used</param>
        /// <returns>An object containing the data</returns>
        public T ReadXml<T>(string key, Encoding encoding = null)
        {
            encoding = encoding ?? Encoding.UTF8;

            var xml = Read(key, encoding);

            var serializer = new XmlSerializer(typeof(T));
            using (var reader = new StringReader(xml))
            {
                return (T)serializer.Deserialize(reader);
            }
        }

        /// <summary>
        /// Saves the object data in text format for the specified key
        /// </summary>
        /// <param name="key">The object key</param>
        /// <param name="text">The string object to be saved</param>
        /// <param name="encoding">The string encoding used</param>
        /// <returns>True if the object was saved successfully</returns>
        public bool Save(string key, string text, Encoding encoding = null)
        {
            encoding = encoding ?? Encoding.UTF8;

            return _store.SaveBytes(key, encoding.GetBytes(text));
        }

        /// <summary>
        /// Saves the object data in text format for the specified key
        /// </summary>
        /// <param name="key">The object key</param>
        /// <param name="text">The string object to be saved</param>
        /// <param name="encoding">The string encoding used</param>
        /// <returns>True if the object was saved successfully</returns>
        public bool SaveString(string key, string text, Encoding encoding = null)
        {
            encoding = encoding ?? Encoding.UTF8;

            return _store.SaveBytes(key, encoding.GetBytes(text));
        }

        /// <summary>
        /// Saves the object data in JSON format for the specified key
        /// </summary>
        /// <param name="key">The object key</param>
        /// <param name="obj">The object to be saved</param>
        /// <param name="encoding">The string encoding used</param>
        /// <param name="settings">The settings used by the JSON serializer</param>
        /// <returns>True if the object was saved successfully</returns>
        public bool SaveJson<T>(string key, T obj, Encoding encoding = null, JsonSerializerSettings settings = null)
        {
            encoding = encoding ?? Encoding.UTF8;

            var json = JsonConvert.SerializeObject(obj, settings);
            return SaveString(key, json, encoding);
        }

        /// <summary>
        /// Saves the object data in XML format for the specified key
        /// </summary>
        /// <param name="key">The object key</param>
        /// <param name="obj">The object to be saved</param>
        /// <param name="encoding">The string encoding used</param>
        /// <returns>True if the object was saved successfully</returns>
        public bool SaveXml<T>(string key, T obj, Encoding encoding = null)
        {
            encoding = encoding ?? Encoding.UTF8;

            using (var writer = new StringWriter())
            {
                var serializer = new XmlSerializer(typeof(T));
                serializer.Serialize(writer, obj);

                var xml = writer.ToString();
                return SaveString(key, xml, encoding);
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
            return ((IEnumerable) _store).GetEnumerator();
        }

        /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
        /// <filterpriority>2</filterpriority>
        public void Dispose()
        {
            _store.Dispose();
        }
    }
}

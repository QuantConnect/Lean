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
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Text;
using Newtonsoft.Json;
using QuantConnect.Packets;

namespace QuantConnect.Interfaces
{
    /// <summary>
    /// Provides object storage for data persistence.
    /// </summary>
    [InheritedExport(typeof(IObjectStore))]
    public interface IObjectStore : IDisposable, IEnumerable<KeyValuePair<string, byte[]>>
    {
        /// <summary>
        /// Event raised each time there's an error
        /// </summary>
        event EventHandler<ObjectStoreErrorRaisedEventArgs> ErrorRaised;

        /// <summary>
        /// Initializes the object store
        /// </summary>
        /// <param name="algorithmName">The algorithm name</param>
        /// <param name="userId">The user id</param>
        /// <param name="projectId">The project id</param>
        /// <param name="userToken">The user token</param>
        /// <param name="controls">The job controls instance</param>
        void Initialize(string algorithmName, int userId, int projectId, string userToken, Controls controls);

        /// <summary>
        /// Determines whether the store contains data for the specified key
        /// </summary>
        /// <param name="key">The object key</param>
        /// <returns>True if the key was found</returns>
        bool ContainsKey(string key);

        /// <summary>
        /// Returns the object data for the specified key
        /// </summary>
        /// <param name="key">The object key</param>
        /// <returns>A byte array containing the data</returns>
        byte[] ReadBytes(string key);

        /// <summary>
        /// Returns the string object data for the specified key
        /// </summary>
        /// <param name="key">The object key</param>
        /// <param name="encoding">The string encoding used</param>
        /// <returns>A string containing the data</returns>
        string Read(string key, Encoding encoding = null);

        /// <summary>
        /// Returns the string object data for the specified key
        /// </summary>
        /// <param name="key">The object key</param>
        /// <param name="encoding">The string encoding used</param>
        /// <returns>A string containing the data</returns>
        string ReadString(string key, Encoding encoding = null);

        /// <summary>
        /// Returns the JSON deserialized object data for the specified key
        /// </summary>
        /// <param name="key">The object key</param>
        /// <param name="encoding">The string encoding used</param>
        /// <param name="settings">The settings used by the JSON deserializer</param>
        /// <returns>An object containing the data</returns>
        T ReadJson<T>(string key, Encoding encoding = null, JsonSerializerSettings settings = null);

        /// <summary>
        /// Returns the XML deserialized object data for the specified key
        /// </summary>
        /// <param name="key">The object key</param>
        /// <param name="encoding">The string encoding used</param>
        /// <returns>An object containing the data</returns>
        T ReadXml<T>(string key, Encoding encoding = null);

        /// <summary>
        /// Saves the object data for the specified key
        /// </summary>
        /// <param name="key">The object key</param>
        /// <param name="contents">The object data</param>
        /// <returns>True if the save operation was successful</returns>
        bool SaveBytes(string key, byte[] contents);

        /// <summary>
        /// Saves the object data in text format for the specified key
        /// </summary>
        /// <param name="key">The object key</param>
        /// <param name="text">The string object to be saved</param>
        /// <param name="encoding">The string encoding used</param>
        /// <returns>True if the object was saved successfully</returns>
        bool Save(string key, string text, Encoding encoding = null);

        /// <summary>
        /// Saves the object data in text format for the specified key
        /// </summary>
        /// <param name="key">The object key</param>
        /// <param name="text">The string object to be saved</param>
        /// <param name="encoding">The string encoding used</param>
        /// <returns>True if the object was saved successfully</returns>
        bool SaveString(string key, string text, Encoding encoding = null);

        /// <summary>
        /// Saves the object data in JSON format for the specified key
        /// </summary>
        /// <param name="key">The object key</param>
        /// <param name="obj">The object to be saved</param>
        /// <param name="encoding">The string encoding used</param>
        /// <param name="settings">The settings used by the JSON serializer</param>
        /// <returns>True if the object was saved successfully</returns>
        bool SaveJson<T>(string key, T obj, Encoding encoding = null, JsonSerializerSettings settings = null);

        /// <summary>
        /// Saves the object data in XML format for the specified key
        /// </summary>
        /// <param name="key">The object key</param>
        /// <param name="obj">The object to be saved</param>
        /// <param name="encoding">The string encoding used</param>
        /// <returns>True if the object was saved successfully</returns>
        bool SaveXml<T>(string key, T obj, Encoding encoding = null);

        /// <summary>
        /// Deletes the object data for the specified key
        /// </summary>
        /// <param name="key">The object key</param>
        /// <returns>True if the delete operation was successful</returns>
        bool Delete(string key);

        /// <summary>
        /// Returns the file path for the specified key
        /// </summary>
        /// <param name="key">The object key</param>
        /// <returns>The path for the file</returns>
        string GetFilePath(string key);
    }
}
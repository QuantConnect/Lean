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
        /// Saves the object data for the specified key
        /// </summary>
        /// <param name="key">The object key</param>
        /// <param name="contents">The object data</param>
        /// <returns>True if the save operation was successful</returns>
        bool SaveBytes(string key, byte[] contents);

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

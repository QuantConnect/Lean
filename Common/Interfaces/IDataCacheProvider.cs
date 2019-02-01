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
 *
*/

using System;
using System.Collections.Generic;
using System.IO;
using QuantConnect.Data;

namespace QuantConnect.Interfaces
{
    /// <summary>
    /// Defines a cache for data
    /// </summary>
    public interface IDataCacheProvider : IDisposable
    {
        /// <summary>
        /// Fetch data from the cache as stream
        /// </summary>
        /// <param name="key">A string representing the key of the cached data</param>
        /// <returns>An <see cref="Stream"/> of the cached data</returns>
        Stream FetchStream(string key);

        /// <summary>
        /// Fetch data from the cache as enumerator
        /// </summary>
        /// <param name="key">A string representing the key of the cached data</param>
        /// <param name="config">The subscription config</param>
        /// <param name="startDate">Provide the start date of data to be fetched. Inclusive.</param>
        /// <param name="endDate">Provide the end date of data to be fetched. Inclusive.</param>
        /// <returns>An enumerator of the cached data</returns>
        IEnumerator<string> FetchEnumerator(
            string key,
            SubscriptionDataConfig config,
            DateTime? startDate = null,
            DateTime? endDate = null);

        /// <summary>
        /// Store the data in the cache
        /// </summary>
        /// <param name="key">The source of the data, used as a key to retrieve data in the cache</param>
        /// <param name="data">The data to cache as a byte array</param>
        void Store(string key, byte[] data);
    }
}
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
        /// Fetch data from the cache
        /// </summary>
        /// <param name="source">The <see cref="SubscriptionDataSource"/> of the requested data</param>
        /// <param name="date"></param>
        /// <returns>An <see cref="IStreamReader"/> that has the data from the cache preloaded</returns>
        Stream Fetch(string source, string entryName);

        /// <summary>
        /// Store the data in the cache
        /// </summary>
        /// <param name="source">The source of the data, used as a key to retrieve data in the cache</param>
        /// <param name="data">The data as a byte array</param>
        void Store(string source, byte[] data);
    }
}
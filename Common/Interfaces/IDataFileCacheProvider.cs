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

using QuantConnect.Data;
using System;
using System.ComponentModel.Composition;

namespace QuantConnect.Interfaces
{
    /// <summary>
    /// Fetches and caches a file for a security 
    /// </summary>
    public interface IDataFileCacheProvider : IDisposable
    {
        /// <summary>
        /// Gets and caches the file
        /// </summary>
        /// <param name="symbol"><see cref="Symbol"/> of the security</param>
        /// <param name="source"><see cref="SubscriptionDataSource"/> of the security</param>
        /// <param name="resolution"><see cref="Resolution"/> of the data requested</param>
        /// <param name="date">DateTime of the data requested</param>
        /// <param name="tickType"><see cref="TickType"/> of the security</param>
        /// <returns><see cref="IStreamReader"/> or null if the file was not fetched correctly</returns>
        IStreamReader Fetch(Symbol symbol, SubscriptionDataSource source, DateTime date, Resolution resolution, TickType tickType);
    }
}

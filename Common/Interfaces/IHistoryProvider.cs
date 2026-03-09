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

using System.Collections.Generic;
using System.ComponentModel.Composition;
using NodaTime;
using QuantConnect.Data;
using HistoryRequest = QuantConnect.Data.HistoryRequest;

namespace QuantConnect.Interfaces
{
    /// <summary>
    /// Provides historical data to an algorithm at runtime
    /// </summary>
    [InheritedExport(typeof(IHistoryProvider))]
    public interface IHistoryProvider : IDataProviderEvents
    {
        /// <summary>
        /// Gets the total number of data points emitted by this history provider
        /// </summary>
        int DataPointCount { get; }

        /// <summary>
        /// Initializes this history provider to work for the specified job
        /// </summary>
        /// <param name="parameters">The initialization parameters</param>
        void Initialize(HistoryProviderInitializeParameters parameters);

        /// <summary>
        /// Gets the history for the requested securities
        /// </summary>
        /// <param name="requests">The historical data requests</param>
        /// <param name="sliceTimeZone">The time zone used when time stamping the slice instances</param>
        /// <returns>An enumerable of the slices of data covering the span specified in each request</returns>
        IEnumerable<Slice> GetHistory(IEnumerable<HistoryRequest> requests, DateTimeZone sliceTimeZone);
    }
}

/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2026 QuantConnect Corporation.
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
using NodaTime;
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Util;
using QuantConnect.Interfaces;
using System.Collections.Generic;
using QuantConnect.Lean.Engine.DataFeeds;

namespace QuantConnect.Lean.Engine.HistoricalData
{
    /// <summary>
    /// Base class for history providers that resolve symbol mappings
    /// and synchronize multiple data streams into time-aligned slices.
    /// </summary>
    public abstract class MappedSynchronizingHistoryProvider : SynchronizingHistoryProvider
    {
        /// <summary>
        /// Resolves map files to correctly handle current and historical ticker symbols.
        /// </summary>
        private static readonly Lazy<IMapFileProvider> _mapFileProvider = new(Composer.Instance.GetPart<IMapFileProvider>);

        /// <summary>
        /// Gets historical data for a single resolved history request.
        /// Implementations should assume the symbol is already correctly mapped.
        /// </summary>
        /// <param name="request">The resolved history request.</param>
        /// <returns>The historical data.</returns>
        public abstract IEnumerable<BaseData>? GetHistory(HistoryRequest request);

        /// <summary>
        /// Gets the history for the requested securities
        /// </summary>
        /// <param name="requests">The historical data requests</param>
        /// <param name="sliceTimeZone">The time zone used when time stamping the slice instances</param>
        /// <returns>An enumerable of the slices of data covering the span specified in each request</returns>
        public override IEnumerable<Slice>? GetHistory(IEnumerable<HistoryRequest> requests, DateTimeZone sliceTimeZone)
        {
            var subscriptions = new List<Subscription>();
            foreach (var request in requests)
            {
                var history = request
                    .SplitHistoryRequestWithUpdatedMappedSymbol(_mapFileProvider.Value)
                    .SelectMany(x => GetHistory(x) ?? []);
                var subscription = CreateSubscription(request, history);
                if (!subscription.MoveNext())
                {
                    continue;
                }

                subscriptions.Add(subscription);
            }

            if (subscriptions.Count == 0)
            {
                return null;
            }

            // Ownership of subscription is transferred to CreateSliceEnumerableFromSubscriptions
            return CreateSliceEnumerableFromSubscriptions(subscriptions, sliceTimeZone);
        }
    }
}

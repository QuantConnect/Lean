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

using NodaTime;
using QuantConnect.Configuration;
using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine.DataFeeds.Enumerators;
using QuantConnect.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using HistoryRequest = QuantConnect.Data.HistoryRequest;

namespace QuantConnect.Lean.Engine.HistoricalData
{
    /// <summary>
    /// Provides an implementation of <see cref="IHistoryProvider"/> which
    /// acts as a wrapper to use multiple history providers together
    /// </summary>
    public class HistoryProviderManager : SynchronizingHistoryProvider
    {
        private IBrokerage _brokerage;
        private bool _initialized;

        /// <summary>
        /// Collection of history providers being used
        /// </summary>
        /// <remarks>Protected for testing purposes</remarks>
        protected List<IHistoryProvider> HistoryProviders { get; } = new();

        /// <summary>
        /// Sets the brokerage to be used for historical requests
        /// </summary>
        /// <param name="brokerage">The brokerage instance</param>
        public void SetBrokerage(IBrokerage brokerage)
        {
            _brokerage = brokerage;
        }

        /// <summary>
        /// Initializes this history provider to work for the specified job
        /// </summary>
        /// <param name="parameters">The initialization parameters</param>
        public override void Initialize(HistoryProviderInitializeParameters parameters)
        {
            if (_initialized)
            {
                // let's make sure no one tries to change our parameters values
                throw new InvalidOperationException("BrokerageHistoryProvider can only be initialized once");
            }
            _initialized = true;

            var dataProvidersList = parameters.Job.HistoryProvider.DeserializeList();
            if (dataProvidersList.IsNullOrEmpty())
            {
                dataProvidersList.Add(Config.Get("history-provider", "SubscriptionDataReaderHistoryProvider"));
            }

            foreach (var historyProviderName in dataProvidersList)
            {
                var historyProvider = Composer.Instance.GetExportedValueByTypeName<IHistoryProvider>(historyProviderName);
                if (historyProvider is BrokerageHistoryProvider)
                {
                    (historyProvider as BrokerageHistoryProvider).SetBrokerage(_brokerage);
                }
                historyProvider.Initialize(parameters);
                HistoryProviders.Add(historyProvider);
            }
        }

        /// <summary>
        /// Gets the history for the requested securities
        /// </summary>
        /// <param name="requests">The historical data requests</param>
        /// <param name="sliceTimeZone">The time zone used when time stamping the slice instances</param>
        /// <returns>An enumerable of the slices of data covering the span specified in each request</returns>
        public override IEnumerable<Slice> GetHistory(IEnumerable<HistoryRequest> requests, DateTimeZone sliceTimeZone)
        {
            List<IEnumerator<Slice>> historyEnumerators = new(HistoryProviders.Count);
            var historyRequets = requests.ToList();
            foreach (var historyProvider in HistoryProviders)
            {
                try
                {
                    var history = historyProvider.GetHistory(historyRequets, sliceTimeZone);
                    historyEnumerators.Add(history.GetEnumerator());
                }
                catch (Exception e)
                {
                    // ignore
                }
            }
            var synchronizer = new SynchronizingSliceEnumerator(historyEnumerators);
            var previousTime = DateTime.MinValue;
            Slice previousMergedSlice = null;
            Slice latestMergeSlice = null;
            bool isFirstComplete = new();
            while (synchronizer.MoveNext())
            {
                if (synchronizer.Current == null)
                {
                    continue;
                }
                if (synchronizer.Current.Time > previousTime)
                {
                    latestMergeSlice = synchronizer.Current;
                    previousTime = synchronizer.Current.Time;
                    if (!isFirstComplete)
                    {
                        isFirstComplete = true;
                        previousMergedSlice = latestMergeSlice;
                        continue;
                    }
                    yield return previousMergedSlice;
                }
                else
                {
                    latestMergeSlice.MergeSlice(synchronizer.Current);
                }
                previousMergedSlice = latestMergeSlice;
            }
            synchronizer.Dispose();
            if (!isFirstComplete)
            {
                yield break;
            }
            yield return previousMergedSlice;
        }
    }
}

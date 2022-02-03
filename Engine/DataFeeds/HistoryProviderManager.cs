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
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine.HistoricalData;
using QuantConnect.Util;
using System;
using System.Collections.Generic;
using HistoryRequest = QuantConnect.Data.HistoryRequest;

namespace QuantConnect.Lean.Engine.DataFeeds
{
    /// <summary>
    /// Provides an implementation of <see cref="IHistoryProvider"/> that relies on
    /// a brokerage connection to retrieve historical data
    /// </summary>
    public class HistoryProviderManager : SynchronizingHistoryProvider
    {
        /// <summary>
        /// Collection of history providers being used
        /// </summary>
        /// <remarks>Protected for testing purposes</remarks>
        protected List<IHistoryProvider> HistoryProviders { get; } = new();
        private IBrokerage _brokerage;
        private bool _initialized;

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
            SortedDictionary<DateTime, Slice> mergedHistory = new();
            foreach (var historyProvider in HistoryProviders)
            {
                try
                {
                    var history = historyProvider.GetHistory(requests, sliceTimeZone);
                    if (history != null)
                    {
                        foreach (var slice in history)
                        {
                            if (!mergedHistory.ContainsKey(slice.Time))
                            {
                                mergedHistory[slice.Time] = slice;
                            }
                            else
                            {
                                mergedHistory[slice.Time] = MergeSlice(slice, mergedHistory[slice.Time]);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    // ignore
                }
            }
            return mergedHistory.Values;
        }

        /// <summary>
        /// Merge two slice with same Time
        /// </summary>
        /// <param name="slice1">slice instance</param>
        /// <param name="slice2">slice instance</param>
        private Slice MergeSlice(Slice slice1, Slice slice2)
        {
            var time = slice1.Time;
            List<BaseData> rawData = new();
            List<TradeBar> tradeBars = new();
            List<QuoteBar> quoteBars = new();

            // Merge TradeBars
            if (slice1.Bars != null && slice2.Bars != null)
            {
                List<TradeBar> tempBars = new();
                Dictionary<DateTime, TradeBar> barsMapping = new();
                tempBars.AddRange(slice1.ListOfBars);
                tempBars.AddRange(slice2.ListOfBars);
                foreach (var bar in tempBars)
                {
                    // Assuming resolution of all bars are same as history request was same
                    if (!barsMapping.ContainsKey(bar.Time))
                    {
                        barsMapping.Add(bar.Time, bar);
                    }
                }
                tradeBars.AddRange(barsMapping.Values);
            }
            else
            {
                tradeBars = slice1.ListOfBars ?? slice2.ListOfBars;
            }

            // Merge QuoteBars
            // Merge Ticks

            // Compile all Data types together to convert into Slice
            rawData.AddRange(tradeBars);
            rawData.AddRange(quoteBars);

            // Return merged slice
            return new Slice(time, rawData);
        }
    }
}

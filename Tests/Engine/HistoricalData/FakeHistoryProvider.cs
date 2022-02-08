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
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;
using System;
using System.Collections.Generic;
using HistoryRequest = QuantConnect.Data.HistoryRequest;

namespace QuantConnect.Lean.Engine.HistoricalData
{
    /// <summary>
    /// Provides FAKE implementation of <see cref="IHistoryProvider"/>
    /// </summary>
    internal class FakeHistoryProvider : SynchronizingHistoryProvider
    {
        private IBrokerage _brokerage;

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
        }

        /// <summary>
        /// Gets the history for the requested securities
        /// </summary>
        /// <param name="requests">The historical data requests</param>
        /// <param name="sliceTimeZone">The time zone used when time stamping the slice instances</param>
        /// <returns>An enumerable of the slices of data covering the span specified in each request</returns>
        public override IEnumerable<Slice> GetHistory(IEnumerable<HistoryRequest> requests, DateTimeZone sliceTimeZone)
        {
            List<Slice> result = new();
            var slice1Date = new DateTime(2008, 01, 03);
            var slice2Date = new DateTime(2013, 06, 28, 09, 32, 0);
            var spy = new Symbol(SecurityIdentifier.GenerateEquity("SPY", Market.USA), "SPY");
            var aapl = new Symbol(SecurityIdentifier.GenerateEquity("AAPL", Market.USA), "SPY");
            var msft = new Symbol(SecurityIdentifier.GenerateEquity("MSFT", Market.USA), "SPY");
            var sbin = new Symbol(SecurityIdentifier.GenerateEquity("SBIN", Market.USA), "SPY");

            TradeBar tradeBar1 = new TradeBar { Symbol = spy, Time = DateTime.Now };
            TradeBar tradeBar2 = new TradeBar { Symbol = aapl, Time = DateTime.Now };
            var quoteBar1 = new QuoteBar { Symbol = spy, Time = DateTime.Now };
            var tick1 = new Tick(DateTime.Now, spy, 1.1m, 2.1m) { TickType = TickType.Trade };
            var split1 = new Split(spy, DateTime.Now, 1, 1, SplitType.SplitOccurred);
            var dividend1 = new Dividend(spy, DateTime.Now, 1, 1);
            var delisting1 = new Delisting(spy, DateTime.Now, 1, DelistingType.Delisted);
            var symbolChangedEvent1 = new SymbolChangedEvent(spy, DateTime.Now, "SPY", "SP");
            Slice slice1 = new Slice(slice1Date, new BaseData[] { tradeBar1, tradeBar2,
                quoteBar1, tick1, split1, dividend1, delisting1, symbolChangedEvent1
            });

            TradeBar tradeBar3 = new TradeBar { Symbol = msft, Time = DateTime.Now };
            TradeBar tradeBar4 = new TradeBar { Symbol = sbin, Time = DateTime.Now };
            var quoteBar2 = new QuoteBar { Symbol = sbin, Time = DateTime.Now };
            var tick2 = new Tick(DateTime.Now, sbin, 1.1m, 2.1m) { TickType = TickType.Trade };
            var split2 = new Split(sbin, DateTime.Now, 1, 1, SplitType.SplitOccurred);
            var dividend2 = new Dividend(sbin, DateTime.Now, 1, 1);
            var delisting2 = new Delisting(sbin, DateTime.Now, 1, DelistingType.Delisted);
            var symbolChangedEvent2 = new SymbolChangedEvent(sbin, DateTime.Now, "SBIN", "BIN");
            Slice slice2 = new Slice(slice2Date, new BaseData[] { tradeBar3, tradeBar4,
                quoteBar2, tick2, split2, dividend2, delisting2, symbolChangedEvent2
            });

            result.Add(slice1);
            result.Add(slice2);
            return result;
        }
    }
}

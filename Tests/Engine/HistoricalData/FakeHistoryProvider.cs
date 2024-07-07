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
using NodaTime;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;
using HistoryRequest = QuantConnect.Data.HistoryRequest;

namespace QuantConnect.Tests.Engine.HistoricalData
{
    /// <summary>
    /// Provides FAKE implementation of <see cref="IHistoryProvider"/>
    /// </summary>
    internal class TestHistoryProvider : HistoryProviderBase
    {
        public override int DataPointCount => 2;

        /// <summary>
        /// Initializes this history provider to work for the specified job
        /// </summary>
        /// <param name="parameters">The initialization parameters</param>
        public override void Initialize(HistoryProviderInitializeParameters parameters) { }

        /// <summary>
        /// Gets the history for the requested securities
        /// </summary>
        /// <param name="requests">The historical data requests</param>
        /// <param name="sliceTimeZone">The time zone used when time stamping the slice instances</param>
        /// <returns>An enumerable of the slices of data covering the span specified in each request</returns>
        public override IEnumerable<Slice> GetHistory(
            IEnumerable<HistoryRequest> requests,
            DateTimeZone sliceTimeZone
        )
        {
            List<Slice> result = new();
            var slice1Date = new DateTime(2008, 01, 03, 5, 0, 0);
            var slice2Date = new DateTime(2013, 06, 28, 13, 32, 0);

            TradeBar tradeBar1 = new TradeBar { Symbol = Symbols.SPY, Time = DateTime.Now };
            TradeBar tradeBar2 = new TradeBar { Symbol = Symbols.AAPL, Time = DateTime.Now };
            var quoteBar1 = new QuoteBar { Symbol = Symbols.SPY, Time = DateTime.Now };
            var tick1 = new Tick(DateTime.Now, Symbols.SPY, 1.1m, 2.1m)
            {
                TickType = TickType.Trade
            };
            var split1 = new Split(Symbols.SPY, DateTime.Now, 1, 1, SplitType.SplitOccurred);
            var dividend1 = new Dividend(Symbols.SPY, DateTime.Now, 1, 1);
            var delisting1 = new Delisting(Symbols.SPY, DateTime.Now, 1, DelistingType.Delisted);
            var symbolChangedEvent1 = new SymbolChangedEvent(
                Symbols.SPY,
                DateTime.Now,
                "SPY",
                "SP"
            );
            Slice slice1 = new Slice(
                slice1Date,
                new BaseData[]
                {
                    tradeBar1,
                    tradeBar2,
                    quoteBar1,
                    tick1,
                    split1,
                    dividend1,
                    delisting1,
                    symbolChangedEvent1
                },
                slice1Date
            );

            TradeBar tradeBar3 = new TradeBar { Symbol = Symbols.MSFT, Time = DateTime.Now };
            TradeBar tradeBar4 = new TradeBar { Symbol = Symbols.SBIN, Time = DateTime.Now };
            var quoteBar2 = new QuoteBar { Symbol = Symbols.SBIN, Time = DateTime.Now };
            var tick2 = new Tick(DateTime.Now, Symbols.SBIN, 1.1m, 2.1m)
            {
                TickType = TickType.Trade
            };
            var split2 = new Split(Symbols.SBIN, DateTime.Now, 1, 1, SplitType.SplitOccurred);
            var dividend2 = new Dividend(Symbols.SBIN, DateTime.Now, 1, 1);
            var delisting2 = new Delisting(Symbols.SBIN, DateTime.Now, 1, DelistingType.Delisted);
            var symbolChangedEvent2 = new SymbolChangedEvent(
                Symbols.SBIN,
                DateTime.Now,
                "SBIN",
                "BIN"
            );
            Slice slice2 = new Slice(
                slice2Date,
                new BaseData[]
                {
                    tradeBar3,
                    tradeBar4,
                    quoteBar2,
                    tick2,
                    split2,
                    dividend2,
                    delisting2,
                    symbolChangedEvent2
                },
                slice2Date
            );

            result.Add(slice1);
            result.Add(slice2);
            return result;
        }

        public void TriggerEvents()
        {
            OnInvalidConfigurationDetected(
                new InvalidConfigurationDetectedEventArgs(Symbols.SPY, "invalid config")
            );
            OnNumericalPrecisionLimited(
                new NumericalPrecisionLimitedEventArgs(Symbols.SPY, "invalid config")
            );
            OnStartDateLimited(new StartDateLimitedEventArgs(Symbols.SPY, "invalid config"));
            OnDownloadFailed(new DownloadFailedEventArgs(Symbols.SPY, "invalid config"));
            OnReaderErrorDetected(new ReaderErrorDetectedEventArgs(Symbols.SPY, "invalid config"));
        }
    }
}

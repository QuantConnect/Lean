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
using System.Globalization;
using System.Linq;
using NodaTime;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;
using QuantConnect.Logging;
using QuantConnect.Packets;
using QuantConnect.Securities;
using HistoryRequest = QuantConnect.Data.HistoryRequest;
using Order = QuantConnect.Orders.Order;

namespace QuantConnect.Brokerages.Alpaca
{
    /// <summary>
    /// Alpaca Brokerage implementation
    /// </summary>
    [BrokerageFactory(typeof(AlpacaBrokerageFactory))]
    public class AlpacaBrokerage : AlpacaApiBase
    {
        /// <summary>
        /// The maximum number of bars per historical data request
        /// </summary>
        public const int MaxBarsPerRequest = 5000;

        /// <summary>
        /// Initializes a new instance of the <see cref="AlpacaBrokerage"/> class.
        /// </summary>
        /// <param name="orderProvider">The order provider.</param>
        /// <param name="securityProvider">The holdings provider.</param>
        /// <param name="keyId">The Alpaca api key id</param>
        /// <param name="secretKey">The api secret key</param>
        /// <param name="tradingMode">The Alpaca trading mode. paper/live</param>
        public AlpacaBrokerage(IOrderProvider orderProvider, ISecurityProvider securityProvider, string keyId, string secretKey, string tradingMode)
            : base("Alpaca Brokerage")
        {
            var baseUrl = "api.alpaca.markets";
            if (tradingMode.Equals("paper")) baseUrl = "paper-" + baseUrl;
            baseUrl = "https://" + baseUrl;
            base.initialize(orderProvider, securityProvider, keyId, secretKey, baseUrl);

        }

        #region IBrokerage implementation

        /// <summary>
        /// Returns true if we're currently connected to the broker
        /// </summary>
        public override bool IsConnected
        {
            get { return IsConnected; }
        }

        /// <summary>
        /// Connects the client to the broker's remote servers
        /// </summary>
        public override void Connect()
        {
            if (IsConnected) return;

            base.Connect();
        }

        
        /// <summary>
        /// Gets all holdings for the account
        /// </summary>
        /// <returns>The current holdings from the account</returns>
        public override List<Holding> GetAccountHoldings()
        {
            var holdings = base.GetAccountHoldings();

            // Set MarketPrice in each Holding
            var alpacaSymbols = holdings
                .Select(x => x.Symbol.Value)
                .ToList();

            if (alpacaSymbols.Count > 0)
            {
                var quotes = base.GetRates(alpacaSymbols);
                foreach (var holding in holdings)
                {
                    var alpacaSymbol = holding.Symbol;
                    Tick tick;
                    if (quotes.TryGetValue(alpacaSymbol.Value, out tick))
                    {
                        holding.MarketPrice = (tick.BidPrice + tick.AskPrice) / 2;
                    }
                }
            }

            return holdings;
        }

        /// <summary>
        /// Gets the history for the requested security
        /// </summary>
        /// <param name="request">The historical data request</param>
        /// <returns>An enumerable of bars covering the span specified in the request</returns>
        public override IEnumerable<BaseData> GetHistory(HistoryRequest request)
        {

            var exchangeTimeZone = _marketHours.GetExchangeHours(Market.USA, request.Symbol, request.Symbol.SecurityType).TimeZone;

            var period = request.Resolution.ToTimeSpan();

            // set the starting date/time
            var startDateTime = request.StartTimeUtc;

            if (request.Resolution == Resolution.Tick)
            {
                var ticks = base.DownloadTicks(request.Symbol, startDateTime, request.EndTimeUtc, exchangeTimeZone).ToList();
                if (ticks.Count != 0)
                {
                    foreach (var tick in ticks)
                    {
                        yield return tick;
                    }
                }
            }
            else if (request.Resolution == Resolution.Second)
            {
                var quoteBars = base.DownloadQuoteBars(request.Symbol, startDateTime, request.EndTimeUtc, request.Resolution, exchangeTimeZone).ToList();
                if (quoteBars.Count != 0)
                {
                    foreach (var quoteBar in quoteBars)
                    {
                        yield return quoteBar;
                    }
                }
            }
            // Due to the slow processing time for QuoteBars in larger resolution, we change into TradeBar in these cases
            else
            {
                var tradeBars = base.DownloadTradeBars(request.Symbol, startDateTime, request.EndTimeUtc, request.Resolution, exchangeTimeZone).ToList();
                if (tradeBars.Count != 0)
                {
                    tradeBars.RemoveAt(0);
                    foreach (var tradeBar in tradeBars)
                    {
                        yield return tradeBar;
                    }
                }
            }
        }

        #endregion

        /// <summary>
        /// Returns a DateTime from an RFC3339 string (with microsecond resolution)
        /// </summary>
        /// <param name="time">The time string</param>
        public static DateTime GetDateTimeFromString(string time)
        {
            return DateTime.ParseExact(time, "yyyy-MM-dd'T'HH:mm:ss.ffffff'Z'", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Retrieves the current quotes for an instrument
        /// </summary>
        /// <param name="instrument">the instrument to check</param>
        /// <returns>Returns a Tick object with the current bid/ask prices for the instrument</returns>
        public Tick GetRates(string instrument)
        {
            return base.GetRates(new List<string> { instrument }).Values.First();
        }
        
    }
}

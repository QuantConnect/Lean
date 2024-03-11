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
using System.IO;
using QuantConnect.Util;
using System.Threading.Tasks;
using QuantConnect.Interfaces;
using System.Collections.Generic;
using System.Globalization;

namespace QuantConnect.Data.Shortable
{
    /// <summary>
    /// Sources short availability data from the local disk for the given brokerage
    /// </summary>
    public class LocalDiskShortableProvider : IShortableProvider
    {
        /// <summary>
        /// The data provider instance to use
        /// </summary>
        protected static IDataProvider DataProvider = Composer.Instance.GetPart<IDataProvider>();

        private string _ticker;
        private bool _scheduledCleanup;
        private Dictionary<DateTime, ShortableData> _shortableDataPerDate;

        /// <summary>
        /// The short availability provider
        /// </summary>
        protected string Brokerage { get; set; }

        /// <summary>
        /// Creates an instance of the class. Establishes the directory to read from.
        /// </summary>
        /// <param name="brokerage">Brokerage to read the short availability data</param>
        public LocalDiskShortableProvider(string brokerage)
        {
            Brokerage = brokerage.ToLowerInvariant();
        }

        /// <summary>
        /// Gets interest rate charged on borrowed shares for a given asset.
        /// </summary>
        /// <param name="symbol">Symbol to lookup fee rate</param>
        /// <param name="localTime">Time of the algorithm</param>
        /// <returns>Fee rate. Zero if the data for the brokerage/date does not exist.</returns>
        public decimal FeeRate(Symbol symbol, DateTime localTime)
        {
            if (symbol != null && GetCacheData(symbol).TryGetValue(localTime.Date, out var result))
            {
                return result.FeeRate;
            }
            // Any missing entry will be considered to be zero.
            return 0m;
        }

        /// <summary>
        /// Gets the Fed funds or other currency-relevant benchmark rate minus the interest rate charged on borrowed shares for a given asset.
        /// E.g.: Interest rate - borrow fee rate = borrow rebate rate: 5.32% - 0.25% = 5.07%.
        /// </summary>
        /// <param name="symbol">Symbol to lookup rebate rate</param>
        /// <param name="localTime">Time of the algorithm</param>
        /// <returns>Rebate fee. Zero if the data for the brokerage/date does not exist.</returns>
        public decimal RebateRate(Symbol symbol, DateTime localTime)
        {
            if (symbol != null && GetCacheData(symbol).TryGetValue(localTime.Date, out var result))
            {
                return result.RebateFee;
            }
            // Any missing entry will be considered to be zero.
            return 0m;
        }

        /// <summary>
        /// Gets the quantity shortable for the Symbol at the given date.
        /// </summary>
        /// <param name="symbol">Symbol to lookup shortable quantity</param>
        /// <param name="localTime">Time of the algorithm</param>
        /// <returns>Quantity shortable. Null if the data for the brokerage/date does not exist.</returns>
        public long? ShortableQuantity(Symbol symbol, DateTime localTime)
        {
            if (symbol != null && GetCacheData(symbol).TryGetValue(localTime.Date, out var result))
            {
                return result.ShortableQuantity;
            }
            // Any missing entry will be considered to be Shortable.
            return null;
        }

        /// <summary>
        /// We cache data per ticker
        /// </summary>
        /// <param name="symbol">The requested symbol</param>
        private Dictionary<DateTime, ShortableData> GetCacheData(Symbol symbol)
        {
            var result = _shortableDataPerDate;
            if (_ticker == symbol.Value)
            {
                return result;
            }

            if (!_scheduledCleanup)
            {
                // we schedule it once
                _scheduledCleanup = true;
                ClearCache();
            }

            // create a new collection
            _ticker = symbol.Value;
            result = _shortableDataPerDate = new();

            // Implicitly trusts that Symbol.Value has been mapped and updated to the latest ticker
            var shortableSymbolFile = Path.Combine(Globals.DataFolder, symbol.SecurityType.SecurityTypeToLower(), symbol.ID.Market,
                "shortable", Brokerage, "symbols", $"{_ticker.ToLowerInvariant()}.csv");

            foreach (var line in DataProvider.ReadLines(shortableSymbolFile))
            {
                if (string.IsNullOrEmpty(line) || line.StartsWith("#"))
                {
                    // ignore empty or comment lines
                    continue;
                }
                // Data example. The rates, if available, are expressed in percentage.
                // 20201221,2000,5.0700,0.2500
                var csv = line.Split(',');
                var date = Parse.DateTimeExact(csv[0], "yyyyMMdd");
                var lenght = csv.Length;
                var shortableQuantity = csv[1].IfNotNullOrEmpty(s => long.Parse(s, NumberStyles.Any, CultureInfo.InvariantCulture));
                var rebateRate = csv.Length > 2 ? csv[2].IfNotNullOrEmpty(s => decimal.Parse(s, NumberStyles.Any, CultureInfo.InvariantCulture)) : 0;
                var feeRate = csv.Length > 3 ? csv[3].IfNotNullOrEmpty(s => decimal.Parse(s, NumberStyles.Any, CultureInfo.InvariantCulture)) : 0;
                result[date] = new ShortableData(shortableQuantity, rebateRate / 100, feeRate / 100);
            }

            return result;
        }

        /// <summary>
        /// For live deployments we don't want to have stale short quantity so we refresh them every day
        /// </summary>
        private void ClearCache()
        {
            var now = DateTime.UtcNow;
            var tomorrowMidnight = now.Date.AddDays(1);
            var delayToClean = tomorrowMidnight - now;

            Task.Delay(delayToClean).ContinueWith((_) =>
            {
                // create new instances so we don't need to worry about locks
                _ticker = null;
                _shortableDataPerDate = new();

                ClearCache();
            });
        }

        protected record ShortableData(long? ShortableQuantity, decimal RebateFee, decimal FeeRate);
    }
}

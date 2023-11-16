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
using QuantConnect.Interfaces;
using QuantConnect.Configuration;
using System.Collections.Generic;

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
        protected static IDataProvider DataProvider = Composer.Instance.GetExportedValueByTypeName<IDataProvider>(Config.Get("data-provider",
                "DefaultDataProvider"), forceTypeNameOnExisting: false);

        private string _ticker;
        private Dictionary<DateTime, long> _shortableQuantityPerDate;

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
        /// Gets the quantity shortable for the Symbol at the given date.
        /// </summary>
        /// <param name="symbol">Symbol to lookup shortable quantity</param>
        /// <param name="localTime">Time of the algorithm</param>
        /// <returns>Quantity shortable. Null if the data for the brokerage/date does not exist.</returns>
        public long? ShortableQuantity(Symbol symbol, DateTime localTime)
        {
            if(_ticker != symbol.Value)
            {
                // symbol could of been remapped
                CacheData(symbol);
            }

            if (!_shortableQuantityPerDate.TryGetValue(localTime.Date, out var result))
            {
                // Any missing entry will be considered to be Shortable.
                return null;
            }
            return result;
        }

        /// <summary>
        /// We cache data per ticker
        /// </summary>
        /// <param name="symbol">The requested symbol</param>
        private void CacheData(Symbol symbol)
        {
            _ticker = symbol.Value;
            _shortableQuantityPerDate ??= new();
            _shortableQuantityPerDate.Clear();

            // Implicitly trusts that Symbol.Value has been mapped and updated to the latest ticker
            var shortableSymbolFile = Path.Combine(Globals.DataFolder, symbol.SecurityType.SecurityTypeToLower(), symbol.ID.Market,
                "shortable", Brokerage, "symbols", $"{_ticker.ToLowerInvariant()}.csv");

            foreach (var line in DataProvider.ReadLines(shortableSymbolFile))
            {
                var csv = line.Split(',');
                var date = Parse.DateTimeExact(csv[0], "yyyyMMdd");
                var quantity = Parse.Long(csv[1]);
                _shortableQuantityPerDate[date] = quantity;
            }
        }
    }
}

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
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Securities;

namespace QuantConnect.ToolBox.RandomDataGenerator
{
    /// <summary>
    /// <see cref="IDataDownloader"/> implementation serving the generated data it was fed,
    /// used to create the derivative universe files with the same code path as the data downloaders
    /// </summary>
    public class InMemoryDataDownloader : IDataDownloader
    {
        private readonly List<(TickType TickType, BaseData Data)> _data = new();

        /// <summary>
        /// The exchange hours of the data this instance holds
        /// </summary>
        public SecurityExchangeHours ExchangeHours { get; }

        /// <summary>
        /// Creates a new instance
        /// </summary>
        /// <param name="exchangeHours">The exchange hours of the data this instance will hold, which is in the exchange time zone</param>
        public InMemoryDataDownloader(SecurityExchangeHours exchangeHours)
        {
            ExchangeHours = exchangeHours;
        }

        /// <summary>
        /// Adds data to serve
        /// </summary>
        /// <param name="tickType">The tick type of the data</param>
        /// <param name="data">The data to add, in the exchange time zone</param>
        public void Add(TickType tickType, IEnumerable<BaseData> data)
        {
            _data.AddRange(data.Select(x => (tickType, x)));
        }

        /// <summary>
        /// Get historical data enumerable for a single symbol, type and resolution given this start and end time (in UTC).
        /// Requests for a canonical symbol will return the data of all its contracts
        /// </summary>
        /// <param name="dataDownloaderGetParameters">model class for passing in parameters for historical data</param>
        /// <returns>Enumerable of base data for this symbol</returns>
        public IEnumerable<BaseData> Get(DataDownloaderGetParameters dataDownloaderGetParameters)
        {
            var symbol = dataDownloaderGetParameters.Symbol;
            var start = dataDownloaderGetParameters.StartUtc.ConvertFromUtc(ExchangeHours.TimeZone);
            var end = dataDownloaderGetParameters.EndUtc.ConvertFromUtc(ExchangeHours.TimeZone);

            return _data
                .Where(x => x.TickType == dataDownloaderGetParameters.TickType
                    && x.Data.Time >= start && x.Data.Time < end
                    && (symbol.IsCanonical() ? x.Data.Symbol.HasCanonical() && x.Data.Symbol.Canonical == symbol : x.Data.Symbol == symbol))
                .Select(x => x.Data);
        }
    }
}

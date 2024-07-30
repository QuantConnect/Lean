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

using QuantConnect.Util;
using QuantConnect.Data;
using QuantConnect.Packets;
using QuantConnect.Interfaces;
using QuantConnect.Securities;
using QuantConnect.Configuration;

namespace QuantConnect.DownloaderDataProvider.Launcher.Models
{
    /// <summary>
    /// Class for downloading data from a brokerage.
    /// </summary>
    public class BrokerageDataDownloader : IDataDownloader
    {
        /// <summary>
        /// Represents the Brokerage implementation.
        /// </summary>
        private IBrokerage _brokerage;

        /// <summary>
        /// Provides access to exchange hours and raw data times zones in various markets
        /// </summary>
        private readonly MarketHoursDatabase _marketHoursDatabase = MarketHoursDatabase.FromDataFolder();

        /// <summary>
        /// Initializes a new instance of the <see cref="BrokerageDataDownloader"/> class.
        /// </summary>
        public BrokerageDataDownloader()
        {
            var liveNodeConfiguration = new LiveNodePacket() { Brokerage = Config.Get("data-downloader-brokerage") };

            try
            {
                // import the brokerage data for the configured brokerage
                var brokerageFactory = Composer.Instance.Single<IBrokerageFactory>(factory => factory.BrokerageType.MatchesTypeName(liveNodeConfiguration.Brokerage));
                liveNodeConfiguration.BrokerageData = brokerageFactory.BrokerageData;
            }
            catch (InvalidOperationException error)
            {
                throw new InvalidOperationException($"{nameof(BrokerageDataDownloader)}.An error occurred while resolving brokerage data for a live job. Brokerage: {liveNodeConfiguration.Brokerage}.", error);
            }

            _brokerage = Composer.Instance.GetExportedValueByTypeName<IBrokerage>(liveNodeConfiguration.Brokerage);
            ((IDataQueueHandler)_brokerage).SetJob(liveNodeConfiguration);
        }

        /// <summary>
        /// Get historical data enumerable for a single symbol, type and resolution given this start and end time (in UTC).
        /// </summary>
        /// <param name="dataDownloaderGetParameters">model class for passing in parameters for historical data</param>
        /// <returns>Enumerable of base data for this symbol</returns>
        public IEnumerable<BaseData>? Get(DataDownloaderGetParameters dataDownloaderGetParameters)
        {
            var symbol = dataDownloaderGetParameters.Symbol;
            var resolution = dataDownloaderGetParameters.Resolution;
            var startUtc = dataDownloaderGetParameters.StartUtc;
            var endUtc = dataDownloaderGetParameters.EndUtc;
            var tickType = dataDownloaderGetParameters.TickType;

            var dataType = LeanData.GetDataType(resolution, tickType);
            var exchangeHours = _marketHoursDatabase.GetExchangeHours(symbol.ID.Market, symbol, symbol.SecurityType);
            var dataTimeZone = _marketHoursDatabase.GetDataTimeZone(symbol.ID.Market, symbol, symbol.SecurityType);

            var historyRequest = new Data.HistoryRequest(startUtc, endUtc, dataType, symbol, resolution, exchangeHours, dataTimeZone, resolution,
                true, false, DataNormalizationMode.Raw, tickType);

            var historyData = _brokerage.GetHistory(historyRequest);

            if (historyData == null)
            {
                return null;
            }

            return historyData;
        }
    }
}

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
            var liveNodeConfiguration = new LiveNodePacket()
            {
                Brokerage = Config.Get("data-downloader-brokerage"),
                UserToken = Globals.UserToken,
                UserId = Globals.UserId,
                ProjectId = Globals.ProjectId,
                OrganizationId = Globals.OrganizationID,
                Version = Globals.Version,
                DeploymentTarget = DeploymentTarget.LocalPlatform
            };

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

            _brokerage.Message += (object _, Brokerages.BrokerageMessageEvent e) =>
            {
                if (e.Type == Brokerages.BrokerageMessageType.Error)
                {
                    Logging.Log.Error(e.Message);
                }
                else
                {
                    Logging.Log.Trace(e.Message);
                }
            };

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

            var symbols = new List<Symbol> { symbol };
            if (symbol.IsCanonical())
            {
                symbols = GetChainSymbols(symbol, true).ToList();
            }

            return symbols
                .Select(symbol =>
                {
                    var request = new Data.HistoryRequest(startUtc, endUtc, dataType, symbol, resolution, exchangeHours: exchangeHours, dataTimeZone: dataTimeZone, resolution,
                        includeExtendedMarketHours: true, false, DataNormalizationMode.Raw, tickType);

                    var history = _brokerage.GetHistory(request);

                    if (history == null)
                    {
                        Logging.Log.Trace($"{nameof(BrokerageDataDownloader)}.{nameof(Get)}: Ignoring history request for unsupported symbol {symbol}");
                    }

                    return history;
                })
                .Where(history => history != null)
                .SelectMany(history => history);
        }

        /// <summary>
        /// Returns an IEnumerable of Future/Option contract symbols for the given root ticker
        /// </summary>
        /// <param name="symbol">The Symbol to get futures/options chain for</param>
        /// <param name="includeExpired">Include expired contracts</param>
        private IEnumerable<Symbol> GetChainSymbols(Symbol symbol, bool includeExpired)
        {
            if (_brokerage is IDataQueueUniverseProvider universeProvider)
            {
                return universeProvider.LookupSymbols(symbol, includeExpired);
            }
            else
            {
                throw new InvalidOperationException($"{nameof(BrokerageDataDownloader)}.{nameof(GetChainSymbols)}: The current brokerage does not support fetching canonical symbols. Please ensure your brokerage instance supports this feature.");
            }
        }
    }
}

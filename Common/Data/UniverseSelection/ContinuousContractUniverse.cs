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
using System.Linq;
using QuantConnect.Util;
using QuantConnect.Interfaces;
using QuantConnect.Securities;
using System.Collections.Generic;
using QuantConnect.Data.Auxiliary;

namespace QuantConnect.Data.UniverseSelection
{
    /// <summary>
    /// Continuous contract universe selection that based on the requested mapping mode will select each symbol
    /// </summary>
    public class ContinuousContractUniverse : Universe, ITimeTriggeredUniverse
    {
        private readonly  IMapFileProvider _mapFileProvider;
        private readonly SubscriptionDataConfig _config;
        private readonly Security _security;
        private readonly bool _liveMode;
        private Symbol _currentSymbol;
        private string _mappedSymbol;

        /// <summary>
        /// True if this universe filter can run async in the data stack
        /// TODO: see IContinuousSecurity.Mapped
        /// </summary>
        public override bool Asynchronous => false;

        /// <summary>
        /// Creates a new instance
        /// </summary>
        public ContinuousContractUniverse(Security security, UniverseSettings universeSettings, bool liveMode, SubscriptionDataConfig universeConfig)
            : base(universeConfig)
        {
            _security = security;
            _liveMode = liveMode;
            UniverseSettings = universeSettings;
            _mapFileProvider = Composer.Instance.GetPart<IMapFileProvider>();

            _config = new SubscriptionDataConfig(Configuration, dataMappingMode: UniverseSettings.DataMappingMode, symbol: _security.Symbol.Canonical);
        }

        /// <summary>
        /// Performs universe selection based on the symbol mapping
        /// </summary>
        /// <param name="utcTime">The current utc time</param>
        /// <param name="data">Empty data</param>
        /// <returns>The symbols to use</returns>
        public override IEnumerable<Symbol> SelectSymbols(DateTime utcTime, BaseDataCollection data)
        {
            yield return _security.Symbol.Canonical;

            var mapFile = _mapFileProvider.ResolveMapFile(_config);
            var mappedSymbol = mapFile.GetMappedSymbol(utcTime.ConvertFromUtc(_security.Exchange.TimeZone), dataMappingMode: _config.DataMappingMode);
            if (!string.IsNullOrEmpty(mappedSymbol) && mappedSymbol != _mappedSymbol)
            {
                if (_currentSymbol != null)
                {
                    // let's emit the old and new for the mapping date
                    yield return _currentSymbol;
                }
                _mappedSymbol = mappedSymbol;

                _currentSymbol = _security.Symbol.Canonical
                    .UpdateMappedSymbol(mappedSymbol, Configuration.ContractDepthOffset)
                    .Underlying;
            }

            if (_currentSymbol != null)
            {
                // TODO: this won't work with async universe selection
                ((IContinuousSecurity)_security).Mapped = _currentSymbol;
                yield return _currentSymbol;
            }
        }

        /// <summary>
        /// Gets the subscription requests to be added for the specified security
        /// </summary>
        /// <param name="security">The security to get subscriptions for</param>
        /// <param name="currentTimeUtc">The current time in utc. This is the frontier time of the algorithm</param>
        /// <param name="maximumEndTimeUtc">The max end time</param>
        /// <param name="subscriptionService">Instance which implements <see cref="ISubscriptionDataConfigService"/> interface</param>
        /// <returns>All subscriptions required by this security</returns>
        public override IEnumerable<SubscriptionRequest> GetSubscriptionRequests(Security security,
            DateTime currentTimeUtc,
            DateTime maximumEndTimeUtc,
            ISubscriptionDataConfigService subscriptionService)
        {
            var configs = AddConfigurations(subscriptionService, UniverseSettings, security.Symbol);
            return configs.Select(config => new SubscriptionRequest(isUniverseSubscription: false,
                universe: this,
                security: security,
                configuration: new SubscriptionDataConfig(config, isInternalFeed: config.IsInternalFeed || config.TickType == TickType.OpenInterest),
                startTimeUtc: currentTimeUtc,
                endTimeUtc: maximumEndTimeUtc));
        }

        /// <summary>
        /// Each tradeable day of the future we trigger a new selection.
        /// Allows use to select the current contract
        /// </summary>
        public IEnumerable<DateTime> GetTriggerTimes(DateTime startTimeUtc, DateTime endTimeUtc, MarketHoursDatabase marketHoursDatabase)
        {
            var startTimeLocal = startTimeUtc.ConvertFromUtc(_security.Exchange.TimeZone);
            var endTimeLocal = endTimeUtc.ConvertFromUtc(_security.Exchange.TimeZone);

            return Time.EachTradeableDay(_security, startTimeLocal, endTimeLocal)
                // in live trading selection happens on start see 'DataQueueFuturesChainUniverseDataCollectionEnumerator'
                .Where(tradeableDay => _liveMode || tradeableDay >= startTimeLocal)
                // in live trading we delay selection so that we make sure auxiliary data is ready
                .Select(time => _liveMode ? time.Add(Time.LiveAuxiliaryDataOffset) : time);
        }

        /// <summary>
        /// Helper method to add and get the required configurations associated with a continuous universe
        /// </summary>
        public static List<SubscriptionDataConfig> AddConfigurations(ISubscriptionDataConfigService subscriptionService, UniverseSettings universeSettings, Symbol symbol)
        {
            List<SubscriptionDataConfig> configs = new(universeSettings.SubscriptionDataTypes.Count);
            foreach (var pair in universeSettings.SubscriptionDataTypes)
            {
                configs.AddRange(subscriptionService.Add(symbol,
                    universeSettings.Resolution,
                    universeSettings.FillForward,
                    universeSettings.ExtendedMarketHours,
                    dataNormalizationMode: universeSettings.DataNormalizationMode,
                    // we need to provider the data types we want, else since it's canonical it would assume the default ZipEntry type used in universe chain
                    subscriptionDataTypes: new List<Tuple<Type, TickType>> { pair },
                    dataMappingMode: universeSettings.DataMappingMode,
                    contractDepthOffset: (uint)Math.Abs(universeSettings.ContractDepthOffset),
                    // open interest is internal and the underlying mapped contracts of the continuous canonical
                    isInternalFeed: !symbol.IsCanonical() || pair.Item2 == TickType.OpenInterest));
            }
            return configs;
        }

        /// <summary>
        /// Creates a continuous universe symbol
        /// </summary>
        /// <param name="symbol">The associated symbol</param>
        /// <returns>A symbol for a continuous universe of the specified symbol</returns>
        public static Symbol CreateSymbol(Symbol symbol)
        {
            var ticker = $"qc-universe-continuous-{symbol.ID.Market.ToLowerInvariant()}-{symbol.SecurityType}-{symbol.ID.Symbol}";
            return UniverseExtensions.CreateSymbol(symbol.SecurityType, symbol.ID.Market, ticker);
        }
    }
}

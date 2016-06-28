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
 *
*/

using System;
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;
using QuantConnect.Securities;
using QuantConnect.Securities.Option;
using QuantConnect.Util;

namespace QuantConnect.Data.UniverseSelection
{
    /// <summary>
    /// Defines a universe for a single option chain
    /// </summary>
    public class OptionChainUniverse : Universe
    {
        private static readonly IReadOnlyList<TickType> QuotesAndTrades = new[] { TickType.Quote, TickType.Trade };

        private BaseData _underlying;
        private readonly Option _option;
        private readonly UniverseSettings _universeSettings;

        /// <summary>
        /// Initializes a new instance of the <see cref="OptionChainUniverse"/> class
        /// </summary>
        /// <param name="option">The canonical option chain security</param>
        /// <param name="universeSettings">The universe settings to be used for new subscriptions</param>
        /// <param name="securityInitializer">The security initializer to use on newly created securities</param>
        public OptionChainUniverse(Option option, UniverseSettings universeSettings, ISecurityInitializer securityInitializer = null)
            : base(option.SubscriptionDataConfig, securityInitializer)
        {
            _option = option;
            _universeSettings = universeSettings;
        }

        /// <summary>
        /// Gets the settings used for subscriptons added for this universe
        /// </summary>
        public override UniverseSettings UniverseSettings
        {
            get { return _universeSettings; }
        }

        /// <summary>
        /// Performs universe selection using the data specified
        /// </summary>
        /// <param name="utcTime">The current utc time</param>
        /// <param name="data">The symbols to remain in the universe</param>
        /// <returns>The data that passes the filter</returns>
        public override IEnumerable<Symbol> SelectSymbols(DateTime utcTime, BaseDataCollection data)
        {
            var optionsUniverseDataCollection = data as OptionChainUniverseDataCollection;
            if (optionsUniverseDataCollection == null)
            {
                throw new ArgumentException(string.Format("Expected data of type '{0}'", typeof (OptionChainUniverseDataCollection).Name));
            }

            _underlying = optionsUniverseDataCollection.Underlying ?? _underlying;
            optionsUniverseDataCollection.Underlying = _underlying;

            if (_underlying == null || data.Data.Count == 0)
            {
                return Unchanged;
            }

            var availableContracts = optionsUniverseDataCollection.Data.Select(x => x.Symbol);
            var results = _option.ContractFilter.Filter(availableContracts, _underlying).ToHashSet();

            // we save off the filtered results to the universe data collection for later
            // population into the OptionChain. This is non-ideal and could be remedied by
            // the universe subscription emitting a special type after selection that could
            // be checked for in TimeSlice.Create, but for now this will do
            optionsUniverseDataCollection.FilteredContracts = results;

            return results;
        }

        /// <summary>
        /// Gets the subscription requests to be added for the specified security
        /// </summary>
        /// <param name="security">The security to get subscriptions for</param>
        /// <param name="currentTimeUtc">The current time in utc. This is the frontier time of the algorithm</param>
        /// <param name="maximumEndTimeUtc">The max end time</param>
        /// <returns>All subscriptions required by this security</returns>
        public override IEnumerable<SubscriptionRequest> GetSubscriptionRequests(Security security, DateTime currentTimeUtc, DateTime maximumEndTimeUtc)
        {
            // we want to return both quote and trade subscriptions
            return QuotesAndTrades
                .Select(tickType => new SubscriptionDataConfig(
                    objectType: GetDataType(UniverseSettings.Resolution, tickType),
                    symbol: security.Symbol,
                    resolution: UniverseSettings.Resolution,
                    dataTimeZone: Configuration.DataTimeZone,
                    exchangeTimeZone: security.Exchange.TimeZone,
                    fillForward: UniverseSettings.FillForward,
                    extendedHours: UniverseSettings.ExtendedMarketHours,
                    isInternalFeed: false,
                    isCustom: false,
                    tickType: tickType,
                    isFilteredSubscription: true
                    ))
                .Select(config => new SubscriptionRequest(
                    isUniverseSubscription: false,
                    universe: this,
                    security: security,
                    configuration: config,
                    startTimeUtc: currentTimeUtc,
                    endTimeUtc: maximumEndTimeUtc
                    ));
        }

        /// <summary>
        /// Creates and configures a security for the specified symbol
        /// </summary>
        /// <param name="symbol">The symbol of the security to be created</param>
        /// <param name="algorithm">The algorithm instance</param>
        /// <param name="marketHoursDatabase">The market hours database</param>
        /// <param name="symbolPropertiesDatabase">The symbol properties database</param>
        /// <returns>The newly initialized security object</returns>
        public override Security CreateSecurity(Symbol symbol, IAlgorithm algorithm, MarketHoursDatabase marketHoursDatabase, SymbolPropertiesDatabase symbolPropertiesDatabase)
        {
            // set the underlying security and pricing model from the canonical security
            var option = (Option)base.CreateSecurity(symbol, algorithm, marketHoursDatabase, symbolPropertiesDatabase);
            option.Underlying = _option.Underlying;
            option.PriceModel = _option.PriceModel;
            return option;
        }

        /// <summary>
        /// Determines whether or not the specified security can be removed from
        /// this universe. This is useful to prevent securities from being taken
        /// out of a universe before the algorithm has had enough time to make
        /// decisions on the security
        /// </summary>
        /// <param name="utcTime">The current utc time</param>
        /// <param name="security">The security to check if its ok to remove</param>
        /// <returns>True if we can remove the security, false otherwise</returns>
        public override bool CanRemoveMember(DateTime utcTime, Security security)
        {
            // if we haven't begun receiving data for this security then it's safe to remove
            var lastData = security.Cache.GetData();
            if (lastData == null)
            {
                return true;
            }

            // only remove members on day changes, this prevents us from needing to
            // fast forward contracts continuously as price moves and out filtered
            // contracts change thoughout the day
            var localTime = utcTime.ConvertFromUtc(security.Exchange.TimeZone);
            if (localTime.Date != lastData.Time.Date)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Gets the data type required for the specified combination of resolution and tick type
        /// </summary>
        private static Type GetDataType(Resolution resolution, TickType tickType)
        {
            if (resolution == Resolution.Tick) return typeof(Tick);
            if (tickType == TickType.Quote) return typeof(QuoteBar);
            return typeof(TradeBar);
        }
    }
}

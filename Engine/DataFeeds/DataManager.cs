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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Data.Auxiliary;
using QuantConnect.Interfaces;
using QuantConnect.Logging;
using QuantConnect.Securities;
using QuantConnect.Util;

namespace QuantConnect.Lean.Engine.DataFeeds
{
    /// <summary>
    /// DataManager will manage the subscriptions for both the DataFeeds and the SubscriptionManager
    /// </summary>
    public class DataManager : IAlgorithmSubscriptionManager, IDataFeedSubscriptionManager, IDataManager
    {
        private readonly IDataFeed _dataFeed;
        private readonly IAlgorithmSettings _algorithmSettings;
        private readonly MarketHoursDatabase _marketHoursDatabase = MarketHoursDatabase.FromDataFolder();

        /// There is no ConcurrentHashSet collection in .NET,
        /// so we use ConcurrentDictionary with byte value to minimize memory usage
        private readonly ConcurrentDictionary<SubscriptionDataConfig, SubscriptionDataConfig> _subscriptionManagerSubscriptions
            = new ConcurrentDictionary<SubscriptionDataConfig, SubscriptionDataConfig>();

        /// <summary>
        /// Creates a new instance of the DataManager
        /// </summary>
        public DataManager(IDataFeed dataFeed, UniverseSelection universeSelection, IAlgorithmSettings algorithmSettings)
        {
            _dataFeed = dataFeed;
            UniverseSelection = universeSelection;
            _algorithmSettings = algorithmSettings;
        }

        #region IDataFeedSubscriptionManager

        /// <summary>
        /// Gets the data feed subscription collection
        /// </summary>
        public SubscriptionCollection DataFeedSubscriptions { get; } = new SubscriptionCollection();

        #endregion

        #region IAlgorithmSubscriptionManager

        /// <summary>
        /// The different <see cref="TickType"/> each <see cref="SecurityType"/> supports
        /// </summary>
        private Dictionary<SecurityType, List<TickType>> _availableDataTypes;

        /// <summary>
        /// Gets all the current data config subscriptions that are being processed for the SubscriptionManager
        /// </summary>
        public IEnumerable<SubscriptionDataConfig> SubscriptionManagerSubscriptions => _subscriptionManagerSubscriptions.Select(x => x.Key);

        /// <summary>
        /// Gets existing or adds new <see cref="SubscriptionDataConfig"/>
        /// </summary>
        /// <returns>Returns the SubscriptionDataConfig instance used</returns>
        public SubscriptionDataConfig SubscriptionManagerGetOrAdd(SubscriptionDataConfig newConfig)
        {
            var config = _subscriptionManagerSubscriptions.GetOrAdd(newConfig, newConfig);

            // if the reference is not the same, means it was already there and we did not add anything new
            if (!ReferenceEquals(config, newConfig))
            {
                Log.Trace("DataManager.SubscriptionManagerGetOrAdd(): subscription already added: " + config);
            }
            else
            {
                // count data subscriptions by symbol, ignoring multiple data types
                var uniqueCount = SubscriptionManagerSubscriptions
                    .Where(x => !x.Symbol.IsCanonical())
                    // TODO should limit subscriptions or unique securities
                    .DistinctBy(x => x.Symbol.Value)
                    .Count();

                if (uniqueCount > _algorithmSettings.DataSubscriptionLimit)
                {
                    throw new Exception(
                        $"The maximum number of concurrent market data subscriptions was exceeded ({_algorithmSettings.DataSubscriptionLimit})." +
                        "Please reduce the number of symbols requested or increase the limit using Settings.DataSubscriptionLimit.");
                }
            }

            return config;
        }

        /// <summary>
        /// Returns the amount of data config subscriptions processed for the SubscriptionManager
        /// </summary>
        public int SubscriptionManagerCount()
        {
            return _subscriptionManagerSubscriptions.Skip(0).Count();
        }

        /// <summary>
        /// Get the data feed types for a given <see cref="SecurityType"/> <see cref="Resolution"/>
        /// </summary>
        /// <param name="symbolSecurityType">The <see cref="SecurityType"/> used to determine the types</param>
        /// <param name="resolution">The resolution of the data requested</param>
        /// <param name="isCanonical">Indicates whether the security is Canonical (future and options)</param>
        /// <returns>Types that should be added to the <see cref="SubscriptionDataConfig"/></returns>
        public List<Tuple<Type, TickType>> LookupSubscriptionConfigDataTypes(SecurityType symbolSecurityType, Resolution resolution, bool isCanonical)
        {
            if (isCanonical)
            {
                return new List<Tuple<Type, TickType>> { new Tuple<Type, TickType>(typeof(ZipEntryName), TickType.Quote) };
            }
            return _availableDataTypes[symbolSecurityType].Select(tickType => new Tuple<Type, TickType>(LeanData.GetDataType(resolution, tickType), tickType)).ToList();
        }

        /// <summary>
        /// Sets up the available data types
        /// </summary>
        public void SetAvailableDataTypes(Dictionary<SecurityType, List<TickType>> availableDataTypes)
        {
            _availableDataTypes = availableDataTypes;
        }

        #region ISubscriptionDataConfigBuilder

        /// <summary>
        /// Creates a list of <see cref="SubscriptionDataConfig"/> for a given symbol and configuration.
        /// Can optionally pass in desired subscription data types to use.
        /// </summary>
        public List<SubscriptionDataConfig> Create(Symbol symbol, Resolution resolution,
                                                   bool fillForward, bool extendedMarketHours,
                                                   bool isFilteredSubscription = true,
                                                   bool isInternalFeed = false, bool isCustomData = false,
                                                   List<Tuple<Type, TickType>> subscriptionDataTypes = null
            )
        {
            var dataTypes = subscriptionDataTypes ?? LookupSubscriptionConfigDataTypes(symbol.SecurityType, resolution, symbol.IsCanonical());

            var marketHoursDbEntry = _marketHoursDatabase.GetEntry(symbol.ID.Market, symbol, symbol.ID.SecurityType);
            var exchangeHours = marketHoursDbEntry.ExchangeHours;
            var dataNormalizationMode = symbol.ID.SecurityType == SecurityType.Option || symbol.ID.SecurityType == SecurityType.Future
                                        ? DataNormalizationMode.Raw : DataNormalizationMode.Adjusted;

            if (marketHoursDbEntry.DataTimeZone == null)
            {
                throw new ArgumentNullException(nameof(marketHoursDbEntry.DataTimeZone), "DataTimeZone is a required parameter for new subscriptions.  Set to the time zone the raw data is time stamped in.");
            }
            if (exchangeHours.TimeZone == null)
            {
                throw new ArgumentNullException(nameof(exchangeHours.TimeZone), "ExchangeTimeZone is a required parameter for new subscriptions.  Set to the time zone the security exchange resides in.");
            }
            if (!dataTypes.Any())
            {
                throw new ArgumentNullException(nameof(dataTypes), "At least one type needed to create security");
            }

            var result = (from subscriptionDataType in dataTypes
                          let dataType = subscriptionDataType.Item1
                          let tickType = subscriptionDataType.Item2
                          select new SubscriptionDataConfig(dataType, symbol, resolution, marketHoursDbEntry.DataTimeZone,
                              exchangeHours.TimeZone, fillForward, extendedMarketHours,
                              isInternalFeed: isInternalFeed, isCustom: isCustomData,
                              isFilteredSubscription: isFilteredSubscription, tickType: tickType,
                              dataNormalizationMode: dataNormalizationMode)).ToList();
            return result;
        }

        #endregion

        #endregion

        #region IDataManager

        /// <summary>
        /// Get the universe selection instance
        /// </summary>
        public UniverseSelection UniverseSelection { get; }

        /// <summary>
        /// Returns an enumerable which provides the data to stream to the algorithm
        /// </summary>
        public IEnumerable<TimeSlice> StreamData()
        {
            return _dataFeed;
        }

        #endregion
    }
}

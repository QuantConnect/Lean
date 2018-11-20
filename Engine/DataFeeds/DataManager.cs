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
using System.Collections.Specialized;
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Data.Auxiliary;
using QuantConnect.Data.UniverseSelection;
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
        private readonly IAlgorithmSettings _algorithmSettings;
        private readonly IDataFeed _dataFeed;
        private readonly MarketHoursDatabase _marketHoursDatabase;
        private readonly ITimeKeeper _timeKeeper;
        private readonly bool _liveMode;

        /// There is no ConcurrentHashSet collection in .NET,
        /// so we use ConcurrentDictionary with byte value to minimize memory usage
        private readonly ConcurrentDictionary<SubscriptionDataConfig, SubscriptionDataConfig> _subscriptionManagerSubscriptions
            = new ConcurrentDictionary<SubscriptionDataConfig, SubscriptionDataConfig>();

        /// <summary>
        /// Creates a new instance of the DataManager
        /// </summary>
        public DataManager(
            IDataFeed dataFeed,
            UniverseSelection universeSelection,
            IAlgorithm algorithm,
            ITimeKeeper timeKeeper,
            MarketHoursDatabase marketHoursDatabase)
        {
            _dataFeed = dataFeed;
            UniverseSelection = universeSelection;
            UniverseSelection.SetDataManager(this);
            _algorithmSettings = algorithm.Settings;
            AvailableDataTypes = SubscriptionManager.DefaultDataTypes();
            _timeKeeper = timeKeeper;
            _marketHoursDatabase = marketHoursDatabase;
            _liveMode = algorithm.LiveMode;

            var liveStart = DateTime.UtcNow;
            // wire ourselves up to receive notifications when universes are added/removed
            algorithm.UniverseManager.CollectionChanged += (sender, args) =>
            {
                switch (args.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                        foreach (var universe in args.NewItems.OfType<Universe>())
                        {
                            var config = universe.Configuration;
                            var start = algorithm.LiveMode ? liveStart : algorithm.UtcTime;

                            var end = algorithm.LiveMode ? Time.EndOfTime
                                : algorithm.EndDate.ConvertToUtc(algorithm.TimeZone);

                            Security security;
                            if (!algorithm.Securities.TryGetValue(config.Symbol, out security))
                            {
                                // create a canonical security object if it doesn't exist
                                security = new Security(
                                    _marketHoursDatabase.GetExchangeHours(config),
                                    config,
                                    algorithm.Portfolio.CashBook[CashBook.AccountCurrency],
                                    SymbolProperties.GetDefault(CashBook.AccountCurrency),
                                    algorithm.Portfolio.CashBook
                                 );
                            }
                            AddSubscription(
                                new SubscriptionRequest(true,
                                    universe,
                                    security,
                                    config,
                                    start,
                                    end));
                        }
                        break;

                    case NotifyCollectionChangedAction.Remove:
                        foreach (var universe in args.OldItems.OfType<Universe>())
                        {
                            RemoveSubscription(universe.Configuration, universe);
                        }
                        break;

                    default:
                        throw new NotImplementedException("The specified action is not implemented: " + args.Action);
                }
            };
        }

        #region IDataFeedSubscriptionManager

        /// <summary>
        /// Gets the data feed subscription collection
        /// </summary>
        public SubscriptionCollection DataFeedSubscriptions { get; } = new SubscriptionCollection();

        /// <summary>
        /// Will remove all current <see cref="Subscription"/>
        /// </summary>
        public void RemoveAllSubscriptions()
        {
            // remove each subscription from our collection
            foreach (var subscription in DataFeedSubscriptions)
            {
                try
                {
                    RemoveSubscription(subscription.Configuration);
                }
                catch (Exception err)
                {
                    Log.Error(err, "DataManager.RemoveAllSubscriptions():" +
                        $"Error removing: {subscription.Configuration}");
                }
            }
        }

        /// <summary>
        /// Adds a new <see cref="Subscription"/> to provide data for the specified security.
        /// </summary>
        /// <param name="request">Defines the <see cref="SubscriptionRequest"/> to be added</param>
        /// <returns>True if the subscription was created and added successfully, false otherwise</returns>
        public bool AddSubscription(SubscriptionRequest request)
        {
            Subscription subscription;
            if (DataFeedSubscriptions.TryGetValue(request.Configuration, out subscription))
            {
                // duplicate subscription request
                return subscription.AddSubscriptionRequest(request);
            }

            subscription = _dataFeed.CreateSubscription(request);

            if (subscription == null)
            {
                Log.Trace($"DataManager.AddSubscription(): Unable to add subscription for: {request.Configuration}");
                // subscription will be null when there's no tradeable dates for the security between the requested times, so
                // don't even try to load the data
                return false;
            }

            LiveDifferentiatedLog($"DataManager.AddSubscription(): Added {request.Configuration}." +
                $" Start: {request.StartTimeUtc}. End: {request.EndTimeUtc}");
            return DataFeedSubscriptions.TryAdd(subscription);
        }

        /// <summary>
        /// Removes the <see cref="Subscription"/>, if it exists
        /// </summary>
        /// <param name="configuration">The <see cref="SubscriptionDataConfig"/> of the subscription to remove</param>
        /// <param name="universe">Universe requesting to remove <see cref="Subscription"/>.
        /// Default value, null, will remove all universes</param>
        /// <returns>True if the subscription was successfully removed, false otherwise</returns>
        public bool RemoveSubscription(SubscriptionDataConfig configuration, Universe universe = null)
        {
            // remove the subscription from our collection, if it exists
            Subscription subscription;

            if (DataFeedSubscriptions.TryGetValue(configuration, out subscription))
            {
                // don't remove universe subscriptions immediately, instead mark them as disposed
                // so we can turn the crank one more time to ensure we emit security changes properly
                if (subscription.IsUniverseSelectionSubscription
                    && subscription.Universes.Single().DisposeRequested)
                {
                    // subscription syncer will dispose the universe AFTER we've run selection a final time
                    // and then will invoke SubscriptionFinished which will remove the universe subscription
                    return false;
                }

                // we remove the subscription when there are no other requests left
                if (subscription.RemoveSubscriptionRequest(universe))
                {
                    if (!DataFeedSubscriptions.TryRemove(configuration, out subscription))
                    {
                        Log.Error($"DataManager.RemoveSubscription(): Unable to remove {configuration}");
                        return false;
                    }

                    _dataFeed.RemoveSubscription(subscription);

                    subscription.Dispose();

                    RemoveSubscriptionDataConfig(configuration);

                    LiveDifferentiatedLog($"DataManager.RemoveSubscription(): Removed {configuration}");
                    return true;
                }
            }
            return false;
        }

        #endregion

        #region IAlgorithmSubscriptionManager

        /// <summary>
        /// Flags the existence of custom data in the subscriptions
        /// </summary>
        public bool HasCustomData { get; set; }

        /// <summary>
        /// Gets all the current data config subscriptions that are being processed for the SubscriptionManager
        /// </summary>
        public IEnumerable<SubscriptionDataConfig> SubscriptionManagerSubscriptions =>
            _subscriptionManagerSubscriptions.Select(x => x.Key);

        /// <summary>
        /// Gets existing or adds new <see cref="SubscriptionDataConfig" />
        /// </summary>
        /// <returns>Returns the SubscriptionDataConfig instance used</returns>
        public SubscriptionDataConfig SubscriptionManagerGetOrAdd(SubscriptionDataConfig newConfig)
        {
            var config = _subscriptionManagerSubscriptions.GetOrAdd(newConfig, newConfig);

            // if the reference is not the same, means it was already there and we did not add anything new
            if (!ReferenceEquals(config, newConfig))
            {
                Log.Debug("DataManager.SubscriptionManagerGetOrAdd(): subscription already added: " + config);
            }
            else
            {
                // count data subscriptions by symbol, ignoring multiple data types.
                // this limit was added due to the limits IB places on number of subscriptions
                var uniqueCount = SubscriptionManagerSubscriptions
                    .Where(x => !x.Symbol.IsCanonical())
                    .DistinctBy(x => x.Symbol.Value)
                    .Count();

                if (uniqueCount > _algorithmSettings.DataSubscriptionLimit)
                {
                    throw new Exception(
                        $"The maximum number of concurrent market data subscriptions was exceeded ({_algorithmSettings.DataSubscriptionLimit})." +
                        "Please reduce the number of symbols requested or increase the limit using Settings.DataSubscriptionLimit.");
                }

                // add the time zone to our time keeper
                _timeKeeper.AddTimeZone(newConfig.ExchangeTimeZone);

                // if is custom data, sets HasCustomData to true
                HasCustomData = HasCustomData || newConfig.IsCustomData;
            }

            return config;
        }

        /// <summary>
        /// Will try to remove a <see cref="SubscriptionDataConfig"/> and update the corresponding
        /// consumers accordingly
        /// </summary>
        /// <param name="config">The configuration to remove</param>
        private void RemoveSubscriptionDataConfig(SubscriptionDataConfig config)
        {
            if (_subscriptionManagerSubscriptions.TryRemove(config, out config))
            {
                if (HasCustomData && config.IsCustomData)
                {
                    HasCustomData = _subscriptionManagerSubscriptions.Any(x => x.Key.IsCustomData);
                }
            }
        }

        /// <summary>
        /// Returns the amount of data config subscriptions processed for the SubscriptionManager
        /// </summary>
        public int SubscriptionManagerCount()
        {
            return _subscriptionManagerSubscriptions.Skip(0).Count();
        }

        #region ISubscriptionDataConfigService

        /// <summary>
        /// The different <see cref="TickType" /> each <see cref="SecurityType" /> supports
        /// </summary>
        public Dictionary<SecurityType, List<TickType>> AvailableDataTypes { get; }

        /// <summary>
        /// Creates and adds a list of <see cref="SubscriptionDataConfig" /> for a given symbol and configuration.
        /// Can optionally pass in desired subscription data type to use.
        /// If the config already existed will return existing instance instead
        /// </summary>
        public SubscriptionDataConfig Add(
            Type dataType,
            Symbol symbol,
            Resolution resolution,
            bool fillForward = true,
            bool extendedMarketHours = false,
            bool isFilteredSubscription = true,
            bool isInternalFeed = false,
            bool isCustomData = false
            )
        {
            return Add(symbol, resolution, fillForward, extendedMarketHours, isFilteredSubscription, isInternalFeed, isCustomData,
                new List<Tuple<Type, TickType>> { new Tuple<Type, TickType>(dataType, LeanData.GetCommonTickTypeForCommonDataTypes(dataType, symbol.SecurityType))})
                .First();
        }

        /// <summary>
        /// Creates and adds a list of <see cref="SubscriptionDataConfig" /> for a given symbol and configuration.
        /// Can optionally pass in desired subscription data types to use.
        ///  If the config already existed will return existing instance instead
        /// </summary>
        public List<SubscriptionDataConfig> Add(
            Symbol symbol,
            Resolution resolution,
            bool fillForward,
            bool extendedMarketHours,
            bool isFilteredSubscription = true,
            bool isInternalFeed = false,
            bool isCustomData = false,
            List<Tuple<Type, TickType>> subscriptionDataTypes = null
            )
        {
            var dataTypes = subscriptionDataTypes ??
                LookupSubscriptionConfigDataTypes(symbol.SecurityType, resolution, symbol.IsCanonical());

            var marketHoursDbEntry = _marketHoursDatabase.GetEntry(symbol.ID.Market, symbol, symbol.ID.SecurityType);
            var exchangeHours = marketHoursDbEntry.ExchangeHours;
            var dataNormalizationMode = symbol.ID.SecurityType == SecurityType.Option || symbol.ID.SecurityType == SecurityType.Future
                ? DataNormalizationMode.Raw
                : DataNormalizationMode.Adjusted;

            if (marketHoursDbEntry.DataTimeZone == null)
            {
                throw new ArgumentNullException(nameof(marketHoursDbEntry.DataTimeZone),
                    "DataTimeZone is a required parameter for new subscriptions. Set to the time zone the raw data is time stamped in.");
            }

            if (exchangeHours.TimeZone == null)
            {
                throw new ArgumentNullException(nameof(exchangeHours.TimeZone),
                    "ExchangeTimeZone is a required parameter for new subscriptions. Set to the time zone the security exchange resides in.");
            }

            if (!dataTypes.Any())
            {
                throw new ArgumentNullException(nameof(dataTypes), "At least one type needed to create new subscriptions");
            }

            var result = (from subscriptionDataType in dataTypes
                let dataType = subscriptionDataType.Item1
                let tickType = subscriptionDataType.Item2
                select new SubscriptionDataConfig(
                    dataType,
                    symbol,
                    resolution,
                    marketHoursDbEntry.DataTimeZone,
                    exchangeHours.TimeZone,
                    fillForward,
                    extendedMarketHours,
                    isInternalFeed,
                    isCustomData,
                    isFilteredSubscription: isFilteredSubscription,
                    tickType: tickType,
                    dataNormalizationMode: dataNormalizationMode)).ToList();

            for (int i = 0; i < result.Count; i++)
            {
                result[i] = SubscriptionManagerGetOrAdd(result[i]);
            }
            return result;
        }

        /// <summary>
        /// Get the data feed types for a given <see cref="SecurityType" /> <see cref="Resolution" />
        /// </summary>
        /// <param name="symbolSecurityType">The <see cref="SecurityType" /> used to determine the types</param>
        /// <param name="resolution">The resolution of the data requested</param>
        /// <param name="isCanonical">Indicates whether the security is Canonical (future and options)</param>
        /// <returns>Types that should be added to the <see cref="SubscriptionDataConfig" /></returns>
        public List<Tuple<Type, TickType>> LookupSubscriptionConfigDataTypes(
            SecurityType symbolSecurityType,
            Resolution resolution,
            bool isCanonical
            )
        {
            if (isCanonical)
            {
                return new List<Tuple<Type, TickType>> { new Tuple<Type, TickType>(typeof(ZipEntryName), TickType.Quote) };
            }

            return AvailableDataTypes[symbolSecurityType]
                .Select(tickType => new Tuple<Type, TickType>(LeanData.GetDataType(resolution, tickType), tickType)).ToList();
        }

        /// <summary>
        /// Gets a list of all registered <see cref="SubscriptionDataConfig"/> for a given <see cref="Symbol"/>
        /// </summary>
        public List<SubscriptionDataConfig> GetSubscriptionDataConfigs(Symbol symbol)
        {
            return SubscriptionManagerSubscriptions.Where(x => x.Symbol == symbol).ToList();
        }

        #endregion

        #endregion

        #region IDataManager

        /// <summary>
        /// Get the universe selection instance
        /// </summary>
        public UniverseSelection UniverseSelection { get; }

        #endregion

        private void LiveDifferentiatedLog(string message)
        {
            if (_liveMode)
            {
                Log.Trace(message);
            }
            else
            {
                Log.Debug(message);
            }
        }
    }
}

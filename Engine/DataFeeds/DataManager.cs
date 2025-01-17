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
using System.Collections.Specialized;
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Data.Auxiliary;
using QuantConnect.Data.Market;
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
        private readonly IDataFeed _dataFeed;
        private readonly MarketHoursDatabase _marketHoursDatabase;
        private readonly ITimeKeeper _timeKeeper;
        private readonly bool _liveMode;
        private bool _sentUniverseScheduleWarning;
        private readonly IRegisteredSecurityDataTypesProvider _registeredTypesProvider;
        private readonly IDataPermissionManager _dataPermissionManager;
        private List<SubscriptionDataConfig> _subscriptionDataConfigsEnumerator;

        /// There is no ConcurrentHashSet collection in .NET,
        /// so we use ConcurrentDictionary with byte value to minimize memory usage
        private readonly Dictionary<SubscriptionDataConfig, SubscriptionDataConfig> _subscriptionManagerSubscriptions = new();

        /// <summary>
        /// Event fired when a new subscription is added
        /// </summary>
        public event EventHandler<Subscription> SubscriptionAdded;

        /// <summary>
        /// Event fired when an existing subscription is removed
        /// </summary>
        public event EventHandler<Subscription> SubscriptionRemoved;

        /// <summary>
        /// Creates a new instance of the DataManager
        /// </summary>
        public DataManager(
            IDataFeed dataFeed,
            UniverseSelection universeSelection,
            IAlgorithm algorithm,
            ITimeKeeper timeKeeper,
            MarketHoursDatabase marketHoursDatabase,
            bool liveMode,
            IRegisteredSecurityDataTypesProvider registeredTypesProvider,
            IDataPermissionManager dataPermissionManager)
        {
            _dataFeed = dataFeed;
            UniverseSelection = universeSelection;
            UniverseSelection.SetDataManager(this);
            AvailableDataTypes = SubscriptionManager.DefaultDataTypes();
            _timeKeeper = timeKeeper;
            _marketHoursDatabase = marketHoursDatabase;
            _liveMode = liveMode;
            _registeredTypesProvider = registeredTypesProvider;
            _dataPermissionManager = dataPermissionManager;

            // wire ourselves up to receive notifications when universes are added/removed
            algorithm.UniverseManager.CollectionChanged += (sender, args) =>
            {
                switch (args.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                        foreach (var universe in args.NewItems.OfType<Universe>())
                        {
                            var config = universe.Configuration;
                            var start = algorithm.UtcTime;

                            var end = algorithm.LiveMode ? Time.EndOfTime
                                : algorithm.EndDate.ConvertToUtc(algorithm.TimeZone);

                            Security security;
                            if (!algorithm.Securities.TryGetValue(config.Symbol, out security))
                            {
                                // create a canonical security object if it doesn't exist
                                security = new Security(
                                    _marketHoursDatabase.GetExchangeHours(config),
                                    config,
                                    algorithm.Portfolio.CashBook[algorithm.AccountCurrency],
                                    SymbolProperties.GetDefault(algorithm.AccountCurrency),
                                    algorithm.Portfolio.CashBook,
                                    RegisteredSecurityDataTypesProvider.Null,
                                    new SecurityCache()
                                 );
                            }

                            // Let's adjust the start time to the previous tradable date
                            // so universe selection always happens right away at the start of the algorithm.
                            var universeType = universe.GetType();
                            if (
                                // We exclude the UserDefinedUniverse because their selection already happens at the algorithm start time.
                                // For instance, ETFs universe selection depends its first trigger time to be before the equity universe
                                // (the UserDefinedUniverse), because the ETFs are EndTime-indexed and that would make their first selection
                                // time to be before the algorithm start time, with the EndTime being the algorithms's start date,
                                // and both the Equity and the ETFs constituents first selection to happen together.
                                !universeType.IsAssignableTo(typeof(UserDefinedUniverse)) &&
                                // We exclude the ScheduledUniverse because it's already scheduled to run at a specific time.
                                // Adjusting the start time would cause the first selection trigger time to be before the algorithm start time,
                                // making the selection to be triggered at the first algorithm time, which would be the exact StartDate.
                                universeType != typeof(ScheduledUniverse))
                            {
                                const int maximumLookback = 60;
                                var loopCount = 0;
                                var startLocalTime = start.ConvertFromUtc(security.Exchange.TimeZone);
                                if (universe.UniverseSettings.Schedule.Initialized)
                                {
                                    do
                                    {
                                        // determine if there's a scheduled selection time at the current start local time date, note that next
                                        // we get the previous day of the first scheduled date we find, so we are sure the data is available to trigger selection
                                        if (universe.UniverseSettings.Schedule.Get(startLocalTime.Date, startLocalTime.Date).Any())
                                        {
                                            break;
                                        }
                                        startLocalTime = startLocalTime.AddDays(-1);
                                        if (++loopCount >= maximumLookback)
                                        {
                                            // fallback to the original, we found none
                                            startLocalTime = algorithm.UtcTime.ConvertFromUtc(security.Exchange.TimeZone);
                                            if (!_sentUniverseScheduleWarning)
                                            {
                                                // just in case
                                                _sentUniverseScheduleWarning = true;
                                                algorithm.Debug($"Warning: Found no valid start time for scheduled universe, will use default");
                                            }
                                        }
                                    } while (loopCount < maximumLookback);
                                }

                                startLocalTime = Time.GetStartTimeForTradeBars(security.Exchange.Hours, startLocalTime,
                                    // disable universe selection on extended market hours, for example futures/index options have a sunday pre market we are not interested on
                                    Time.OneDay, 1, extendedMarketHours: false, config.DataTimeZone,
                                    LeanData.UseDailyStrictEndTimes(algorithm.Settings, config.Type, security.Symbol, Time.OneDay, security.Exchange.Hours));
                                start = startLocalTime.ConvertToUtc(security.Exchange.TimeZone);
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
                            // removing the subscription will be handled by the SubscriptionSynchronizer
                            // in the next loop as well as executing a UniverseSelection one last time.
                            if (!universe.DisposeRequested)
                            {
                                universe.Dispose();
                            }
                        }
                        break;

                    default:
                        throw new NotImplementedException("The specified action is not implemented: " + args.Action);
                }
            };

            DataFeedSubscriptions = new SubscriptionCollection();
            if (!_liveMode)
            {
                DataFeedSubscriptions.FillForwardResolutionChanged += (object sender, FillForwardResolutionChangedEvent changedEvent) =>
                {
                    var requests = DataFeedSubscriptions
                        // we don't fill forward tick resolution so we don't need to touch their subscriptions
                        .Where(subscription => subscription.Configuration.FillDataForward && subscription.Configuration.Resolution != Resolution.Tick)
                        .SelectMany(subscription => subscription.SubscriptionRequests)
                        .ToList();

                    if(requests.Count > 0)
                    {
                        Log.Trace($"DataManager(): Fill forward resolution has changed from {changedEvent.Old} to {changedEvent.New} at utc: {algorithm.UtcTime}. " +
                            $"Restarting {requests.Count} subscriptions...");

                        // disable reentry while we remove and re add
                        DataFeedSubscriptions.FreezeFillForwardResolution(true);

                        // remove
                        foreach (var request in requests)
                        {
                            // force because we want them actually removed even if still a member of the universe, because the FF res changed
                            // which means we will drop any data points that could be in the next potential slice being created
                            RemoveSubscriptionInternal(request.Configuration, universe: request.Universe, forceSubscriptionRemoval: true);
                        }

                        // re add
                        foreach (var request in requests)
                        {
                            // If it is an add we will set time 1 tick ahead to properly sync data
                            // with next timeslice, avoid emitting now twice.
                            // We do the same in the 'TimeTriggeredUniverseSubscriptionEnumeratorFactory' when handling changes
                            var startUtc = algorithm.UtcTime;
                            // If the algorithm is not initialized (locked) the request start time can be even before the algorithm start time,
                            // like in the case of universe requests that are scheduled to run at a specific time in the past for immediate selection.
                            if (!algorithm.GetLocked() && request.StartTimeUtc < startUtc)
                            {
                                startUtc = request.StartTimeUtc;
                            }
                            AddSubscription(new SubscriptionRequest(request,
                                startTimeUtc: startUtc.AddTicks(1),
                                configuration: new SubscriptionDataConfig(request.Configuration)));
                        }

                        DataFeedSubscriptions.FreezeFillForwardResolution(false);
                    }
                };
            }
        }

        #region IDataFeedSubscriptionManager

        /// <summary>
        /// Gets the data feed subscription collection
        /// </summary>
        public SubscriptionCollection DataFeedSubscriptions { get; }

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
            lock (_subscriptionManagerSubscriptions)
            {
                // guarantee the configuration is present in our config collection
                // this is related to GH issue 3877: where we added a configuration which we also removed
                if(_subscriptionManagerSubscriptions.TryAdd(request.Configuration, request.Configuration))
                {
                    _subscriptionDataConfigsEnumerator = null;
                }
            }

            Subscription subscription;
            if (DataFeedSubscriptions.TryGetValue(request.Configuration, out subscription))
            {
                // duplicate subscription request
                subscription.AddSubscriptionRequest(request);
                // only result true if the existing subscription is internal, we actually added something from the users perspective
                return subscription.Configuration.IsInternalFeed;
            }

            if (request.Configuration.DataNormalizationMode == DataNormalizationMode.ScaledRaw)
            {
                throw new InvalidOperationException($"{DataNormalizationMode.ScaledRaw} normalization mode only intended for history requests.");
            }

            // before adding the configuration to the data feed let's assert it's valid
            _dataPermissionManager.AssertConfiguration(request.Configuration, request.StartTimeLocal, request.EndTimeLocal);

            subscription = _dataFeed.CreateSubscription(request);

            if (subscription == null)
            {
                Log.Trace($"DataManager.AddSubscription(): Unable to add subscription for: {request.Configuration}");
                // subscription will be null when there's no tradeable dates for the security between the requested times, so
                // don't even try to load the data
                return false;
            }

            if (_liveMode)
            {
                OnSubscriptionAdded(subscription);
                Log.Trace($"DataManager.AddSubscription(): Added {request.Configuration}." +
                    $" Start: {request.StartTimeUtc}. End: {request.EndTimeUtc}");
            }
            else if(Log.DebuggingEnabled)
            {
                // for performance lets not create the message string if debugging is not enabled
                // this can be executed many times and its in the algorithm thread
                Log.Debug($"DataManager.AddSubscription(): Added {request.Configuration}." +
                    $" Start: {request.StartTimeUtc}. End: {request.EndTimeUtc}");
            }

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
            return RemoveSubscriptionInternal(configuration, universe, forceSubscriptionRemoval: false);
        }

        /// <summary>
        /// Removes the <see cref="Subscription"/>, if it exists
        /// </summary>
        /// <param name="configuration">The <see cref="SubscriptionDataConfig"/> of the subscription to remove</param>
        /// <param name="universe">Universe requesting to remove <see cref="Subscription"/>.
        /// Default value, null, will remove all universes</param>
        /// <param name="forceSubscriptionRemoval">We force the subscription removal by marking it as removed from universe, so that all it's data is dropped</param>
        /// <returns>True if the subscription was successfully removed, false otherwise</returns>
        private bool RemoveSubscriptionInternal(SubscriptionDataConfig configuration, Universe universe, bool forceSubscriptionRemoval)
        {
            // remove the subscription from our collection, if it exists
            Subscription subscription;

            if (DataFeedSubscriptions.TryGetValue(configuration, out subscription))
            {
                // we remove the subscription when there are no other requests left
                if (subscription.RemoveSubscriptionRequest(universe))
                {
                    if (!DataFeedSubscriptions.TryRemove(configuration, out subscription))
                    {
                        Log.Error($"DataManager.RemoveSubscription(): Unable to remove {configuration}");
                        return false;
                    }

                    _dataFeed.RemoveSubscription(subscription);

                    if (_liveMode)
                    {
                        OnSubscriptionRemoved(subscription);
                    }

                    subscription.Dispose();

                    RemoveSubscriptionDataConfig(subscription);

                    if (forceSubscriptionRemoval)
                    {
                        subscription.MarkAsRemovedFromUniverse();
                    }

                    if (_liveMode)
                    {
                        Log.Trace($"DataManager.RemoveSubscription(): Removed {configuration}");
                    }
                    else if(Log.DebuggingEnabled)
                    {
                        // for performance lets not create the message string if debugging is not enabled
                        // this can be executed many times and its in the algorithm thread
                        Log.Debug($"DataManager.RemoveSubscription(): Removed {configuration}");
                    }
                    return true;
                }
            }
            else if (universe != null)
            {
                // a universe requested removal of a subscription which wasn't present anymore, this can happen when a subscription ends
                // it will get removed from the data feed subscription list, but the configuration will remain until the universe removes it
                // why? the effect I found is that the fill models are using these subscriptions to determine which data they could use
                lock (_subscriptionManagerSubscriptions)
                {
                    if (_subscriptionManagerSubscriptions.Remove(configuration))
                    {
                        _subscriptionDataConfigsEnumerator = null;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Event invocator for the <see cref="SubscriptionAdded"/> event
        /// </summary>
        /// <param name="subscription">The added subscription</param>
        private void OnSubscriptionAdded(Subscription subscription)
        {
            SubscriptionAdded?.Invoke(this, subscription);
        }

        /// <summary>
        /// Event invocator for the <see cref="SubscriptionRemoved"/> event
        /// </summary>
        /// <param name="subscription">The removed subscription</param>
        private void OnSubscriptionRemoved(Subscription subscription)
        {
            SubscriptionRemoved?.Invoke(this, subscription);
        }

        #endregion

        #region IAlgorithmSubscriptionManager

        /// <summary>
        /// Gets all the current data config subscriptions that are being processed for the SubscriptionManager
        /// </summary>
        public IEnumerable<SubscriptionDataConfig> SubscriptionManagerSubscriptions
        {
            get
            {
                lock (_subscriptionManagerSubscriptions)
                {
                    if(_subscriptionDataConfigsEnumerator == null)
                    {
                        _subscriptionDataConfigsEnumerator = _subscriptionManagerSubscriptions.Values.ToList();
                    }
                    return _subscriptionDataConfigsEnumerator;
                }
            }
        }

        /// <summary>
        /// Gets existing or adds new <see cref="SubscriptionDataConfig" />
        /// </summary>
        /// <returns>Returns the SubscriptionDataConfig instance used</returns>
        public SubscriptionDataConfig SubscriptionManagerGetOrAdd(SubscriptionDataConfig newConfig)
        {
            SubscriptionDataConfig config;
            lock (_subscriptionManagerSubscriptions)
            {
                if (!_subscriptionManagerSubscriptions.TryGetValue(newConfig, out config))
                {
                    _subscriptionManagerSubscriptions[newConfig] = config = newConfig;
                    _subscriptionDataConfigsEnumerator = null;
                }
            }

            // if the reference is not the same, means it was already there and we did not add anything new
            if (!ReferenceEquals(config, newConfig))
            {
                // for performance lets not create the message string if debugging is not enabled
                // this can be executed many times and its in the algorithm thread
                if (Log.DebuggingEnabled)
                {
                    Log.Debug("DataManager.SubscriptionManagerGetOrAdd(): subscription already added: " + config);
                }
            }
            else
            {
                // add the time zone to our time keeper
                _timeKeeper.AddTimeZone(newConfig.ExchangeTimeZone);
            }

            return config;
        }

        /// <summary>
        /// Will try to remove a <see cref="SubscriptionDataConfig"/> and update the corresponding
        /// consumers accordingly
        /// </summary>
        /// <param name="subscription">The <see cref="Subscription"/> owning the configuration to remove</param>
        private void RemoveSubscriptionDataConfig(Subscription subscription)
        {
            // the subscription could of ended but might still be part of the universe
            if (subscription.RemovedFromUniverse.Value)
            {
                lock (_subscriptionManagerSubscriptions)
                {
                    if (_subscriptionManagerSubscriptions.Remove(subscription.Configuration))
                    {
                        _subscriptionDataConfigsEnumerator = null;
                    }
                }
            }
        }

        /// <summary>
        /// Returns the amount of data config subscriptions processed for the SubscriptionManager
        /// </summary>
        public int SubscriptionManagerCount()
        {
            lock (_subscriptionManagerSubscriptions)
            {
                return _subscriptionManagerSubscriptions.Count;
            }
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
            Resolution? resolution = null,
            bool fillForward = true,
            bool extendedMarketHours = false,
            bool isFilteredSubscription = true,
            bool isInternalFeed = false,
            bool isCustomData = false,
            DataNormalizationMode dataNormalizationMode = DataNormalizationMode.Adjusted,
            DataMappingMode dataMappingMode = DataMappingMode.OpenInterest,
            uint contractDepthOffset = 0
            )
        {
            return Add(symbol, resolution, fillForward, extendedMarketHours, isFilteredSubscription, isInternalFeed, isCustomData,
                new List<Tuple<Type, TickType>> { new Tuple<Type, TickType>(dataType, LeanData.GetCommonTickTypeForCommonDataTypes(dataType, symbol.SecurityType))},
                dataNormalizationMode, dataMappingMode, contractDepthOffset)
                .First();
        }

        /// <summary>
        /// Creates and adds a list of <see cref="SubscriptionDataConfig" /> for a given symbol and configuration.
        /// Can optionally pass in desired subscription data types to use.
        ///  If the config already existed will return existing instance instead
        /// </summary>
        public List<SubscriptionDataConfig> Add(
            Symbol symbol,
            Resolution? resolution = null,
            bool fillForward = true,
            bool extendedMarketHours = false,
            bool isFilteredSubscription = true,
            bool isInternalFeed = false,
            bool isCustomData = false,
            List<Tuple<Type, TickType>> subscriptionDataTypes = null,
            DataNormalizationMode dataNormalizationMode = DataNormalizationMode.Adjusted,
            DataMappingMode dataMappingMode = DataMappingMode.OpenInterest,
            uint contractDepthOffset = 0
            )
        {
            var dataTypes = subscriptionDataTypes;
            if(dataTypes == null)
            {
                if (symbol.SecurityType == SecurityType.Base && SecurityIdentifier.TryGetCustomDataTypeInstance(symbol.ID.Symbol, out var type))
                {
                    // we've detected custom data request if we find a type let's use it
                    dataTypes = new List<Tuple<Type, TickType>> { new Tuple<Type, TickType>(type, TickType.Trade) };
                }
                else
                {
                    dataTypes = LookupSubscriptionConfigDataTypes(symbol.SecurityType, resolution ?? Resolution.Minute, symbol.IsCanonical());
                }
            }

            if (!dataTypes.Any())
            {
                throw new ArgumentNullException(nameof(dataTypes), "At least one type needed to create new subscriptions");
            }

            var resolutionWasProvided = resolution.HasValue;
            foreach (var typeTuple in dataTypes)
            {
                var baseInstance = typeTuple.Item1.GetBaseDataInstance();
                baseInstance.Symbol = symbol;
                if (!resolutionWasProvided)
                {
                    var defaultResolution = baseInstance.DefaultResolution();
                    if (resolution.HasValue && resolution != defaultResolution)
                    {
                        // we are here because there are multiple 'dataTypes'.
                        // if we get different default resolutions lets throw, this shouldn't happen
                        throw new InvalidOperationException(
                            $"Different data types ({string.Join(",", dataTypes.Select(tuple => tuple.Item1))})" +
                            $" provided different default resolutions {defaultResolution} and {resolution}, this is an unexpected invalid operation.");
                    }
                    resolution = defaultResolution;
                }
                else
                {
                    // only assert resolution in backtesting, live can use other data source
                    // for example daily data for options
                    if (!_liveMode)
                    {
                        var supportedResolutions = baseInstance.SupportedResolutions();
                        if (supportedResolutions.Contains(resolution.Value))
                        {
                            continue;
                        }

                        throw new ArgumentException($"Sorry {resolution.ToStringInvariant()} is not a supported resolution for {typeTuple.Item1.Name}" +
                                                    $" and SecurityType.{symbol.SecurityType.ToStringInvariant()}." +
                                                    $" Please change your AddData to use one of the supported resolutions ({string.Join(",", supportedResolutions)}).");
                    }
                }
            }
            var marketHoursDbEntry = _marketHoursDatabase.GetEntry(symbol, dataTypes.Select(tuple => tuple.Item1));

            var exchangeHours = marketHoursDbEntry.ExchangeHours;
            if (symbol.ID.SecurityType.IsOption() ||
                symbol.ID.SecurityType == SecurityType.Index)
            {
                dataNormalizationMode = DataNormalizationMode.Raw;
            }

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

            var result = (from subscriptionDataType in dataTypes
                let dataType = subscriptionDataType.Item1
                let tickType = subscriptionDataType.Item2
                select new SubscriptionDataConfig(
                    dataType,
                    symbol,
                    resolution.Value,
                    marketHoursDbEntry.DataTimeZone,
                    exchangeHours.TimeZone,
                    fillForward,
                    extendedMarketHours,
                    // if the subscription data types were not provided and the tick type is OpenInterest we make it internal
                    subscriptionDataTypes == null && tickType == TickType.OpenInterest || isInternalFeed,
                    isCustomData,
                    isFilteredSubscription: isFilteredSubscription,
                    tickType: tickType,
                    dataNormalizationMode: dataNormalizationMode,
                    dataMappingMode: dataMappingMode,
                    contractDepthOffset: contractDepthOffset)).ToList();

            for (int i = 0; i < result.Count; i++)
            {
                result[i] = SubscriptionManagerGetOrAdd(result[i]);

                // track all registered data types
                _registeredTypesProvider.RegisterType(result[i].Type);
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
        /// <remarks>TODO: data type additions are very related to ticktype and should be more generic/independent of each other</remarks>
        public List<Tuple<Type, TickType>> LookupSubscriptionConfigDataTypes(
            SecurityType symbolSecurityType,
            Resolution resolution,
            bool isCanonical
            )
        {
            if (isCanonical)
            {
                if (symbolSecurityType.IsOption())
                {
                    return new List<Tuple<Type, TickType>> { new Tuple<Type, TickType>(typeof(OptionUniverse), TickType.Quote) };
                }

                return new List<Tuple<Type, TickType>> { new Tuple<Type, TickType>(typeof(FutureUniverse), TickType.Quote) };
            }

            IEnumerable<TickType> availableDataType = AvailableDataTypes[symbolSecurityType]
                // Equities will only look for trades in case of low resolutions.
                .Where(tickType => LeanData.IsValidConfiguration(symbolSecurityType, resolution, tickType));

            var result = availableDataType
                .Select(tickType => new Tuple<Type, TickType>(LeanData.GetDataType(resolution, tickType), tickType)).ToList();

            if(symbolSecurityType == SecurityType.CryptoFuture)
            {
                result.Add(new Tuple<Type, TickType>(typeof(MarginInterestRate), TickType.Quote));
            }
            return result;
        }

        /// <summary>
        /// Gets a list of all registered <see cref="SubscriptionDataConfig"/> for a given <see cref="Symbol"/>
        /// </summary>
        /// <remarks>Will not return internal subscriptions by default</remarks>
        public List<SubscriptionDataConfig> GetSubscriptionDataConfigs(Symbol symbol = null, bool includeInternalConfigs = false)
        {
            lock (_subscriptionManagerSubscriptions)
            {
                return _subscriptionManagerSubscriptions.Keys
                    .Where(config => (includeInternalConfigs || !config.IsInternalFeed) && (symbol == null || config.Symbol.ID == symbol.ID))
                    .OrderBy(config => config.IsInternalFeed)
                    .ToList();
            }
        }

        #endregion

        #endregion

        #region IDataManager

        /// <summary>
        /// Get the universe selection instance
        /// </summary>
        public UniverseSelection UniverseSelection { get; }

        #endregion
    }
}

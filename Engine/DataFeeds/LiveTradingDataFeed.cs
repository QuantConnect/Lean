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
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Util;
using QuantConnect.Logging;
using QuantConnect.Packets;
using QuantConnect.Interfaces;
using QuantConnect.Securities;
using System.Collections.Generic;
using QuantConnect.Configuration;
using QuantConnect.Data.Auxiliary;
using QuantConnect.Data.Custom.Tiingo;
using QuantConnect.Lean.Engine.Results;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Lean.Engine.DataFeeds.Enumerators;
using QuantConnect.Lean.Engine.DataFeeds.Enumerators.Factories;
using QuantConnect.Data.Fundamental;

namespace QuantConnect.Lean.Engine.DataFeeds
{
    /// <summary>
    /// Provides an implementation of <see cref="IDataFeed"/> that is designed to deal with
    /// live, remote data sources
    /// </summary>
    public class LiveTradingDataFeed : FileSystemDataFeed
    {
        private static readonly int MaximumWarmupHistoryDaysLookBack = Config.GetInt("maximum-warmup-history-days-look-back", 5);

        private LiveNodePacket _job;

        // used to get current time
        private ITimeProvider _timeProvider;
        private IAlgorithm _algorithm;
        private ITimeProvider _frontierTimeProvider;
        private IDataProvider _dataProvider;
        private IMapFileProvider _mapFileProvider;
        private IDataQueueHandler _dataQueueHandler;
        private BaseDataExchange _customExchange;
        private SubscriptionCollection _subscriptions;
        private IFactorFileProvider _factorFileProvider;
        private IDataChannelProvider _channelProvider;
        // in live trading we delay scheduled universe selection between 11 & 12 hours after midnight UTC so that we allow new selection data to be piped in
        // NY goes from -4/-5 UTC time, so:
        // 11 UTC - 4 => 7am NY
        // 12 UTC - 4 => 8am NY
        private readonly TimeSpan _scheduledUniverseUtcTimeShift = TimeSpan.FromMinutes(11 * 60 + DateTime.UtcNow.Second);
        private readonly HashSet<string> _unsupportedConfigurations = new();

        /// <summary>
        /// Public flag indicator that the thread is still busy.
        /// </summary>
        public bool IsActive
        {
            get; private set;
        }

        /// <summary>
        /// Initializes the data feed for the specified job and algorithm
        /// </summary>
        public override void Initialize(IAlgorithm algorithm,
            AlgorithmNodePacket job,
            IResultHandler resultHandler,
            IMapFileProvider mapFileProvider,
            IFactorFileProvider factorFileProvider,
            IDataProvider dataProvider,
            IDataFeedSubscriptionManager subscriptionManager,
            IDataFeedTimeProvider dataFeedTimeProvider,
            IDataChannelProvider dataChannelProvider)
        {
            if (!(job is LiveNodePacket))
            {
                throw new ArgumentException("The LiveTradingDataFeed requires a LiveNodePacket.");
            }

            _algorithm = algorithm;
            _job = (LiveNodePacket)job;
            _timeProvider = dataFeedTimeProvider.TimeProvider;
            _dataProvider = dataProvider;
            _mapFileProvider = mapFileProvider;
            _factorFileProvider = factorFileProvider;
            _channelProvider = dataChannelProvider;
            _frontierTimeProvider = dataFeedTimeProvider.FrontierTimeProvider;
            _customExchange = GetBaseDataExchange();
            _subscriptions = subscriptionManager.DataFeedSubscriptions;

            _dataQueueHandler = GetDataQueueHandler();
            _dataQueueHandler?.SetJob(_job);

            // run the custom data exchange
            _customExchange.Start();

            IsActive = true;

            base.Initialize(algorithm, job, resultHandler, mapFileProvider, factorFileProvider, dataProvider, subscriptionManager, dataFeedTimeProvider, dataChannelProvider);
        }

        /// <summary>
        /// Creates a new subscription to provide data for the specified security.
        /// </summary>
        /// <param name="request">Defines the subscription to be added, including start/end times the universe and security</param>
        /// <returns>The created <see cref="Subscription"/> if successful, null otherwise</returns>
        public override Subscription CreateSubscription(SubscriptionRequest request)
        {
            Subscription subscription = null;
            try
            {
                // create and add the subscription to our collection
                subscription = request.IsUniverseSubscription
                    ? CreateUniverseSubscription(request)
                    : CreateDataSubscription(request);
            }
            catch (Exception err)
            {
                Log.Error(err, $"CreateSubscription(): Failed configuration: '{request.Configuration}'");
                // kill the algorithm, this shouldn't happen
                _algorithm.SetRuntimeError(err, $"Failed to subscribe to {request.Configuration.Symbol}");
            }

            return subscription;
        }

        /// <summary>
        /// Removes the subscription from the data feed, if it exists
        /// </summary>
        /// <param name="subscription">The subscription to remove</param>
        public override void RemoveSubscription(Subscription subscription)
        {
            var symbol = subscription.Configuration.Symbol;

            // remove the subscriptions
            if (!_channelProvider.ShouldStreamSubscription(subscription.Configuration))
            {
                _customExchange.RemoveEnumerator(symbol);
            }
            else
            {
                _dataQueueHandler.UnsubscribeWithMapping(subscription.Configuration);
            }
        }

        /// <summary>
        /// External controller calls to signal a terminate of the thread.
        /// </summary>
        public override void Exit()
        {
            if (IsActive)
            {
                IsActive = false;
                Log.Trace("LiveTradingDataFeed.Exit(): Start. Setting cancellation token...");
                if (_dataQueueHandler is DataQueueHandlerManager manager)
                {
                    manager.UnsupportedConfiguration -= HandleUnsupportedConfigurationEvent;
                }
                _customExchange?.Stop();
                Log.Trace("LiveTradingDataFeed.Exit(): Exit Finished.");

                base.Exit();
            }
        }

        /// <summary>
        /// Gets the <see cref="IDataQueueHandler"/> to use by default <see cref="DataQueueHandlerManager"/>
        /// </summary>
        /// <remarks>Useful for testing</remarks>
        /// <returns>The loaded <see cref="IDataQueueHandler"/></returns>
        protected virtual IDataQueueHandler GetDataQueueHandler()
        {
            var result = new DataQueueHandlerManager(_algorithm.Settings);
            result.UnsupportedConfiguration += HandleUnsupportedConfigurationEvent;
            return result;
        }

        /// <summary>
        /// Gets the <see cref="BaseDataExchange"/> to use
        /// </summary>
        /// <remarks>Useful for testing</remarks>
        protected virtual BaseDataExchange GetBaseDataExchange()
        {
            return new BaseDataExchange("CustomDataExchange") { SleepInterval = 100 };
        }

        /// <summary>
        /// Creates a new subscription for the specified security
        /// </summary>
        /// <param name="request">The subscription request</param>
        /// <returns>A new subscription instance of the specified security</returns>
        private Subscription CreateDataSubscription(SubscriptionRequest request)
        {
            Subscription subscription = null;

            var localEndTime = request.EndTimeUtc.ConvertFromUtc(request.Security.Exchange.TimeZone);
            var timeZoneOffsetProvider = new TimeZoneOffsetProvider(request.Configuration.ExchangeTimeZone, request.StartTimeUtc, request.EndTimeUtc);

            IEnumerator<BaseData> enumerator = null;
            if (!_channelProvider.ShouldStreamSubscription(request.Configuration))
            {
                if (!Tiingo.IsAuthCodeSet)
                {
                    // we're not using the SubscriptionDataReader, so be sure to set the auth token here
                    Tiingo.SetAuthCode(Config.Get("tiingo-auth-token"));
                }

                var factory = new LiveCustomDataSubscriptionEnumeratorFactory(_timeProvider, _algorithm.ObjectStore);
                var enumeratorStack = factory.CreateEnumerator(request, _dataProvider);

                var enqueable = new EnqueueableEnumerator<BaseData>();
                _customExchange.AddEnumerator(request.Configuration.Symbol, enumeratorStack, handleData: data =>
                {
                    enqueable.Enqueue(data);

                    subscription?.OnNewDataAvailable();
                });

                enumerator = enqueable;
            }
            else
            {
                var auxEnumerators = new List<IEnumerator<BaseData>>();

                if (LiveAuxiliaryDataEnumerator.TryCreate(request.Configuration, _timeProvider, request.Security.Cache, _mapFileProvider,
                    _factorFileProvider, request.StartTimeLocal, out var auxDataEnumator))
                {
                    auxEnumerators.Add(auxDataEnumator);
                }

                EventHandler handler = (_, _) => subscription?.OnNewDataAvailable();
                enumerator = Subscribe(request.Configuration, handler, IsExpired);

                if (auxEnumerators.Count > 0)
                {
                    enumerator = new LiveAuxiliaryDataSynchronizingEnumerator(_timeProvider, request.Configuration.ExchangeTimeZone, enumerator, auxEnumerators);
                }
            }

            // scale prices before 'SubscriptionFilterEnumerator' since it updates securities realtime price
            // and before fill forwarding so we don't happen to apply twice the factor
            if (request.Configuration.PricesShouldBeScaled(liveMode: true))
            {
                enumerator = new PriceScaleFactorEnumerator(
                    enumerator,
                    request.Configuration,
                    _factorFileProvider,
                    liveMode: true);
            }

            if (request.Configuration.FillDataForward)
            {
                var fillForwardResolution = _subscriptions.UpdateAndGetFillForwardResolution(request.Configuration);
                // Pass the security exchange hours explicitly to avoid using the ones in the request, since
                // those could be different. e.g. when requests are created for open interest data the exchange
                // hours are set to always open to avoid OI data being filtered out due to the exchange being closed.
                var useDailyStrictEndTimes = LeanData.UseDailyStrictEndTimes(_algorithm.Settings, request, request.Configuration.Symbol, request.Configuration.Increment, request.Security.Exchange.Hours);

                enumerator = new LiveFillForwardEnumerator(_frontierTimeProvider, enumerator, request.Security.Exchange, fillForwardResolution,
                    request.Configuration.ExtendedMarketHours, localEndTime, request.Configuration.Resolution, request.Configuration.DataTimeZone,
                    useDailyStrictEndTimes, request.Configuration.Type);
            }

            // make our subscriptions aware of the frontier of the data feed, prevents future data from spewing into the feed
            enumerator = new FrontierAwareEnumerator(enumerator, _frontierTimeProvider, timeZoneOffsetProvider);

            // define market hours and user filters to incoming data after the frontier enumerator so during warmup we avoid any realtime data making it's way into the securities
            if (request.Configuration.IsFilteredSubscription)
            {
                enumerator = new SubscriptionFilterEnumerator(enumerator, request.Security, localEndTime, request.Configuration.ExtendedMarketHours, true, request.ExchangeHours);
            }

            enumerator = GetWarmupEnumerator(request, enumerator);

            var subscriptionDataEnumerator = new SubscriptionDataEnumerator(request.Configuration, request.Security.Exchange.Hours, timeZoneOffsetProvider,
                enumerator, request.IsUniverseSubscription, _algorithm.Settings.DailyPreciseEndTime);
            subscription = new Subscription(request, subscriptionDataEnumerator, timeZoneOffsetProvider);

            return subscription;
        }

        /// <summary>
        /// Helper method to determine if the symbol associated with the requested configuration is expired or not
        /// </summary>
        /// <remarks>This is useful during warmup where we can be requested to add some already expired asset. We want to skip sending it
        /// to our live <see cref="_dataQueueHandler"/> instance to avoid explosions. But we do want to add warmup enumerators</remarks>
        private bool IsExpired(SubscriptionDataConfig dataConfig)
        {
            var mapFile = _mapFileProvider.ResolveMapFile(dataConfig);
            var delistingDate = dataConfig.Symbol.GetDelistingDate(mapFile);
            return _timeProvider.GetUtcNow().Date > delistingDate.ConvertToUtc(dataConfig.ExchangeTimeZone);
        }

        private IEnumerator<BaseData> Subscribe(SubscriptionDataConfig dataConfig, EventHandler newDataAvailableHandler, Func<SubscriptionDataConfig, bool> isExpired)
        {
            return new LiveSubscriptionEnumerator(dataConfig, _dataQueueHandler, newDataAvailableHandler, isExpired);
        }

        /// <summary>
        /// Creates a new subscription for universe selection
        /// </summary>
        /// <param name="request">The subscription request</param>
        private Subscription CreateUniverseSubscription(SubscriptionRequest request)
        {
            Subscription subscription = null;

            // TODO : Consider moving the creating of universe subscriptions to a separate, testable class

            // grab the relevant exchange hours
            var config = request.Universe.Configuration;
            var localEndTime = request.EndTimeUtc.ConvertFromUtc(request.Security.Exchange.TimeZone);
            var tzOffsetProvider = new TimeZoneOffsetProvider(request.Configuration.ExchangeTimeZone, request.StartTimeUtc, request.EndTimeUtc);

            IEnumerator<BaseData> enumerator = null;

            var timeTriggered = request.Universe as ITimeTriggeredUniverse;
            if (timeTriggered != null)
            {
                Log.Trace($"LiveTradingDataFeed.CreateUniverseSubscription(): Creating user defined universe: {config.Symbol.ID}");

                // spoof a tick on the requested interval to trigger the universe selection function
                var enumeratorFactory = new TimeTriggeredUniverseSubscriptionEnumeratorFactory(timeTriggered, MarketHoursDatabase.FromDataFolder(), _frontierTimeProvider);
                enumerator = enumeratorFactory.CreateEnumerator(request, _dataProvider);

                enumerator = new FrontierAwareEnumerator(enumerator, _timeProvider, tzOffsetProvider);

                var enqueueable = new EnqueueableEnumerator<BaseData>();
                _customExchange.AddEnumerator(new EnumeratorHandler(config.Symbol, enumerator, enqueueable));
                enumerator = enqueueable;
            }
            else if (config.Type.IsAssignableTo(typeof(ETFConstituentUniverse)) ||
                config.Type.IsAssignableTo(typeof(FundamentalUniverse)) ||
                request.Universe is OptionChainUniverse ||
                request.Universe is FuturesChainUniverse)
            {
                Log.Trace($"LiveTradingDataFeed.CreateUniverseSubscription(): Creating {config.Type.Name} universe: {config.Symbol.ID}");

                // Will try to pull data from the data folder every 10min, file with yesterdays date.
                // If lean is started today it will trigger initial coarse universe selection
                var factory = new LiveCustomDataSubscriptionEnumeratorFactory(_timeProvider,
                    _algorithm.ObjectStore,
                    // we adjust time to the previous tradable date
                    time => Time.GetStartTimeForTradeBars(request.Security.Exchange.Hours, time, Time.OneDay, 1, false, config.DataTimeZone, _algorithm.Settings.DailyPreciseEndTime),
                    TimeSpan.FromMinutes(10)
                );
                var enumeratorStack = factory.CreateEnumerator(request, _dataProvider);

                // aggregates each coarse data point into a single BaseDataCollection
                var aggregator = new BaseDataCollectionAggregatorEnumerator(enumeratorStack, config.Symbol, true);
                var enqueable = new EnqueueableEnumerator<BaseData>();
                _customExchange.AddEnumerator(config.Symbol, aggregator, handleData: data =>
                {
                    enqueable.Enqueue(data);
                    subscription?.OnNewDataAvailable();
                });

                enumerator = GetConfiguredFrontierAwareEnumerator(enqueable, tzOffsetProvider,
                    // advance time if before 23pm or after 5am and not on Saturdays
                    time => time.Hour < 23 && time.Hour > 5 && time.DayOfWeek != DayOfWeek.Saturday);
            }
            else
            {
                Log.Trace("LiveTradingDataFeed.CreateUniverseSubscription(): Creating custom universe: " + config.Symbol.ID);

                var factory = new LiveCustomDataSubscriptionEnumeratorFactory(_timeProvider, _algorithm.ObjectStore);
                var enumeratorStack = factory.CreateEnumerator(request, _dataProvider);
                enumerator = new BaseDataCollectionAggregatorEnumerator(enumeratorStack, config.Symbol, liveMode: true);

                var enqueueable = new EnqueueableEnumerator<BaseData>();
                _customExchange.AddEnumerator(new EnumeratorHandler(config.Symbol, enumerator, enqueueable));
                enumerator = enqueueable;
            }

            enumerator = AddScheduleWrapper(request, enumerator, new PredicateTimeProvider(_frontierTimeProvider, (currentUtcDateTime) => {
                // will only let time advance after it's passed the live time shift frontier
                return currentUtcDateTime.TimeOfDay > _scheduledUniverseUtcTimeShift;
            }));

            enumerator = GetWarmupEnumerator(request, enumerator);

            // create the subscription
            var subscriptionDataEnumerator = new SubscriptionDataEnumerator(request.Configuration, request.Security.Exchange.Hours, tzOffsetProvider,
                enumerator, request.IsUniverseSubscription, _algorithm.Settings.DailyPreciseEndTime);
            subscription = new Subscription(request, subscriptionDataEnumerator, tzOffsetProvider);

            return subscription;
        }

        /// <summary>
        /// Build and apply the warmup enumerators when required
        /// </summary>
        private IEnumerator<BaseData> GetWarmupEnumerator(SubscriptionRequest request, IEnumerator<BaseData> liveEnumerator)
        {
            if (_algorithm.IsWarmingUp)
            {
                var warmupRequest = new SubscriptionRequest(request, endTimeUtc: _timeProvider.GetUtcNow(),
                    // we will not fill forward each warmup enumerators separately but concatenated bellow
                    configuration: new SubscriptionDataConfig(request.Configuration, fillForward: false,
                    resolution: _algorithm.Settings.WarmupResolution));
                if (warmupRequest.TradableDaysInDataTimeZone.Any()
                    // make sure there is at least room for a single bar of the requested resolution, else can cause issues with some history providers
                    // this could happen when we create some internal subscription whose start time is 'Now', which we don't really want to warmup
                    && warmupRequest.EndTimeUtc - warmupRequest.StartTimeUtc >= warmupRequest.Configuration.Resolution.ToTimeSpan()
                    // since we change the resolution, let's validate it's still valid configuration (example daily equity quotes are not!)
                    && LeanData.IsValidConfiguration(warmupRequest.Configuration.SecurityType, warmupRequest.Configuration.Resolution, warmupRequest.Configuration.TickType))
                {
                    // since we will source data locally and from the history provider, let's limit the history request size
                    // by setting a start date respecting the 'MaximumWarmupHistoryDaysLookBack'
                    var historyWarmup = warmupRequest;
                    var warmupHistoryStartDate = warmupRequest.EndTimeUtc.AddDays(-MaximumWarmupHistoryDaysLookBack);
                    if (warmupHistoryStartDate > warmupRequest.StartTimeUtc)
                    {
                        historyWarmup = new SubscriptionRequest(warmupRequest, startTimeUtc: warmupHistoryStartDate);
                    }

                    // let's keep track of the last point we got from the file based enumerator and start our history enumeration from this point
                    // this is much more efficient since these duplicated points will be dropped by the filter righ away causing memory usage spikes
                    var lastPointTracker = new LastPointTracker();

                    var synchronizedWarmupEnumerator = TryAddFillForwardEnumerator(warmupRequest,
                        // we concatenate the file based and history based warmup enumerators, dropping duplicate time stamps
                        new ConcatEnumerator(true, GetFileBasedWarmupEnumerator(warmupRequest, lastPointTracker), GetHistoryWarmupEnumerator(historyWarmup, lastPointTracker)) { CanEmitNull = false },
                        // if required by the original request, we will fill forward the Synced warmup data
                        request.Configuration.FillDataForward,
                        _algorithm.Settings.WarmupResolution);
                    synchronizedWarmupEnumerator = AddScheduleWrapper(warmupRequest, synchronizedWarmupEnumerator, null);

                    // don't let future data past. We let null pass because that's letting the next enumerator know we've ended because we always return true in live
                    synchronizedWarmupEnumerator = new FilterEnumerator<BaseData>(synchronizedWarmupEnumerator, data => data == null || data.EndTime <= warmupRequest.EndTimeLocal);

                    // the order here is important, concat enumerator will keep the last enumerator given and dispose of the rest
                    liveEnumerator = new ConcatEnumerator(true, synchronizedWarmupEnumerator, liveEnumerator);
                }
            }
            return liveEnumerator;
        }

        /// <summary>
        /// File based warmup enumerator
        /// </summary>
        private IEnumerator<BaseData> GetFileBasedWarmupEnumerator(SubscriptionRequest warmup, LastPointTracker lastPointTracker)
        {
            IEnumerator<BaseData> result = null;
            try
            {
                result = new FilterEnumerator<BaseData>(CreateEnumerator(warmup),
                    data =>
                    {
                        // don't let future data past, nor fill forward, that will be handled after merging with the history request response
                        if (data == null || data.EndTime < warmup.EndTimeLocal && !data.IsFillForward)
                        {
                            if (data != null)
                            {
                                lastPointTracker.LastDataPoint = data;
                            }
                            return true;
                        }
                        return false;
                    });
            }
            catch (Exception e)
            {
                Log.Error(e, $"File based warmup: {warmup.Configuration}");
            }
            return result;
        }

        /// <summary>
        /// History based warmup enumerator
        /// </summary>
        private IEnumerator<BaseData> GetHistoryWarmupEnumerator(SubscriptionRequest warmup, LastPointTracker lastPointTracker)
        {
            IEnumerator<BaseData> result;
            if (warmup.IsUniverseSubscription)
            {
                // we ignore the fill forward time span argument because we will fill forwared the concatenated file and history based enumerators next in the stack
                result = CreateUniverseEnumerator(warmup, createUnderlyingEnumerator: (req, _) => GetHistoryWarmupEnumerator(req, lastPointTracker));
            }
            else
            {
                // we create an enumerable of which we get the enumerator to defer the creation of the history request until the file based enumeration ended
                // and potentially the 'lastPointTracker' is available to adjust our start time
                result = new[] { warmup }.SelectMany(_ =>
                {
                    var startTimeUtc = warmup.StartTimeUtc;
                    if (lastPointTracker != null && lastPointTracker.LastDataPoint != null)
                    {
                        var lastPointExchangeTime = lastPointTracker.LastDataPoint.Time;
                        if (warmup.Configuration.Resolution == Resolution.Daily)
                        {
                            // time could be 9.30 for example using strict daily end times, but we just want the date in this case
                            lastPointExchangeTime = lastPointExchangeTime.Date;
                        }

                        var utcLastPointTime = lastPointExchangeTime.ConvertToUtc(warmup.ExchangeHours.TimeZone);
                        if (utcLastPointTime > startTimeUtc)
                        {
                            if (Log.DebuggingEnabled)
                            {
                                Log.Debug($"LiveTradingDataFeed.GetHistoryWarmupEnumerator(): Adjusting history warmup start time to {utcLastPointTime} from {startTimeUtc} for {warmup.Configuration}");
                            }
                            startTimeUtc = utcLastPointTime;
                        }
                    }
                    var historyRequest = new Data.HistoryRequest(warmup.Configuration, warmup.ExchangeHours, startTimeUtc, warmup.EndTimeUtc);
                    try
                    {
                        return _algorithm.HistoryProvider.GetHistory(new[] { historyRequest }, _algorithm.TimeZone).Select(slice =>
                        {
                            try
                            {
                                var data = slice.Get(historyRequest.DataType);
                                return (BaseData)data[warmup.Configuration.Symbol];
                            }
                            catch (Exception e)
                            {
                                Log.Error(e, $"History warmup: {warmup.Configuration}");
                            }
                            return null;
                        });
                    }
                    catch
                    {
                        // some history providers could throw if they do not support a type
                    }
                    return Enumerable.Empty<BaseData>();
                }).GetEnumerator();
            }

            return new FilterEnumerator<BaseData>(result,
                // don't let future data past, nor fill forward, that will be handled after merging with the file based enumerator
                data => data == null || data.EndTime < warmup.EndTimeLocal && !data.IsFillForward);
        }

        /// <summary>
        /// Will wrap the provided enumerator with a <see cref="FrontierAwareEnumerator"/>
        /// using a <see cref="PredicateTimeProvider"/> that will advance time based on the provided
        /// function
        /// </summary>
        /// <remarks>Won't advance time if now.Hour is bigger or equal than 23pm, less or equal than 5am or Saturday.
        /// This is done to prevent universe selection occurring in those hours so that the subscription changes
        /// are handled correctly.</remarks>
        private IEnumerator<BaseData> GetConfiguredFrontierAwareEnumerator(
            IEnumerator<BaseData> enumerator,
            TimeZoneOffsetProvider tzOffsetProvider,
            Func<DateTime, bool> customStepEvaluator)
        {
            var stepTimeProvider = new PredicateTimeProvider(_frontierTimeProvider, customStepEvaluator);

            return new FrontierAwareEnumerator(enumerator, stepTimeProvider, tzOffsetProvider);
        }

        private IDataQueueUniverseProvider GetUniverseProvider(SecurityType securityType)
        {
            if (_dataQueueHandler is not IDataQueueUniverseProvider or DataQueueHandlerManager { HasUniverseProvider: false })
            {
                throw new NotSupportedException($"The DataQueueHandler does not support {securityType}.");
            }
            return (IDataQueueUniverseProvider)_dataQueueHandler;
        }

        private void HandleUnsupportedConfigurationEvent(object _, SubscriptionDataConfig config)
        {
            if (_algorithm != null)
            {
                lock (_unsupportedConfigurations)
                {
                    var key = $"{config.Symbol.ID.Market} {config.Symbol.ID.SecurityType} {config.Type.Name}";
                    if (_unsupportedConfigurations.Add(key))
                    {
                        Log.Trace($"LiveTradingDataFeed.HandleUnsupportedConfigurationEvent(): detected unsupported configuration: {config}");

                        _algorithm.Debug($"Warning: {key} data not supported. Please consider reviewing the data providers selection.");
                    }
                }
            }
        }

        /// <summary>
        /// Overrides methods of the base data exchange implementation
        /// </summary>
        private class EnumeratorHandler : BaseDataExchange.EnumeratorHandler
        {
            public EnumeratorHandler(Symbol symbol, IEnumerator<BaseData> enumerator, EnqueueableEnumerator<BaseData> enqueueable)
                : base(symbol, enumerator, handleData: enqueueable.Enqueue)
            {
                EnumeratorFinished += (_, _) => enqueueable.Stop();
            }
        }

        private class LastPointTracker
        {
            public BaseData LastDataPoint { get; set; }
        }
    }
}

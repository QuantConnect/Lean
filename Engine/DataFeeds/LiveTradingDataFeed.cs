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
using System.Threading;
using System.Threading.Tasks;
using QuantConnect.Configuration;
using QuantConnect.Data;
using QuantConnect.Data.Custom;
using QuantConnect.Data.Custom.Fred;
using QuantConnect.Data.Custom.Tiingo;
using QuantConnect.Data.Custom.TradingEconomics;
using QuantConnect.Data.Market;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine.DataFeeds.Enumerators;
using QuantConnect.Lean.Engine.DataFeeds.Enumerators.Factories;
using QuantConnect.Lean.Engine.Results;
using QuantConnect.Logging;
using QuantConnect.Packets;
using QuantConnect.Securities;
using QuantConnect.Util;

namespace QuantConnect.Lean.Engine.DataFeeds
{
    /// <summary>
    /// Provides an implementation of <see cref="IDataFeed"/> that is designed to deal with
    /// live, remote data sources
    /// </summary>
    public class LiveTradingDataFeed : IDataFeed
    {
        private LiveNodePacket _job;
        // used to get current time
        private ITimeProvider _timeProvider;
        private ITimeProvider _frontierTimeProvider;
        private IDataProvider _dataProvider;
        private IDataQueueHandler _dataQueueHandler;
        private BaseDataExchange _customExchange;
        private SubscriptionCollection _subscriptions;
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private IDataChannelProvider _channelProvider;

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
        public void Initialize(IAlgorithm algorithm,
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

            _cancellationTokenSource = new CancellationTokenSource();

            _job = (LiveNodePacket) job;
            _timeProvider = dataFeedTimeProvider.TimeProvider;
            _dataProvider = dataProvider;
            _channelProvider = dataChannelProvider;

            _frontierTimeProvider = dataFeedTimeProvider.FrontierTimeProvider;
            _customExchange = new BaseDataExchange("CustomDataExchange") {SleepInterval = 10};
            _subscriptions = subscriptionManager.DataFeedSubscriptions;

            _dataQueueHandler = GetDataQueueHandler();
            _dataQueueHandler?.SetJob(_job);

            // run the custom data exchange
            var manualEvent = new ManualResetEventSlim(false);
            Task.Factory.StartNew(() =>
            {
                manualEvent.Set();
                _customExchange.Start(_cancellationTokenSource.Token);
            }, TaskCreationOptions.LongRunning);
            manualEvent.Wait();
            manualEvent.DisposeSafely();

            IsActive = true;
        }

        /// <summary>
        /// Creates a new subscription to provide data for the specified security.
        /// </summary>
        /// <param name="request">Defines the subscription to be added, including start/end times the universe and security</param>
        /// <returns>The created <see cref="Subscription"/> if successful, null otherwise</returns>
        public Subscription CreateSubscription(SubscriptionRequest request)
        {
            // create and add the subscription to our collection
            var subscription = request.IsUniverseSubscription
                ? CreateUniverseSubscription(request)
                : CreateDataSubscription(request);

            return subscription;
        }

        /// <summary>
        /// Removes the subscription from the data feed, if it exists
        /// </summary>
        /// <param name="subscription">The subscription to remove</param>
        public void RemoveSubscription(Subscription subscription)
        {
            var symbol = subscription.Configuration.Symbol;

            // remove the subscriptions
            if (!_channelProvider.ShouldStreamSubscription(_job, subscription.Configuration))
            {
                _customExchange.RemoveEnumerator(symbol);
                _customExchange.RemoveDataHandler(symbol);
            }
            else
            {
                _dataQueueHandler.Unsubscribe(subscription.Configuration);
                if (subscription.Configuration.SecurityType == SecurityType.Equity && !subscription.Configuration.IsInternalFeed)
                {
                    _dataQueueHandler.Unsubscribe(new SubscriptionDataConfig(subscription.Configuration, typeof(Dividend)));
                    _dataQueueHandler.Unsubscribe(new SubscriptionDataConfig(subscription.Configuration, typeof(Split)));
                }
            }
        }

        /// <summary>
        /// External controller calls to signal a terminate of the thread.
        /// </summary>
        public virtual void Exit()
        {
            if (IsActive)
            {
                IsActive = false;
                Log.Trace("LiveTradingDataFeed.Exit(): Start. Setting cancellation token...");
                _cancellationTokenSource.Cancel();
                _customExchange?.Stop();
                Log.Trace("LiveTradingDataFeed.Exit(): Exit Finished.");
            }
        }

        /// <summary>
        /// Gets the <see cref="IDataQueueHandler"/> to use. By default this will try to load
        /// the type specified in the configuration via the 'data-queue-handler'
        /// </summary>
        /// <returns>The loaded <see cref="IDataQueueHandler"/></returns>
        protected virtual IDataQueueHandler GetDataQueueHandler()
        {
            Log.Trace($"LiveTradingDataFeed.GetDataQueueHandler(): will use {_job.DataQueueHandler}");
            return Composer.Instance.GetExportedValueByTypeName<IDataQueueHandler>(_job.DataQueueHandler);
        }

        /// <summary>
        /// Creates a new subscription for the specified security
        /// </summary>
        /// <param name="request">The subscription request</param>
        /// <returns>A new subscription instance of the specified security</returns>
        protected Subscription CreateDataSubscription(SubscriptionRequest request)
        {
            Subscription subscription = null;

            try
            {
                var localEndTime = request.EndTimeUtc.ConvertFromUtc(request.Security.Exchange.TimeZone);
                var timeZoneOffsetProvider = new TimeZoneOffsetProvider(request.Security.Exchange.TimeZone, request.StartTimeUtc, request.EndTimeUtc);

                IEnumerator<BaseData> enumerator;
                if (!_channelProvider.ShouldStreamSubscription(_job, request.Configuration))
                {
                    if (!Quandl.IsAuthCodeSet)
                    {
                        // we're not using the SubscriptionDataReader, so be sure to set the auth token here
                        Quandl.SetAuthCode(Config.Get("quandl-auth-token"));
                    }

                    if (!Tiingo.IsAuthCodeSet)
                    {
                        // we're not using the SubscriptionDataReader, so be sure to set the auth token here
                        Tiingo.SetAuthCode(Config.Get("tiingo-auth-token"));
                    }

                    if (!USEnergyAPI.IsAuthCodeSet)
                    {
                        // we're not using the SubscriptionDataReader, so be sure to set the auth token here
                        USEnergyAPI.SetAuthCode(Config.Get("us-energy-information-auth-token"));
                    }

                    if (!FredApi.IsAuthCodeSet)
                    {
                        // we're not using the SubscriptionDataReader, so be sure to set the auth token here
                        FredApi.SetAuthCode(Config.Get("fred-auth-token"));
                    }

                    if (!TradingEconomicsCalendar.IsAuthCodeSet)
                    {
                        // we're not using the SubscriptionDataReader, so be sure to set the auth token here
                        TradingEconomicsCalendar.SetAuthCode(Config.Get("trading-economics-auth-token"));
                    }

                    var factory = new LiveCustomDataSubscriptionEnumeratorFactory(_timeProvider);
                    var enumeratorStack = factory.CreateEnumerator(request, _dataProvider);

                    _customExchange.AddEnumerator(request.Configuration.Symbol, enumeratorStack);

                    var enqueable = new EnqueueableEnumerator<BaseData>();
                    _customExchange.SetDataHandler(request.Configuration.Symbol, data =>
                    {
                        enqueable.Enqueue(data);

                        subscription.OnNewDataAvailable();
                    });
                    enumerator = enqueable;
                }
                else
                {
                    EventHandler handler = (sender, args) => subscription?.OnNewDataAvailable();
                    enumerator = _dataQueueHandler.Subscribe(request.Configuration, handler);

                    if (request.Configuration.Symbol.SecurityType == SecurityType.Equity && !request.Configuration.IsInternalFeed)
                    {
                        var dividends = _dataQueueHandler.Subscribe(new SubscriptionDataConfig(request.Configuration, typeof(Dividend)), handler);
                        var splits = _dataQueueHandler.Subscribe(new SubscriptionDataConfig(request.Configuration, typeof(Split)), handler);

                        enumerator = new LiveEquityDataSynchronizingEnumerator(_timeProvider, request.Configuration.ExchangeTimeZone, enumerator, dividends, splits);
                    }
                }

                if (request.Configuration.FillDataForward)
                {
                    var fillForwardResolution = _subscriptions.UpdateAndGetFillForwardResolution(request.Configuration);

                    enumerator = new LiveFillForwardEnumerator(_frontierTimeProvider, enumerator, request.Security.Exchange, fillForwardResolution, request.Configuration.ExtendedMarketHours, localEndTime, request.Configuration.Increment, request.Configuration.DataTimeZone, request.StartTimeLocal);
                }

                // define market hours and user filters to incoming data
                if (request.Configuration.IsFilteredSubscription)
                {
                    enumerator = new SubscriptionFilterEnumerator(enumerator, request.Security, localEndTime, request.Configuration.ExtendedMarketHours, true);
                }

                // finally, make our subscriptions aware of the frontier of the data feed, prevents future data from spewing into the feed
                enumerator = new FrontierAwareEnumerator(enumerator, _frontierTimeProvider, timeZoneOffsetProvider);

                var subscriptionDataEnumerator = new SubscriptionDataEnumerator(request.Configuration, request.Security.Exchange.Hours, timeZoneOffsetProvider, enumerator);
                subscription = new Subscription(request, subscriptionDataEnumerator, timeZoneOffsetProvider);
            }
            catch (Exception err)
            {
                Log.Error(err);
            }

            return subscription;
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
            var tzOffsetProvider = new TimeZoneOffsetProvider(request.Security.Exchange.TimeZone, request.StartTimeUtc, request.EndTimeUtc);

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
            else if (config.Type == typeof (CoarseFundamental))
            {
                Log.Trace($"LiveTradingDataFeed.CreateUniverseSubscription(): Creating coarse universe: {config.Symbol.ID}");

                // we subscribe using a normalized symbol, without a random GUID,
                // since the ticker plant will send the coarse data using this symbol
                var normalizedSymbol = CoarseFundamental.CreateUniverseSymbol(config.Symbol.ID.Market, false);

                // Will try to pull coarse data from the data folder every 30min, file with today's date.
                // If lean is started today it will trigger initial coarse universe selection
                var factory = new LiveCustomDataSubscriptionEnumeratorFactory(_timeProvider,
                    // we adjust time to the previous tradable date
                    time => Time.GetStartTimeForTradeBars(request.Security.Exchange.Hours, time, Time.OneDay, 1, false, config.DataTimeZone)
                );
                var enumeratorStack = factory.CreateEnumerator(request, _dataProvider);

                // aggregates each coarse data point into a single BaseDataCollection
                var aggregator = new BaseDataCollectionAggregatorEnumerator(enumeratorStack, normalizedSymbol, true);
                _customExchange.AddEnumerator(normalizedSymbol, aggregator);

                var enqueable = new EnqueueableEnumerator<BaseData>();
                _customExchange.SetDataHandler(normalizedSymbol, data =>
                {
                    var coarseData = data as BaseDataCollection;
                    enqueable.Enqueue(new BaseDataCollection(coarseData.Time, config.Symbol, coarseData.Data));
                    subscription.OnNewDataAvailable();
                });
                enumerator = GetConfiguredFrontierAwareEnumerator(enqueable, tzOffsetProvider,
                    // advance time if before 23pm or after 5am and not on Saturdays
                    time => time.Hour < 23 && time.Hour > 5 && time.DayOfWeek != DayOfWeek.Saturday);
            }
            else if (request.Universe is OptionChainUniverse)
            {
                Log.Trace("LiveTradingDataFeed.CreateUniverseSubscription(): Creating option chain universe: " + config.Symbol.ID);

                Func<SubscriptionRequest, IEnumerator<BaseData>> configure = (subRequest) =>
                {
                    var fillForwardResolution = _subscriptions.UpdateAndGetFillForwardResolution(subRequest.Configuration);
                    var input = _dataQueueHandler.Subscribe(subRequest.Configuration, (sender, args) => subscription.OnNewDataAvailable());
                    return new LiveFillForwardEnumerator(_frontierTimeProvider, input, subRequest.Security.Exchange, fillForwardResolution, subRequest.Configuration.ExtendedMarketHours, localEndTime, subRequest.Configuration.Increment, subRequest.Configuration.DataTimeZone, subRequest.StartTimeLocal);
                };

                var symbolUniverse = _dataQueueHandler as IDataQueueUniverseProvider;
                if (symbolUniverse == null)
                {
                    throw new NotSupportedException("The DataQueueHandler does not support Options.");
                }

                var timeProvider = new PredicateTimeProvider(_timeProvider,
                    time => symbolUniverse.CanAdvanceTime(config.SecurityType));

                var enumeratorFactory = new OptionChainUniverseSubscriptionEnumeratorFactory(configure, symbolUniverse, timeProvider);
                enumerator = enumeratorFactory.CreateEnumerator(request, _dataProvider);

                enumerator = new FrontierAwareEnumerator(enumerator, _frontierTimeProvider, tzOffsetProvider);
            }
            else if (request.Universe is FuturesChainUniverse)
            {
                Log.Trace("LiveTradingDataFeed.CreateUniverseSubscription(): Creating futures chain universe: " + config.Symbol.ID);

                var symbolUniverse = _dataQueueHandler as IDataQueueUniverseProvider;
                if (symbolUniverse == null)
                {
                    throw new NotSupportedException("The DataQueueHandler does not support Futures.");
                }

                var timeProvider = new PredicateTimeProvider(_timeProvider,
                    time => symbolUniverse.CanAdvanceTime(config.SecurityType));

                var enumeratorFactory = new FuturesChainUniverseSubscriptionEnumeratorFactory(symbolUniverse, timeProvider);
                enumerator = enumeratorFactory.CreateEnumerator(request, _dataProvider);

                enumerator = new FrontierAwareEnumerator(enumerator, _frontierTimeProvider, tzOffsetProvider);
            }
            else
            {
                Log.Trace("LiveTradingDataFeed.CreateUniverseSubscription(): Creating custom universe: " + config.Symbol.ID);

                var factory = new LiveCustomDataSubscriptionEnumeratorFactory(_timeProvider);
                var enumeratorStack = factory.CreateEnumerator(request, _dataProvider);
                enumerator = new BaseDataCollectionAggregatorEnumerator(enumeratorStack, config.Symbol, liveMode:true);

                var enqueueable = new EnqueueableEnumerator<BaseData>();
                _customExchange.AddEnumerator(new EnumeratorHandler(config.Symbol, enumerator, enqueueable));
                enumerator = enqueueable;
            }

            // create the subscription
            var subscriptionDataEnumerator = new SubscriptionDataEnumerator(request.Configuration, request.Security.Exchange.Hours, tzOffsetProvider, enumerator);
            subscription = new Subscription(request, subscriptionDataEnumerator, tzOffsetProvider);

            // send the subscription for the new symbol through to the data queuehandler
            if (_channelProvider.ShouldStreamSubscription(_job, subscription.Configuration))
            {
                _dataQueueHandler.Subscribe(request.Configuration, (sender, args) => subscription.OnNewDataAvailable());
            }

            return subscription;
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

        /// <summary>
        /// Overrides methods of the base data exchange implementation
        /// </summary>
        class EnumeratorHandler : BaseDataExchange.EnumeratorHandler
        {
            private readonly EnqueueableEnumerator<BaseData> _enqueueable;
            public EnumeratorHandler(Symbol symbol, IEnumerator<BaseData> enumerator, EnqueueableEnumerator<BaseData> enqueueable)
                : base(symbol, enumerator, true)
            {
                _enqueueable = enqueueable;
            }
            /// <summary>
            /// Returns true if this enumerator should move next
            /// </summary>
            public override bool ShouldMoveNext() { return true; }
            /// <summary>
            /// Calls stop on the internal enqueueable enumerator
            /// </summary>
            public override void OnEnumeratorFinished() { _enqueueable.Stop(); }
            /// <summary>
            /// Enqueues the data
            /// </summary>
            /// <param name="data">The data to be handled</param>
            public override void HandleData(BaseData data)
            {
                _enqueueable.Enqueue(data);
            }
        }
    }
}

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
using System.Threading;
using System.Threading.Tasks;
using QuantConnect.Configuration;
using QuantConnect.Data;
using QuantConnect.Data.Custom;
using QuantConnect.Data.Custom.Tiingo;
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
        private static readonly Symbol DataQueueHandlerSymbol = Symbol.Create("data-queue-handler-symbol", SecurityType.Base, Market.USA);

        private LiveNodePacket _job;
        private IAlgorithm _algorithm;
        // used to get current time
        private ITimeProvider _timeProvider;
        private ITimeProvider _frontierTimeProvider;
        private IDataProvider _dataProvider;
        private IDataQueueHandler _dataQueueHandler;
        private BaseDataExchange _exchange;
        private BaseDataExchange _customExchange;
        private SubscriptionCollection _subscriptions;
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private UniverseSelection _universeSelection;

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
            IDataFeedTimeProvider dataFeedTimeProvider)
        {
            if (!(job is LiveNodePacket))
            {
                throw new ArgumentException("The LiveTradingDataFeed requires a LiveNodePacket.");
            }

            _cancellationTokenSource = new CancellationTokenSource();

            _algorithm = algorithm;
            _job = (LiveNodePacket) job;

            _timeProvider = dataFeedTimeProvider.TimeProvider;
            _dataQueueHandler = GetDataQueueHandler();
            _dataProvider = dataProvider;

            _frontierTimeProvider = dataFeedTimeProvider.FrontierTimeProvider;
            _customExchange = new BaseDataExchange("CustomDataExchange") {SleepInterval = 10};
            // sleep is controlled on this exchange via the GetNextTicksEnumerator
            _exchange = new BaseDataExchange("DataQueueExchange"){SleepInterval = 0};
            _exchange.AddEnumerator(DataQueueHandlerSymbol, GetNextTicksEnumerator());
            _subscriptions = subscriptionManager.DataFeedSubscriptions;

            _universeSelection = subscriptionManager.UniverseSelection;

            // run the exchanges
            Task.Run(() => _exchange.Start(_cancellationTokenSource.Token));
            Task.Run(() => _customExchange.Start(_cancellationTokenSource.Token));

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

            // check if we could create the subscription
            if (subscription != null)
            {
                // send the subscription for the new symbol through to the data queuehandler
                // unless it is custom data, custom data is retrieved using the same as backtest
                if (!subscription.Configuration.IsCustomData)
                {
                    _dataQueueHandler.Subscribe(_job, new[] { request.Security.Symbol });
                }
            }

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
            if (subscription.Configuration.IsCustomData)
            {
                _customExchange.RemoveEnumerator(symbol);
                _customExchange.RemoveDataHandler(symbol);
            }
            else
            {
                _dataQueueHandler.Unsubscribe(_job, new[] { symbol });
                _exchange.RemoveDataHandler(symbol);
            }
        }

        /// <summary>
        /// External controller calls to signal a terminate of the thread.
        /// </summary>
        public void Exit()
        {
            if (IsActive)
            {
                IsActive = false;
                Log.Trace("LiveTradingDataFeed.Exit(): Start. Setting cancellation token...");
                _cancellationTokenSource.Cancel();
                _exchange?.Stop();
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
                if (request.Configuration.IsCustomData)
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

                    var factory = new LiveCustomDataSubscriptionEnumeratorFactory(_timeProvider);
                    var enumeratorStack = factory.CreateEnumerator(request, _dataProvider);

                    _customExchange.AddEnumerator(request.Configuration.Symbol, enumeratorStack);

                    var enqueable = new EnqueueableEnumerator<BaseData>();
                    _customExchange.SetDataHandler(request.Configuration.Symbol, data =>
                    {
                        enqueable.Enqueue(data);

                        subscription.OnNewDataAvailable();

                        UpdateSubscriptionRealTimePrice(
                            subscription,
                            timeZoneOffsetProvider,
                            request.Security.Exchange.Hours,
                            data);
                    });
                    enumerator = enqueable;
                }
                else if (request.Configuration.Resolution != Resolution.Tick)
                {
                    // this enumerator allows the exchange to pump ticks into the 'back' of the enumerator,
                    // and the time sync loop can pull aggregated trade bars off the front
                    switch (request.Configuration.TickType)
                    {
                        case TickType.Quote:
                            var quoteBarAggregator = new QuoteBarBuilderEnumerator(
                                request.Configuration.Increment,
                                request.Security.Exchange.TimeZone,
                                _timeProvider,
                                true,
                                (sender, args) => subscription.OnNewDataAvailable());

                            _exchange.AddDataHandler(request.Configuration.Symbol, data =>
                            {
                                var tick = data as Tick;

                                if (tick?.TickType == TickType.Quote && !tick.Suspicious)
                                {
                                    quoteBarAggregator.ProcessData(tick);

                                    UpdateSubscriptionRealTimePrice(
                                        subscription,
                                        timeZoneOffsetProvider,
                                        request.Security.Exchange.Hours,
                                        data);
                                }
                            });
                            enumerator = quoteBarAggregator;
                            break;

                        case TickType.Trade:
                        default:
                            var tradeBarAggregator = new TradeBarBuilderEnumerator(
                                request.Configuration.Increment,
                                request.Security.Exchange.TimeZone,
                                _timeProvider,
                                true,
                                (sender, args) => subscription.OnNewDataAvailable());

                            var auxDataEnumerator = new LiveAuxiliaryDataEnumerator(request.Security.Exchange.TimeZone, _timeProvider);

                            _exchange.AddDataHandler(request.Configuration.Symbol, data =>
                            {
                                if (data.DataType == MarketDataType.Auxiliary)
                                {
                                    auxDataEnumerator.Enqueue(data);

                                    subscription.OnNewDataAvailable();
                                }
                                else
                                {
                                    var tick = data as Tick;
                                    if (tick?.TickType == TickType.Trade && !tick.Suspicious)
                                    {
                                        tradeBarAggregator.ProcessData(tick);

                                        UpdateSubscriptionRealTimePrice(
                                            subscription,
                                            timeZoneOffsetProvider,
                                            request.Security.Exchange.Hours,
                                            data);
                                    }
                                }
                            });

                            enumerator = request.Configuration.SecurityType == SecurityType.Equity
                                ? (IEnumerator<BaseData>) new LiveEquityDataSynchronizingEnumerator(_frontierTimeProvider, request.Security.Exchange.TimeZone, auxDataEnumerator, tradeBarAggregator)
                                : tradeBarAggregator;
                            break;

                        case TickType.OpenInterest:
                            var oiAggregator = new OpenInterestEnumerator(
                                request.Configuration.Increment,
                                request.Security.Exchange.TimeZone,
                                _timeProvider,
                                true,
                                (sender, args) => subscription.OnNewDataAvailable());

                            _exchange.AddDataHandler(request.Configuration.Symbol, data =>
                            {
                                var tick = data as Tick;

                                if (tick?.TickType == TickType.OpenInterest && !tick.Suspicious)
                                {
                                    oiAggregator.ProcessData(tick);
                                }
                            });
                            enumerator = oiAggregator;
                            break;
                    }
                }
                else
                {
                    // tick subscriptions can pass right through
                    var tickEnumerator = new EnqueueableEnumerator<BaseData>();

                    _exchange.AddDataHandler(request.Configuration.Symbol, data =>
                    {
                        if (data.DataType == MarketDataType.Auxiliary)
                        {
                            tickEnumerator.Enqueue(data);
                            subscription.OnNewDataAvailable();
                        }
                        else
                        {
                            var tick = data as Tick;
                            if (tick?.TickType == request.Configuration.TickType)
                            {
                                tickEnumerator.Enqueue(data);
                                subscription.OnNewDataAvailable();
                                if (tick.TickType != TickType.OpenInterest)
                                {
                                    UpdateSubscriptionRealTimePrice(
                                        subscription,
                                        timeZoneOffsetProvider,
                                        request.Security.Exchange.Hours,
                                        data);
                                }
                            }
                        }
                    });

                    enumerator = tickEnumerator;
                }

                if (request.Configuration.FillDataForward)
                {
                    var fillForwardResolution = _subscriptions.UpdateAndGetFillForwardResolution(request.Configuration);

                    enumerator = new LiveFillForwardEnumerator(_frontierTimeProvider, enumerator, request.Security.Exchange, fillForwardResolution, request.Configuration.ExtendedMarketHours, localEndTime, request.Configuration.Increment, request.Configuration.DataTimeZone, request.StartTimeLocal);
                }

                // define market hours and user filters to incoming data
                if (request.Configuration.IsFilteredSubscription)
                {
                    enumerator = new SubscriptionFilterEnumerator(enumerator, request.Security, localEndTime);
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

            IEnumerator<BaseData> enumerator;

            var timeTriggered = request.Universe as ITimeTriggeredUniverse;
            if (timeTriggered != null)
            {
                Log.Trace("LiveTradingDataFeed.CreateUniverseSubscription(): Creating user defined universe: " + config.Symbol.ToString());

                // spoof a tick on the requested interval to trigger the universe selection function
                var enumeratorFactory = new TimeTriggeredUniverseSubscriptionEnumeratorFactory(timeTriggered, MarketHoursDatabase.FromDataFolder());
                enumerator = enumeratorFactory.CreateEnumerator(request, _dataProvider);

                enumerator = new FrontierAwareEnumerator(enumerator, _timeProvider, tzOffsetProvider);

                var enqueueable = new EnqueueableEnumerator<BaseData>();
                _customExchange.AddEnumerator(new EnumeratorHandler(config.Symbol, enumerator, enqueueable));
                enumerator = enqueueable;

                // Trigger universe selection when security added/removed after Initialize
                if (timeTriggered is UserDefinedUniverse)
                {
                    var userDefined = (UserDefinedUniverse) timeTriggered;
                    userDefined.CollectionChanged += (sender, args) =>
                    {
                        var items =
                            args.Action == NotifyCollectionChangedAction.Add ? args.NewItems :
                            args.Action == NotifyCollectionChangedAction.Remove ? args.OldItems : null;

                        var currentFrontierUtcTime = _frontierTimeProvider.GetUtcNow();
                        if (items == null || currentFrontierUtcTime == DateTime.MinValue) return;

                        var symbol = items.OfType<Symbol>().FirstOrDefault();
                        if (symbol == null) return;

                        var collection = new BaseDataCollection(currentFrontierUtcTime, symbol);
                        var changes = _universeSelection.ApplyUniverseSelection(userDefined, currentFrontierUtcTime, collection);
                        _algorithm.OnSecuritiesChanged(changes);

                        subscription.OnNewDataAvailable();
                    };
                }
            }
            else if (config.Type == typeof (CoarseFundamental))
            {
                Log.Trace("LiveTradingDataFeed.CreateUniverseSubscription(): Creating coarse universe: " + config.Symbol.ToString());

                // we subscribe using a normalized symbol, without a random GUID,
                // since the ticker plant will send the coarse data using this symbol
                var normalizedSymbol = CoarseFundamental.CreateUniverseSymbol(config.Symbol.ID.Market, false);

                // since we're binding to the data queue exchange we'll need to let him
                // know that we expect this data
                _dataQueueHandler.Subscribe(_job, new[] { normalizedSymbol });

                var enqueable = new EnqueueableEnumerator<BaseData>();
                // We `AddDataHandler` not `Set` so we can have multiple handlers for the coarse data
                _exchange.AddDataHandler(normalizedSymbol, data =>
                {
                    enqueable.Enqueue(data);

                    subscription.OnNewDataAvailable();

                });

                enumerator = GetConfiguredFrontierAwareEnumerator(enqueable, tzOffsetProvider);
            }
            else if (request.Universe is OptionChainUniverse)
            {
                Log.Trace("LiveTradingDataFeed.CreateUniverseSubscription(): Creating option chain universe: " + config.Symbol.ToString());

                Func<SubscriptionRequest, IEnumerator<BaseData>, IEnumerator<BaseData>> configure = (subRequest, input) =>
                {
                    // we check if input enumerator is an underlying enumerator. If yes, we subscribe it to the data.
                    var aggregator = input as TradeBarBuilderEnumerator;

                    if (aggregator != null)
                    {
                        _exchange.SetDataHandler(request.Configuration.Symbol, data =>
                        {
                            aggregator.ProcessData((Tick)data);
                        });
                    }

                    var fillForwardResolution = _subscriptions.UpdateAndGetFillForwardResolution(request.Configuration);

                    return new LiveFillForwardEnumerator(_frontierTimeProvider, input, request.Security.Exchange, fillForwardResolution, request.Configuration.ExtendedMarketHours, localEndTime, request.Configuration.Increment, request.Configuration.DataTimeZone, request.StartTimeLocal);
                };

                var symbolUniverse = _dataQueueHandler as IDataQueueUniverseProvider;
                if (symbolUniverse == null)
                {
                    throw new NotSupportedException("The DataQueueHandler does not support Options.");
                }

                var enumeratorFactory = new OptionChainUniverseSubscriptionEnumeratorFactory(configure, symbolUniverse, _timeProvider);
                enumerator = enumeratorFactory.CreateEnumerator(request, _dataProvider);

                enumerator = GetConfiguredFrontierAwareEnumerator(enumerator, tzOffsetProvider);
            }
            else if (request.Universe is FuturesChainUniverse)
            {
                Log.Trace("LiveTradingDataFeed.CreateUniverseSubscription(): Creating futures chain universe: " + config.Symbol.ToString());

                var symbolUniverse = _dataQueueHandler as IDataQueueUniverseProvider;
                if (symbolUniverse == null)
                {
                    throw new NotSupportedException("The DataQueueHandler does not support Futures.");
                }

                var enumeratorFactory = new FuturesChainUniverseSubscriptionEnumeratorFactory(symbolUniverse, _timeProvider);
                enumerator = enumeratorFactory.CreateEnumerator(request, _dataProvider);

                enumerator = GetConfiguredFrontierAwareEnumerator(enumerator, tzOffsetProvider);
            }
            else
            {
                Log.Trace("LiveTradingDataFeed.CreateUniverseSubscription(): Creating custom universe: " + config.Symbol.ToString());

                var factory = new LiveCustomDataSubscriptionEnumeratorFactory(_timeProvider);
                var enumeratorStack = factory.CreateEnumerator(request, _dataProvider);
                enumerator = new BaseDataCollectionAggregatorEnumerator(enumeratorStack, config.Symbol);

                var enqueueable = new EnqueueableEnumerator<BaseData>();
                _customExchange.AddEnumerator(new EnumeratorHandler(config.Symbol, enumerator, enqueueable));
                enumerator = enqueueable;
            }

            // create the subscription
            var subscriptionDataEnumerator = new SubscriptionDataEnumerator(request.Configuration, request.Security.Exchange.Hours, tzOffsetProvider, enumerator);
            subscription = new Subscription(request, subscriptionDataEnumerator, tzOffsetProvider);

            return subscription;
        }

        /// <summary>
        /// Updates the subscription RealTimePrice if the exchange is open
        /// </summary>
        /// <param name="subscription">The <see cref="Subscription"/></param>
        /// <param name="timeZoneOffsetProvider">The <see cref="TimeZoneOffsetProvider"/> used to convert now into the timezone of the exchange</param>
        /// <param name="exchangeHours">The <see cref="SecurityExchangeHours"/> used to determine
        /// if the exchange is open and we should update</param>
        /// <param name="data">The <see cref="BaseData"/> used to update the real time price</param>
        /// <returns>True if the real time price was updated</returns>
        protected bool UpdateSubscriptionRealTimePrice(
            Subscription subscription,
            TimeZoneOffsetProvider timeZoneOffsetProvider,
            SecurityExchangeHours exchangeHours,
            BaseData data)
        {
            if (subscription != null &&
                exchangeHours.IsOpen(
                    timeZoneOffsetProvider.ConvertFromUtc(_timeProvider.GetUtcNow()),
                    subscription.Configuration.ExtendedMarketHours))
            {
                subscription.RealtimePrice = data.Value;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Provides an <see cref="IEnumerator{BaseData}"/> that will continually dequeue data
        /// from the data queue handler while we're not cancelled
        /// </summary>
        /// <returns></returns>
        private IEnumerator<BaseData> GetNextTicksEnumerator()
        {
            while (!_cancellationTokenSource.IsCancellationRequested)
            {
                int ticks = 0;
                foreach (var data in _dataQueueHandler.GetNextTicks())
                {
                    ticks++;
                    yield return data;
                }
                if (ticks == 0) Thread.Sleep(1);
            }

            Log.Trace("LiveTradingDataFeed.GetNextTicksEnumerator(): Exiting enumerator thread...");
        }

        /// <summary>
        /// Will wrap the provided enumerator with a <see cref="FrontierAwareEnumerator"/>
        /// using a <see cref="PredicateTimeProvider"/> that will advance time based on the provided
        /// function
        /// </summary>
        /// <remarks>Won't advance time if now.Hour is bigger or equal than 23pm, less or equal than 5am or Saturday.
        /// This is done to prevent universe selection occurring in those hours so that the subscription changes
        /// are handled correctly.</remarks>
        private IEnumerator<BaseData> GetConfiguredFrontierAwareEnumerator(IEnumerator<BaseData> enumerator,
            TimeZoneOffsetProvider tzOffsetProvider)
        {
            var stepTimeProvider = new PredicateTimeProvider(_frontierTimeProvider,
                // advance time if before 23pm or after 5am and not on Saturdays
                time => time.Hour < 23 && time.Hour > 5 && time.DayOfWeek != DayOfWeek.Saturday);

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

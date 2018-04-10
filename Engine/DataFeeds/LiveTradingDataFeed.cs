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
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using QuantConnect.Configuration;
using QuantConnect.Data;
using QuantConnect.Data.Custom;
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
        private SecurityChanges _changes = SecurityChanges.None;
        private static readonly Symbol DataQueueHandlerSymbol = Symbol.Create("data-queue-handler-symbol", SecurityType.Base, Market.USA);

        private LiveNodePacket _job;
        private IAlgorithm _algorithm;
        // used to get current time
        private ITimeProvider _timeProvider;
        // used to keep time constant during a time sync iteration
        private ManualTimeProvider _frontierTimeProvider;
        private IDataProvider _dataProvider;
        private SingleEntryDataCacheProvider _dataCacheProvider;

        private Ref<TimeSpan> _fillForwardResolution;
        private IResultHandler _resultHandler;
        private IDataQueueHandler _dataQueueHandler;
        private BaseDataExchange _exchange;
        private BaseDataExchange _customExchange;
        private SubscriptionCollection _subscriptions;
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private BusyBlockingCollection<TimeSlice> _bridge;
        private UniverseSelection _universeSelection;
        private DateTime _frontierUtc;


        /// <summary>
        /// Gets all of the current subscriptions this data feed is processing
        /// </summary>
        public IEnumerable<Subscription> Subscriptions
        {
            get { return _subscriptions; }
        }

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
        public void Initialize(IAlgorithm algorithm, AlgorithmNodePacket job, IResultHandler resultHandler, IMapFileProvider mapFileProvider, IFactorFileProvider factorFileProvider, IDataProvider dataProvider)
        {
            if (!(job is LiveNodePacket))
            {
                throw new ArgumentException("The LiveTradingDataFeed requires a LiveNodePacket.");
            }

            _cancellationTokenSource = new CancellationTokenSource();

            _algorithm = algorithm;
            _job = (LiveNodePacket) job;
            _resultHandler = resultHandler;
            _timeProvider = GetTimeProvider();
            _dataQueueHandler = GetDataQueueHandler();
            _dataProvider = dataProvider;
            _dataCacheProvider = new SingleEntryDataCacheProvider(dataProvider);

            _frontierTimeProvider = new ManualTimeProvider(_timeProvider.GetUtcNow());
            _customExchange = new BaseDataExchange("CustomDataExchange") {SleepInterval = 10};
            // sleep is controlled on this exchange via the GetNextTicksEnumerator
            _exchange = new BaseDataExchange("DataQueueExchange"){SleepInterval = 0};
            _exchange.AddEnumerator(DataQueueHandlerSymbol, GetNextTicksEnumerator());
            _subscriptions = new SubscriptionCollection();

            _bridge = new BusyBlockingCollection<TimeSlice>();
            _universeSelection = new UniverseSelection(this, algorithm);

            // run the exchanges
            Task.Run(() => _exchange.Start(_cancellationTokenSource.Token));
            Task.Run(() => _customExchange.Start(_cancellationTokenSource.Token));

            // this value will be modified via calls to AddSubscription/RemoveSubscription
            var ffres = Time.OneMinute;
            _fillForwardResolution = Ref.Create(() => ffres, v => ffres = v);

            // wire ourselves up to receive notifications when universes are added/removed
            var start = _timeProvider.GetUtcNow();
            algorithm.UniverseManager.CollectionChanged += (sender, args) =>
            {
                switch (args.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                        foreach (var universe in args.NewItems.OfType<Universe>())
                        {
                            var config = universe.Configuration;
                            var marketHoursDatabase = MarketHoursDatabase.FromDataFolder();
                            var exchangeHours = marketHoursDatabase.GetExchangeHours(config);

                            Security security;
                            if (!_algorithm.Securities.TryGetValue(config.Symbol, out security))
                            {
                                // create a canonical security object
                                security = new Security(exchangeHours, config, _algorithm.Portfolio.CashBook[CashBook.AccountCurrency], SymbolProperties.GetDefault(CashBook.AccountCurrency));
                            }

                            AddSubscription(new SubscriptionRequest(true, universe, security, config, start, Time.EndOfTime));

                            // Not sure if this is needed but left here because of this:
                            // https://github.com/QuantConnect/Lean/commit/029d70bde6ca83a1eb0c667bb5cc4444bea05678
                            UpdateFillForwardResolution();
                        }
                        break;

                    case NotifyCollectionChangedAction.Remove:
                        foreach (var universe in args.OldItems.OfType<Universe>())
                        {
                            RemoveSubscription(universe.Configuration);
                        }
                        break;

                    default:
                        throw new NotImplementedException("The specified action is not implemented: " + args.Action);
                }
            };
        }

        /// <summary>
        /// Adds a new subscription to provide data for the specified security.
        /// </summary>
        /// <param name="request">Defines the subscription to be added, including start/end times the universe and security</param>
        /// <returns>True if the subscription was created and added successfully, false otherwise</returns>
        public bool AddSubscription(SubscriptionRequest request)
        {
            if (_subscriptions.Contains(request.Configuration))
            {
                // duplicate subscription request
                return false;
            }

            // create and add the subscription to our collection
            var subscription = request.IsUniverseSubscription
                ? CreateUniverseSubscription(request)
                : CreateSubscription(request);

            // for some reason we couldn't create the subscription
            if (subscription == null)
            {
                Log.Trace("Unable to add subscription for: " + request.Configuration);
                return false;
            }

            Log.Trace("LiveTradingDataFeed.AddSubscription(): Added " + request.Configuration);

            _subscriptions.TryAdd(subscription);

            // send the subscription for the new symbol through to the data queuehandler
            // unless it is custom data, custom data is retrieved using the same as backtest
            if (!subscription.Configuration.IsCustomData)
            {
                _dataQueueHandler.Subscribe(_job, new[] {request.Security.Symbol});
            }

            // keep track of security changes, we emit these to the algorithm
            // as notifications, used in universe selection
            if (!request.IsUniverseSubscription)
            {
                _changes += SecurityChanges.Added(request.Security);
            }

            UpdateFillForwardResolution();

            return true;
        }

        /// <summary>
        /// Removes the subscription from the data feed, if it exists
        /// </summary>
        /// <param name="configuration">The configuration of the subscription to remove</param>
        /// <returns>True if the subscription was successfully removed, false otherwise</returns>
        public bool RemoveSubscription(SubscriptionDataConfig configuration)
        {
            // remove the subscription from our collection
            Subscription subscription;
            if (!_subscriptions.TryRemove(configuration, out subscription))
            {
                Log.Error("LiveTradingDataFeed.RemoveSubscription(): Unable to remove: " + configuration.ToString());
                return false;
            }

            var security = subscription.Security;

            // remove the subscriptions
            if (subscription.Configuration.IsCustomData)
            {
                _customExchange.RemoveEnumerator(security.Symbol);
                _customExchange.RemoveDataHandler(security.Symbol);
            }
            else
            {
                _dataQueueHandler.Unsubscribe(_job, new[] { security.Symbol });
                _exchange.RemoveDataHandler(security.Symbol);
            }

            subscription.Dispose();

            // keep track of security changes, we emit these to the algorithm
            // as notications, used in universe selection
            if (!subscription.IsUniverseSelectionSubscription)
            {
                _changes += SecurityChanges.Removed(security);
            }

            Log.Trace("LiveTradingDataFeed.RemoveSubscription(): Removed " + configuration);
            UpdateFillForwardResolution();

            return true;
        }

        /// <summary>
        /// Primary entry point.
        /// </summary>
        public void Run()
        {
            IsActive = true;

            // we want to emit to the bridge minimally once a second since the data feed is
            // the heartbeat of the application, so this value will contain a second after
            // the last emit time, and if we pass this time, we'll emit even with no data
            var nextEmit = DateTime.MinValue;

            try
            {
                while (!_cancellationTokenSource.IsCancellationRequested)
                {
                    // perform sleeps to wake up on the second?
                    _frontierUtc = _timeProvider.GetUtcNow();
                    _frontierTimeProvider.SetCurrentTime(_frontierUtc);

                    var data = new List<DataFeedPacket>();
                    foreach (var subscription in Subscriptions)
                    {
                        var config = subscription.Configuration;
                        var packet = new DataFeedPacket(subscription.Security, config);

                        // dequeue data that is time stamped at or before this frontier
                        while (subscription.MoveNext() && subscription.Current != null)
                        {
                            packet.Add(subscription.Current.Data);
                        }

                        // if we have data, add it to be added to the bridge
                        if (packet.Count > 0) data.Add(packet);

                        // we have new universe data to select based on
                        if (subscription.IsUniverseSelectionSubscription && packet.Count > 0)
                        {
                            var universe = subscription.Universe;

                            // always wait for other thread to sync up
                            if (!_bridge.WaitHandle.WaitOne(Timeout.Infinite, _cancellationTokenSource.Token))
                            {
                                break;
                            }

                            // assume that if the first item is a base data collection then the enumerator handled the aggregation,
                            // otherwise, load all the the data into a new collection instance
                            var collection = packet.Data[0] as BaseDataCollection ?? new BaseDataCollection(_frontierUtc, config.Symbol, packet.Data);

                            _changes += _universeSelection.ApplyUniverseSelection(universe, _frontierUtc, collection);
                        }
                    }

                    // check for cancellation
                    if (_cancellationTokenSource.IsCancellationRequested) return;

                    // emit on data or if we've elapsed a full second since last emit
                    if (data.Count != 0 || _frontierUtc >= nextEmit)
                    {
                        _bridge.Add(TimeSlice.Create(_frontierUtc, _algorithm.TimeZone, _algorithm.Portfolio.CashBook, data, _changes), _cancellationTokenSource.Token);

                        // force emitting every second
                        nextEmit = _frontierUtc.RoundDown(Time.OneSecond).Add(Time.OneSecond);
                    }

                    // reset our security changes
                    _changes = SecurityChanges.None;

                    // take a short nap
                    Thread.Sleep(1);
                }
            }
            catch (Exception err)
            {
                Log.Error(err);
                _algorithm.RunTimeError = err;
                _algorithm.Status = AlgorithmStatus.RuntimeError;

                // send last empty packet list before terminating,
                // so the algorithm manager has a chance to detect the runtime error
                // and exit showing the correct error instead of a timeout
                nextEmit = _frontierUtc.RoundDown(Time.OneSecond).Add(Time.OneSecond);

                if (!_cancellationTokenSource.IsCancellationRequested)
                {
                    _bridge.Add(
                        TimeSlice.Create(nextEmit, _algorithm.TimeZone, _algorithm.Portfolio.CashBook, new List<DataFeedPacket>(), SecurityChanges.None),
                        _cancellationTokenSource.Token);
                }
            }

            Log.Trace("LiveTradingDataFeed.Run(): Exited thread.");
            IsActive = false;
        }

        /// <summary>
        /// External controller calls to signal a terminate of the thread.
        /// </summary>
        public void Exit()
        {
            if (_subscriptions != null)
            {
                // remove each subscription from our collection
                foreach (var subscription in Subscriptions)
                {
                    try
                    {
                        RemoveSubscription(subscription.Configuration);
                    }
                    catch (Exception err)
                    {
                        Log.Error(err, "Error removing: " + subscription.Configuration);
                    }
                }
            }

            if (_exchange != null) _exchange.Stop();
            if (_customExchange != null) _customExchange.Stop();

            Log.Trace("LiveTradingDataFeed.Exit(): Setting cancellation token...");
            _cancellationTokenSource.Cancel();

            if (_bridge != null) _bridge.Dispose();
        }

        /// <summary>
        /// Gets the <see cref="IDataQueueHandler"/> to use. By default this will try to load
        /// the type specified in the configuration via the 'data-queue-handler'
        /// </summary>
        /// <returns>The loaded <see cref="IDataQueueHandler"/></returns>
        protected virtual IDataQueueHandler GetDataQueueHandler()
        {
            return Composer.Instance.GetExportedValueByTypeName<IDataQueueHandler>(_job.DataQueueHandler);
        }

        /// <summary>
        /// Gets the <see cref="ITimeProvider"/> to use. By default this will load the
        /// <see cref="RealTimeProvider"/> which use's the system's <see cref="DateTime.UtcNow"/>
        /// for the current time
        /// </summary>
        /// <returns>he loaded <see cref="ITimeProvider"/></returns>
        protected virtual ITimeProvider GetTimeProvider()
        {
            return new RealTimeProvider();
        }

        /// <summary>
        /// Creates a new subscription for the specified security
        /// </summary>
        /// <param name="request">The subscription request</param>
        /// <returns>A new subscription instance of the specified security</returns>
        protected Subscription CreateSubscription(SubscriptionRequest request)
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

                    var factory = new LiveCustomDataSubscriptionEnumeratorFactory(_timeProvider);
                    var enumeratorStack = factory.CreateEnumerator(request, _dataProvider);

                    _customExchange.AddEnumerator(request.Configuration.Symbol, enumeratorStack);

                    var enqueable = new EnqueueableEnumerator<BaseData>();
                    _customExchange.SetDataHandler(request.Configuration.Symbol, data =>
                    {
                        enqueable.Enqueue(data);
                        if (SubscriptionShouldUpdateRealTimePrice(subscription, timeZoneOffsetProvider)) subscription.RealtimePrice = data.Value;
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
                            var quoteBarAggregator = new QuoteBarBuilderEnumerator(request.Configuration.Increment, request.Security.Exchange.TimeZone, _timeProvider);
                            _exchange.AddDataHandler(request.Configuration.Symbol, data =>
                            {
                                var tick = data as Tick;

                                if (tick.TickType == TickType.Quote)
                                {
                                    quoteBarAggregator.ProcessData(tick);
                                    if (SubscriptionShouldUpdateRealTimePrice(subscription, timeZoneOffsetProvider)) subscription.RealtimePrice = data.Value;
                                }
                            });
                            enumerator = quoteBarAggregator;
                            break;
                        case TickType.Trade:
                        default:
                            var tradeBarAggregator = new TradeBarBuilderEnumerator(request.Configuration.Increment, request.Security.Exchange.TimeZone, _timeProvider);
                            _exchange.AddDataHandler(request.Configuration.Symbol, data =>
                            {
                                var tick = data as Tick;

                                if (tick.TickType == TickType.Trade)
                                {
                                    tradeBarAggregator.ProcessData(tick);
                                    if (SubscriptionShouldUpdateRealTimePrice(subscription, timeZoneOffsetProvider)) subscription.RealtimePrice = data.Value;
                                }
                            });
                            enumerator = tradeBarAggregator;
                            break;
                        case TickType.OpenInterest:
                            var oiAggregator = new OpenInterestEnumerator(request.Configuration.Increment, request.Security.Exchange.TimeZone, _timeProvider);
                            _exchange.AddDataHandler(request.Configuration.Symbol, data =>
                            {
                                var tick = data as Tick;

                                if (tick.TickType == TickType.OpenInterest)
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
                    _exchange.SetDataHandler(request.Configuration.Symbol, data =>
                    {
                        tickEnumerator.Enqueue(data);
                        if (SubscriptionShouldUpdateRealTimePrice(subscription, timeZoneOffsetProvider)) subscription.RealtimePrice = data.Value;
                    });
                    enumerator = tickEnumerator;
                }

                if (request.Configuration.FillDataForward)
                {
                    var subscriptionConfigs = _subscriptions.Select(x => x.Configuration).Concat(new[] { request.Configuration });

                    UpdateFillForwardResolution(subscriptionConfigs);

                    enumerator = new LiveFillForwardEnumerator(_frontierTimeProvider, enumerator, request.Security.Exchange, _fillForwardResolution, request.Configuration.ExtendedMarketHours, localEndTime, request.Configuration.Increment, request.Configuration.DataTimeZone);
                }

                // define market hours and user filters to incoming data
                if (request.Configuration.IsFilteredSubscription)
                {
                    enumerator = new SubscriptionFilterEnumerator(enumerator, request.Security, localEndTime);
                }

                // finally, make our subscriptions aware of the frontier of the data feed, prevents future data from spewing into the feed
                enumerator = new FrontierAwareEnumerator(enumerator, _frontierTimeProvider, timeZoneOffsetProvider);

                var subscriptionDataEnumerator = SubscriptionData.Enumerator(request.Configuration, request.Security, timeZoneOffsetProvider, enumerator);
                subscription = new Subscription(request.Universe, request.Security, request.Configuration, subscriptionDataEnumerator, timeZoneOffsetProvider, request.StartTimeUtc, request.EndTimeUtc, false);
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

                        if (items == null || _frontierUtc == DateTime.MinValue) return;

                        var symbol = items.OfType<Symbol>().FirstOrDefault();
                        if (symbol == null) return;

                        var collection = new BaseDataCollection(_frontierUtc, symbol);
                        var changes = _universeSelection.ApplyUniverseSelection(userDefined, _frontierUtc, collection);
                        _algorithm.OnSecuritiesChanged(changes);
                    };
                }
            }
            else if (config.Type == typeof (CoarseFundamental))
            {
                Log.Trace("LiveTradingDataFeed.CreateUniverseSubscription(): Creating coarse universe: " + config.Symbol.ToString());

                // since we're binding to the data queue exchange we'll need to let him
                // know that we expect this data
                _dataQueueHandler.Subscribe(_job, new[] {request.Security.Symbol});

                var enqueable = new EnqueueableEnumerator<BaseData>();
                _exchange.SetDataHandler(config.Symbol, data =>
                {
                    enqueable.Enqueue(data);
                });
                enumerator = enqueable;
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

                    var subscriptionConfigs = _subscriptions.Select(x => x.Configuration).Concat(new[] { request.Configuration });

                    UpdateFillForwardResolution(subscriptionConfigs);

                    return new LiveFillForwardEnumerator(_frontierTimeProvider, input, request.Security.Exchange, _fillForwardResolution, request.Configuration.ExtendedMarketHours, localEndTime, request.Configuration.Increment, request.Configuration.DataTimeZone);
                };

                var symbolUniverse = _dataQueueHandler as IDataQueueUniverseProvider;
                if (symbolUniverse == null)
                {
                    throw new NotSupportedException("The DataQueueHandler does not support Options.");
                }

                var enumeratorFactory = new OptionChainUniverseSubscriptionEnumeratorFactory(configure, symbolUniverse, _timeProvider);
                enumerator = enumeratorFactory.CreateEnumerator(request, _dataProvider);

                enumerator = new FrontierAwareEnumerator(enumerator, _frontierTimeProvider, tzOffsetProvider);
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

                enumerator = new FrontierAwareEnumerator(enumerator, _frontierTimeProvider, tzOffsetProvider);
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
            var subscriptionDataEnumerator = SubscriptionData.Enumerator(request.Configuration, request.Security, tzOffsetProvider, enumerator);
            var subscription = new Subscription(request.Universe, request.Security, config, subscriptionDataEnumerator, tzOffsetProvider, request.StartTimeUtc, request.EndTimeUtc, true);

            return subscription;
        }

                /// <summary>
        /// Checks if the subscription should update the RealTimePrice
        /// </summary>
        /// <param name="subscription">The <see cref="Subscription"/></param>
        /// <param name="timeZoneOffsetProvider">The <see cref="TimeZoneOffsetProvider"/> used to convert now into the timezone of the exchange</param>
        /// <returns>True if the subscription is not null and the exchange is open</returns>
        protected bool SubscriptionShouldUpdateRealTimePrice(Subscription subscription, TimeZoneOffsetProvider timeZoneOffsetProvider)
        {
            return subscription != null &&
                   subscription.Security.Exchange.Hours.IsOpen(
                       timeZoneOffsetProvider.ConvertFromUtc(_timeProvider.GetUtcNow()),
                       subscription.Security.IsExtendedMarketHours);
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
        /// Updates the fill forward resolution by checking all existing subscriptions and
        /// selecting the smallest resoluton not equal to tick
        /// </summary>
        private void UpdateFillForwardResolution()
        {
            UpdateFillForwardResolution(_subscriptions.Select(x => x.Configuration));
        }

        /// <summary>
        /// Updates the fill forward resolution by checking specified subscription configurations and
        /// selecting the smallest resoluton not equal to tick
        /// </summary>
        /// <param name="subscriptionConfigs">Subscription configurations list</param>
        private void UpdateFillForwardResolution(IEnumerable<SubscriptionDataConfig> subscriptionConfigs)
        {
            _fillForwardResolution.Value = GetFillForwardResolution(subscriptionConfigs);
        }

        /// <summary>
        /// Returns the fill forward resolution by checking specified subscription configurations and
        /// selecting the smallest resoluton not equal to tick
        /// </summary>
        /// <param name="subscriptionConfigs">Subscription configurations list</param>
        private TimeSpan GetFillForwardResolution(IEnumerable<SubscriptionDataConfig> subscriptionConfigs)
        {
            return subscriptionConfigs
                .Where(x => !x.IsInternalFeed)
                .Select(x => x.Resolution)
                .Where(x => x != Resolution.Tick)
                .DefaultIfEmpty(Resolution.Minute)
                .Min().ToTimeSpan();
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.Collections.Generic.IEnumerator`1"/> that can be used to iterate through the collection.
        /// </returns>
        /// <filterpriority>1</filterpriority>
        public IEnumerator<TimeSlice> GetEnumerator()
        {
            return _bridge.GetConsumingEnumerable(_cancellationTokenSource.Token).GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>
        /// An <see cref="T:System.Collections.IEnumerator"/> object that can be used to iterate through the collection.
        /// </returns>
        /// <filterpriority>2</filterpriority>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
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

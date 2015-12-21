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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using QuantConnect.Configuration;
using QuantConnect.Data;
using QuantConnect.Data.Auxiliary;
using QuantConnect.Data.Custom;
using QuantConnect.Data.Market;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine.DataFeeds.Enumerators;
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

        private Ref<TimeSpan> _fillForwardResolution;
        private IResultHandler _resultHandler;
        private IDataQueueHandler _dataQueueHandler;
        private BaseDataExchange _exchange;
        private BaseDataExchange _customExchange;
        private ConcurrentDictionary<Symbol, Subscription> _subscriptions;
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private BusyBlockingCollection<TimeSlice> _bridge;
        private UniverseSelection _universeSelection;
        private DateTime _frontierUtc;

        /// <summary>
        /// Gets all of the current subscriptions this data feed is processing
        /// </summary>
        public IEnumerable<Subscription> Subscriptions
        {
            get { return _subscriptions.Select(x => x.Value); }
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
        public void Initialize(IAlgorithm algorithm, AlgorithmNodePacket job, IResultHandler resultHandler, IMapFileProvider mapFileProvider, IFactorFileProvider factorFileProvider)
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

            _frontierTimeProvider = new ManualTimeProvider(_timeProvider.GetUtcNow());
            _customExchange = new BaseDataExchange("CustomDataExchange") {SleepInterval = 10};
            // sleep is controlled on this exchange via the GetNextTicksEnumerator
            _exchange = new BaseDataExchange("DataQueueExchange"){SleepInterval = 0};
            _exchange.AddEnumerator(DataQueueHandlerSymbol, GetNextTicksEnumerator());
            _subscriptions = new ConcurrentDictionary<Symbol, Subscription>();

            _bridge = new BusyBlockingCollection<TimeSlice>();
            _universeSelection = new UniverseSelection(this, algorithm, job.Controls);

            // run the exchanges
            Task.Run(() => _exchange.Start(_cancellationTokenSource.Token));
            Task.Run(() => _customExchange.Start(_cancellationTokenSource.Token));

            // this value will be modified via calls to AddSubscription/RemoveSubscription
            var ffres = Time.OneSecond;
            _fillForwardResolution = Ref.Create(() => ffres, v => ffres = v);

            ffres = ResolveFillForwardResolution(algorithm);

            // wire ourselves up to receive notifications when universes are added/removed
            var start = _timeProvider.GetUtcNow();
            algorithm.UniverseManager.CollectionChanged += (sender, args) =>
            {
                switch (args.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                        foreach (var universe in args.NewItems.OfType<Universe>())
                        {
                            _subscriptions[universe.Configuration.Symbol] = CreateUniverseSubscription(universe, start, Time.EndOfTime);
                        }
                        break;

                    case NotifyCollectionChangedAction.Remove:
                        foreach (var universe in args.OldItems.OfType<Universe>())
                        {
                            Subscription subscription;
                            if (_subscriptions.TryGetValue(universe.Configuration.Symbol, out subscription))
                            {
                                RemoveSubscription(subscription);
                            }
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
        /// <param name="universe">The universe the subscription is to be added to</param>
        /// <param name="security">The security to add a subscription for</param>
        /// <param name="utcStartTime">The start time of the subscription</param>
        /// <param name="utcEndTime">The end time of the subscription</param>
        /// <returns>True if the subscription was created and added successfully, false otherwise</returns>
        public bool AddSubscription(Universe universe, Security security, DateTime utcStartTime, DateTime utcEndTime)
        {
            // create and add the subscription to our collection
            var subscription = CreateSubscription(universe, security, utcStartTime, utcEndTime);
            
            // for some reason we couldn't create the subscription
            if (subscription == null)
            {
                Log.Trace("Unable to add subscription for: " + security.Symbol.ToString());
                return false;
            }

            Log.Trace("LiveTradingDataFeed.AddSubscription(): Added " + security.Symbol.ToString());

            _subscriptions[subscription.Security.Symbol] = subscription;

            // send the subscription for the new symbol through to the data queuehandler
            // unless it is custom data, custom data is retrieved using the same as backtest
            if (!subscription.Configuration.IsCustomData)
            {
                _dataQueueHandler.Subscribe(_job, new[] {security.Symbol});
            }

            // keep track of security changes, we emit these to the algorithm
            // as notifications, used in universe selection
            _changes += SecurityChanges.Added(security);

            // update our fill forward resolution setting
            _fillForwardResolution.Value = ResolveFillForwardResolution(_algorithm);

            return true;
        }

        /// <summary>
        /// Removes the subscription from the data feed, if it exists
        /// </summary>
        /// <param name="subscription">The subscription to be removed</param>
        /// <returns>True if the subscription was successfully removed, false otherwise</returns>
        public bool RemoveSubscription(Subscription subscription)
        {
            var security = subscription.Security;

            // remove the subscriptions
            if (subscription.Configuration.IsCustomData)
            {
                _customExchange.RemoveEnumerator(security.Symbol);
                _customExchange.RemoveDataHandler(security.Symbol);
            }
            else
            {
                _dataQueueHandler.Unsubscribe(_job, new[] {security.Symbol});
                _exchange.RemoveDataHandler(security.Symbol);
            }

            // remove the subscription from our collection
            Subscription sub;
            if (_subscriptions.TryRemove(subscription.Security.Symbol, out sub))
            {
                sub.Dispose();
            }
            else
            {
                return false;
            }

            Log.Trace("LiveTradingDataFeed.RemoveSubscription(): Removed " + security.Symbol.ToString());

            // keep track of security changes, we emit these to the algorithm
            // as notications, used in universe selection
            _changes += SecurityChanges.Removed(security);

            // update our fill forward resolution setting
            _fillForwardResolution.Value = ResolveFillForwardResolution(_algorithm);

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

                    var data = new List<KeyValuePair<Security, List<BaseData>>>();
                    foreach (var kvp in _subscriptions)
                    {
                        var subscription = kvp.Value;

                        var cache = new KeyValuePair<Security, List<BaseData>>(subscription.Security, new List<BaseData>());

                        // dequeue data that is time stamped at or before this frontier
                        while (subscription.MoveNext() && subscription.Current != null)
                        {
                            cache.Value.Add(subscription.Current);
                        }

                        // if we have data, add it to be added to the bridge
                        if (cache.Value.Count > 0) data.Add(cache);

                        // we have new universe data to select based on
                        if (subscription.IsUniverseSelectionSubscription && cache.Value.Count > 0)
                        {
                            var universe = subscription.Universe;

                            // always wait for other thread to sync up
                            if (!_bridge.WaitHandle.WaitOne(Timeout.Infinite, _cancellationTokenSource.Token))
                            {
                                break;
                            }

                            // assume that if the first item is a base data collection then the enumerator handled the aggregation,
                            // otherwise, load all the the data into a new collection instance
                            var collection = cache.Value[0] as BaseDataCollection ?? new BaseDataCollection(_frontierUtc, subscription.Configuration.Symbol, cache.Value);

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
            }
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
                foreach (var kvp in _subscriptions)
                {
                    try
                    {
                        RemoveSubscription(kvp.Value);
                    }
                    catch (Exception err)
                    {
                        Log.Error(err, "Error removing: " + kvp.Key.ToString());
                    }
                }
            }
            
            if (_exchange != null) _exchange.Stop();
            if (_customExchange != null) _customExchange.Stop();

            if (_cancellationTokenSource != null && !_cancellationTokenSource.IsCancellationRequested)
            {
                _cancellationTokenSource.Cancel();
                if (_bridge != null) _bridge.Dispose();
            }
        }

        /// <summary>
        /// Gets the <see cref="IDataQueueHandler"/> to use. By default this will try to load
        /// the type specified in the configuration via the 'data-queue-handler'
        /// </summary>
        /// <returns>The loaded <see cref="IDataQueueHandler"/></returns>
        protected virtual IDataQueueHandler GetDataQueueHandler()
        {
            return Composer.Instance.GetExportedValueByTypeName<IDataQueueHandler>(Config.Get("data-queue-handler", "LiveDataQueue"));
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
        /// <param name="universe"></param>
        /// <param name="security">The security to create a subscription for</param>
        /// <param name="utcStartTime">The start time of the subscription in UTC</param>
        /// <param name="utcEndTime">The end time of the subscription in UTC</param>
        /// <returns>A new subscription instance of the specified security</returns>
        protected Subscription CreateSubscription(Universe universe, Security security, DateTime utcStartTime, DateTime utcEndTime)
        {
            Subscription subscription = null;
            try
            {
                var config = security.SubscriptionDataConfig;
                var localEndTime = utcEndTime.ConvertFromUtc(security.Exchange.TimeZone);
                var timeZoneOffsetProvider = new TimeZoneOffsetProvider(security.Exchange.TimeZone, utcStartTime, utcEndTime);

                IEnumerator<BaseData> enumerator;
                if (config.IsCustomData)
                {
                    if (!Quandl.IsAuthCodeSet)
                    {
                        // we're not using the SubscriptionDataReader, so be sure to set the auth token here
                        Quandl.SetAuthCode(Config.Get("quandl-auth-token"));
                    }

                    // each time we exhaust we'll new up this enumerator stack
                    var refresher = new RefreshEnumerator<BaseData>(() =>
                    {
                        var sourceProvider = (BaseData)Activator.CreateInstance(config.Type);
                        var currentLocalDate = DateTime.UtcNow.ConvertFromUtc(security.Exchange.TimeZone).Date;
                        var factory = new BaseDataSubscriptionFactory(config, currentLocalDate, true);
                        var source = sourceProvider.GetSource(config, currentLocalDate, true);
                        var factoryReadEnumerator = factory.Read(source).GetEnumerator();
                        var maximumDataAge = TimeSpan.FromTicks(Math.Max(config.Increment.Ticks, TimeSpan.FromSeconds(5).Ticks));
                        var fastForward = new FastForwardEnumerator(factoryReadEnumerator, _timeProvider, security.Exchange.TimeZone, maximumDataAge);
                        return new FrontierAwareEnumerator(fastForward, _timeProvider, timeZoneOffsetProvider);
                    });

                    // rate limit the refreshing of the stack to the requested interval
                    var minimumTimeBetweenCalls = Math.Min(config.Increment.Ticks, TimeSpan.FromMinutes(30).Ticks);
                    var rateLimit = new RateLimitEnumerator(refresher, _timeProvider, TimeSpan.FromTicks(minimumTimeBetweenCalls));
                    _customExchange.AddEnumerator(config.Symbol, rateLimit);

                    var enqueable = new EnqueueableEnumerator<BaseData>();
                    _customExchange.SetDataHandler(config.Symbol, data =>
                    {
                        enqueable.Enqueue(data);
                        if (subscription != null) subscription.RealtimePrice = data.Value;
                    });
                    enumerator = enqueable;
                }
                else if (config.Resolution != Resolution.Tick)
                {
                    // this enumerator allows the exchange to pump ticks into the 'back' of the enumerator,
                    // and the time sync loop can pull aggregated trade bars off the front
                    var aggregator = new TradeBarBuilderEnumerator(config.Increment, security.Exchange.TimeZone, _timeProvider);
                    _exchange.SetDataHandler(config.Symbol, data =>
                    {
                        aggregator.ProcessData((Tick) data);
                        if (subscription != null) subscription.RealtimePrice = data.Value;
                    });
                    enumerator = aggregator;
                }
                else
                {
                    // tick subscriptions can pass right through
                    var tickEnumerator = new EnqueueableEnumerator<BaseData>();
                    _exchange.SetDataHandler(config.Symbol, data =>
                    {
                        tickEnumerator.Enqueue(data);
                        if (subscription != null) subscription.RealtimePrice = data.Value;
                    });
                    enumerator = tickEnumerator;
                }

                if (config.FillDataForward)
                {
                    // TODO : Properly resolve fill forward resolution like in FileSystemDataFeed (make considerations for universe-only)
                    enumerator = new LiveFillForwardEnumerator(_frontierTimeProvider, enumerator, security.Exchange, _fillForwardResolution, config.ExtendedMarketHours, localEndTime, config.Increment);
                }

                // define market hours and user filters to incoming data
                enumerator = new SubscriptionFilterEnumerator(enumerator, security, localEndTime);

                // finally, make our subscriptions aware of the frontier of the data feed, this will help
                enumerator = new FrontierAwareEnumerator(enumerator, _frontierTimeProvider, timeZoneOffsetProvider);


                subscription = new Subscription(universe, security, enumerator, timeZoneOffsetProvider, utcStartTime, utcEndTime, false);
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
        /// <param name="universe">The universe to add a subscription for</param>
        /// <param name="startTimeUtc">The start time of the subscription in utc</param>
        /// <param name="endTimeUtc">The end time of the subscription in utc</param>
        protected virtual Subscription CreateUniverseSubscription(Universe universe, DateTime startTimeUtc, DateTime endTimeUtc)
        {
            // TODO : Consider moving the creating of universe subscriptions to a separate, testable class

            // grab the relevant exchange hours
            var config = universe.Configuration;

            var marketHoursDatabase = MarketHoursDatabase.FromDataFolder();
            var exchangeHours = marketHoursDatabase.GetExchangeHours(config);

            // create a canonical security object
            var security = new Security(exchangeHours, config, universe.UniverseSettings.Leverage);
            var tzOffsetProvider = new TimeZoneOffsetProvider(security.Exchange.TimeZone, startTimeUtc, endTimeUtc);

            IEnumerator<BaseData> enumerator;
            
            var userDefined = universe as UserDefinedUniverse;
            if (userDefined != null)
            {
                Log.Trace("LiveTradingDataFeed.CreateUniverseSubscription(): Creating user defined universe: " + config.Symbol);

                // spoof a tick on the requested interval to trigger the universe selection function
                enumerator = userDefined.GetTriggerTimes(startTimeUtc, endTimeUtc, marketHoursDatabase)
                    .Select(dt => new Tick { Time = dt }).GetEnumerator();

                enumerator = new FrontierAwareEnumerator(enumerator, _timeProvider, tzOffsetProvider);

                var enqueueable = new EnqueueableEnumerator<BaseData>();
                _customExchange.AddEnumerator(new EnumeratorHandler(config.Symbol, enumerator, enqueueable));
                enumerator = enqueueable;
            }
            else if (config.Type == typeof (CoarseFundamental))
            {
                Log.Trace("LiveTradingDataFeed.CreateUniverseSubscription(): Creating coarse universe: " + config.Symbol);

                // since we're binding to the data queue exchange we'll need to let him
                // know that we expect this data
                _dataQueueHandler.Subscribe(_job, new[] {security.Symbol});

                var enqueable = new EnqueueableEnumerator<BaseData>();
                _exchange.SetDataHandler(config.Symbol, data =>
                {
                    enqueable.Enqueue(data);
                });
                enumerator = enqueable;
            }
            else
            {
                Log.Trace("LiveTradingDataFeed.CreateUniverseSubscription(): Creating custom universe: " + config.Symbol);

                // each time we exhaust we'll new up this enumerator stack
                var refresher = new RefreshEnumerator<BaseDataCollection>(() =>
                {
                    var sourceProvider = (BaseData)Activator.CreateInstance(config.Type);
                    var currentLocalDate = DateTime.UtcNow.ConvertFromUtc(security.Exchange.TimeZone).Date;
                    var factory = new BaseDataSubscriptionFactory(config, currentLocalDate, true);
                    var source = sourceProvider.GetSource(config, currentLocalDate, true);
                    var factorEnumerator = factory.Read(source).GetEnumerator();
                    var fastForward = new FastForwardEnumerator(factorEnumerator, _timeProvider, security.Exchange.TimeZone, config.Increment);
                    var frontierAware = new FrontierAwareEnumerator(fastForward, _frontierTimeProvider, tzOffsetProvider);
                    return new BaseDataCollectionAggregatorEnumerator(frontierAware, config.Symbol);
                });
                
                // rate limit the refreshing of the stack to the requested interval
                var minimumTimeBetweenCalls = Math.Min(config.Increment.Ticks, TimeSpan.FromMinutes(30).Ticks);
                var rateLimit = new RateLimitEnumerator(refresher, _timeProvider, TimeSpan.FromTicks(minimumTimeBetweenCalls));
                var enqueueable = new EnqueueableEnumerator<BaseData>();
                _customExchange.AddEnumerator(new EnumeratorHandler(config.Symbol, rateLimit, enqueueable));
                enumerator = enqueueable;
            }

            // create the subscription
            var subscription = new Subscription(universe, security, enumerator, tzOffsetProvider, startTimeUtc, endTimeUtc, true);

            return subscription;
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
        }

        private static TimeSpan ResolveFillForwardResolution(IAlgorithm algorithm)
        {
            return algorithm.SubscriptionManager.Subscriptions
                .Where(x => !x.IsInternalFeed)
                .Select(x => x.Resolution)
                .Union(algorithm.UniverseManager.Select(x => x.Value.UniverseSettings.Resolution))
                .Where(x => x != Resolution.Tick)
                .DefaultIfEmpty(Resolution.Second)
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

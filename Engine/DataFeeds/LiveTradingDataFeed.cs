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
using System.Threading;
using QuantConnect.Configuration;
using QuantConnect.Data;
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
        private TimeSpan _emitRoundingInterval = Time.OneSecond;

        private SecurityChanges _changes = SecurityChanges.None;

        private LiveNodePacket _job;
        private IAlgorithm _algorithm;
        // used to get current time
        private ITimeProvider _timeProvider;
        // used to keep time constant during a time sync iteration
        private ManualTimeProvider _frontierTimeProvider;
            
        private IResultHandler _resultHandler;
        private IDataQueueHandler _dataQueueHandler;
        private BaseDataExchange _exchange;
        private BaseDataExchange _customExchange;
        private ConcurrentDictionary<SymbolSecurityType, Subscription> _subscriptions;
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        /// <summary>
        /// Event fired when the data feed encounters new fundamental data.
        /// This event must be fired when there is nothing in the <see cref="IDataFeed.Bridge"/>,
        /// this can be accomplished using <see cref="BusyBlockingCollection{T}.Wait(int,CancellationToken)"/>
        /// </summary>
        public event EventHandler<UniverseSelectionEventArgs> UniverseSelection;

        /// <summary>
        /// Gets all of the current subscriptions this data feed is processing
        /// </summary>
        public IEnumerable<Subscription> Subscriptions
        {
            get { return _subscriptions.Select(x => x.Value); }
        }

        /// <summary>
        /// Cross-threading queue so the datafeed pushes data into the queue and the primary algorithm thread reads it out.
        /// </summary>
        public BusyBlockingCollection<TimeSlice> Bridge
        {
            get; private set;
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
        public void Initialize(IAlgorithm algorithm, AlgorithmNodePacket job, IResultHandler resultHandler)
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
            _exchange = new BaseDataExchange("DataQueueExchange", GetNextTicksEnumerator());
            _subscriptions = new ConcurrentDictionary<SymbolSecurityType, Subscription>();

            Bridge = new BusyBlockingCollection<TimeSlice>();

            // run the exchanges
            _exchange.Start();
            _customExchange.Start();

            // add user defined subscriptions
            var start = _timeProvider.GetUtcNow();
            foreach (var kvp in _algorithm.Securities.OrderBy(x => x.Key.ToString()))
            {
                var security = kvp.Value;
                AddSubscription(security, start, Time.EndOfTime, true);
            }

            // add universe subscriptions
            foreach (var universe in _algorithm.Universes)
            {
                var subscription = CreateUniverseSubscription(universe, start, Time.EndOfTime);
                _subscriptions[new SymbolSecurityType(subscription)] = subscription;
            }
        }

        /// <summary>
        /// Adds a new subscription to provide data for the specified security.
        /// </summary>
        /// <param name="security">The security to add a subscription for</param>
        /// <param name="utcStartTime">The start time of the subscription</param>
        /// <param name="utcEndTime">The end time of the subscription</param>
        /// <param name="isUserDefinedSubscription">Set to true to prevent coarse universe selection from removing this subscription</param>
        public void AddSubscription(Security security, DateTime utcStartTime, DateTime utcEndTime, bool isUserDefinedSubscription)
        {
            // reduce the emit interval for tick data to 1ms, this affects time
            // rounding and thread sleep times
            if (security.SubscriptionDataConfig.Resolution == Resolution.Tick)
            {
                _emitRoundingInterval = Time.OneMillisecond;
            }

            // create and add the subscription to our collection
            var subscription = CreateSubscription(security, utcStartTime, utcEndTime, isUserDefinedSubscription);
            _subscriptions[new SymbolSecurityType(subscription)] = subscription;

            // send the subscription for the new symbol through to the data queuehandler
            // unless it is custom data, custom data is retrieved using the same as backtest
            if (!subscription.Configuration.IsCustomData)
            {
                _dataQueueHandler.Subscribe(_job, new Dictionary<SecurityType, List<string>>
                {
                    {security.Type, new List<string> {security.Symbol}}
                });
            }

            // keep track of security changes, we emit these to the algorithm
            // as notifications, used in universe selection
            _changes += SecurityChanges.Added(security);
        }

        /// <summary>
        /// Removes the subscription from the data feed, if it exists
        /// </summary>
        /// <param name="security">The security to remove subscriptions for</param>
        public void RemoveSubscription(Security security)
        {
            // check to see if we should increase the emit interval
            // when removing tick subscriptions
            var isTick = security.SubscriptionDataConfig.Resolution == Resolution.Tick;
            if (isTick && _subscriptions.All(x => x.Value.Configuration.Resolution != Resolution.Tick))
            {
                _emitRoundingInterval = Time.OneSecond;
            }

            // remove the subscription from our collection
            Subscription subscription;
            _subscriptions.TryRemove(new SymbolSecurityType(security), out subscription);
            _exchange.RemoveHandler(security.Symbol);

            // request to unsubscribe from the subscription
            if (!security.SubscriptionDataConfig.IsCustomData)
            {
                _dataQueueHandler.Unsubscribe(_job, new Dictionary<SecurityType, List<string>>
                {
                    {security.Type, new List<string> {security.Symbol}}
                });
            }

            // keep track of security changes, we emit these to the algorithm
            // as notications, used in universe selection
            _changes += SecurityChanges.Removed(security);
        }

        /// <summary>
        /// Primary entry point.
        /// </summary>
        public void Run()
        {
            IsActive = true;

            // we want to emit to the bridge minimally once a second since the data feed is
            // the heartbeat of the application, so this value will containg a second after
            // the last emit time, and if we pass this time, we'll emit even with no data
            var nextEmit = DateTime.MinValue;

            try
            {
                var lastTimeSliceEmitUtcTime = DateTime.MinValue;
                while (!_cancellationTokenSource.IsCancellationRequested)
                {
                    // perform sleeps to wake up on the second?
                    var frontier = _timeProvider.GetUtcNow();
                    _frontierTimeProvider.SetCurrentTime(frontier);

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
                            if (!Bridge.Wait(Timeout.Infinite, _cancellationTokenSource.Token))
                            {
                                break;
                            }

                            // fire the universe selection event
                            OnUniverseSelection(universe, subscription.Configuration, frontier, cache.Value);
                        }
                    }

                    // check for cancellation
                    if (_cancellationTokenSource.IsCancellationRequested) return;

                    // emit on data or if we've elapsed a full second since last emit
                    if (data.Count != 0 || frontier >= nextEmit)
                    {
                        var emitTime = frontier.RoundDown(_emitRoundingInterval);

                        // prevent emitting same time twice
                        if (data.Count != 0 || emitTime > lastTimeSliceEmitUtcTime)
                        {
                            Bridge.Add(TimeSlice.Create(emitTime, _algorithm.TimeZone, _algorithm.Portfolio.CashBook, data, _changes));

                            // force emitting every second
                            nextEmit = emitTime.RoundDown(Time.OneSecond).Add(Time.OneSecond);
                            lastTimeSliceEmitUtcTime = emitTime;
                        }
                    }

                    // reset our security changes
                    _changes = SecurityChanges.None;

                    if (_emitRoundingInterval <= Time.OneMillisecond)
                    {
                        // this is the case when we have tick subscriptions, we'll keep this code
                        // performant by shortcutting the sleep length logic
                        Thread.Sleep(1);
                    }
                    else
                    {
                        // determine how long to pause to the next rounded emit time, minimum of 1 ms pause to
                        // allow everyone else a chance to run
                        var currentTime = _timeProvider.GetUtcNow();
                        var nextWakeUpTime = currentTime.RoundDown(_emitRoundingInterval).Add(_emitRoundingInterval);
                        var millis = (int)Math.Round((nextWakeUpTime - currentTime).TotalMilliseconds);
                        Thread.Sleep(Math.Max(1, millis));
                    }
                }
            }
            catch (Exception err)
            {
                Log.Error(err);
            }
            IsActive = false;
        }

        /// <summary>
        /// External controller calls to signal a terminate of the thread.
        /// </summary>
        public void Exit()
        {
            // remove each subscription from our collection
            foreach (var kvp in _subscriptions)
            {
                try
                {
                    RemoveSubscription(kvp.Value.Security);
                }
                catch (Exception err)
                {
                    Log.Error(err, "Error removing: " + kvp.Key);
                }
            }
            if (!_cancellationTokenSource.IsCancellationRequested)
            {
                _cancellationTokenSource.Cancel();
                Bridge.Dispose();
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
        /// <param name="security">The security to create a subscription for</param>
        /// <param name="utcStartTime">The start time of the subscription in UTC</param>
        /// <param name="utcEndTime">The end time of the subscription in UTC</param>
        /// <param name="isUserDefinedSubscription">True for subscriptions manually added by user via AddSecurity</param>
        /// <returns>A new subscription instance of the specified security</returns>
        protected virtual Subscription CreateSubscription(Security security, DateTime utcStartTime, DateTime utcEndTime, bool isUserDefinedSubscription)
        {
            var config = security.SubscriptionDataConfig;
            var localStartTime = utcStartTime.ConvertFromUtc(config.TimeZone);
            var localEndTime = utcEndTime.ConvertFromUtc(config.TimeZone);

            IEnumerator<BaseData> enumerator;
            Subscription subscription = null;
            if (config.IsCustomData)
            {
                // custom data uses backtest readers
                var tradeableDates = Time.EachTradeableDay(security, localStartTime, localEndTime);
                var reader = new SubscriptionDataReader(config, localStartTime, localEndTime, _resultHandler, tradeableDates, true, false);

                // apply fast forwarding, this is especially important for RemoteFile types that
                // can send in large chunks of old, irrelevant data
                var fastForward = new FastForwardEnumerator(reader, _timeProvider, config.TimeZone, config.Increment);

                // apply rate limits (2x per increment, max 30 minutes between calls)
                // TODO : Pull limits from config file?
                var minimumTimeBetweenCalls = Math.Min((long)(config.Increment.Ticks / (double)2), TimeSpan.FromMinutes(30).Ticks);
                var rateLimit = new RateLimitEnumerator(fastForward, _timeProvider, TimeSpan.FromTicks(minimumTimeBetweenCalls));
                
                // add the enumerator to the exchange
                _customExchange.AddEnumerator(rateLimit);

                // this enumerator just allows the exchange to directly dump data into the 'back' of the enumerator
                var enqueable = new EnqueableEnumerator<BaseData>();
                _customExchange.SetHandler(config.Symbol, data =>
                {
                    if (security.DataFilter.Filter(security, data))
                    {
                        enqueable.Enqueue(data);
                        if (subscription != null) subscription.RealtimePrice = data.Value;
                    }
                });
                enumerator = enqueable;
            }
            else if (config.Resolution != Resolution.Tick)
            {
                // this enumerator allows the exchange to pump ticks into the 'back' of the enumerator,
                // and the time sync loop can pull aggregated trade bars off the front
                var aggregator = new TradeBarBuilderEnumerator(config.Increment, config.TimeZone, _timeProvider);
                _exchange.SetHandler(config.Symbol, data =>
                {
                    if (security.DataFilter.Filter(security, data))
                    {
                        aggregator.ProcessData((Tick) data);
                        if (subscription != null) subscription.RealtimePrice = data.Value;
                    }
                });
                enumerator = aggregator;
            }
            else
            {
                // tick subscriptions can pass right through
                var tickEnumerator = new EnqueableEnumerator<BaseData>(int.MaxValue);
                _exchange.SetHandler(config.Symbol, data =>
                {
                    if (security.DataFilter.Filter(security, data))
                    {
                        tickEnumerator.Enqueue(data);
                        if (subscription != null) subscription.RealtimePrice = data.Value;
                    }
                });
                enumerator = tickEnumerator;
            }

            if (config.FillDataForward)
            {
                // TODO : Properly resolve fill forward resolution like in FileSystemDataFeed (make considerations for universe-only)
                enumerator = new LiveFillForwardEnumerator(_timeProvider, enumerator, security.Exchange, config.Increment, config.ExtendedMarketHours, localEndTime, config.Increment);
            }

            // define market hours and user filters to incoming data
            enumerator = new SubscriptionFilterEnumerator(enumerator, security, localEndTime);

            // finally, make our subscriptions aware of the frontier of the data feed, this will help
            var timeZoneOffsetProvider = new TimeZoneOffsetProvider(security.SubscriptionDataConfig.TimeZone, utcStartTime, utcEndTime);
            enumerator = new FrontierAwareEnumerator(enumerator, _frontierTimeProvider, timeZoneOffsetProvider);


            subscription = new Subscription(security, enumerator, timeZoneOffsetProvider, utcStartTime, utcEndTime, isUserDefinedSubscription);

            return subscription;
        }

        /// <summary>
        /// Creates a new subscription for universe selection
        /// </summary>
        /// <param name="universe">The universe to add a subscription for</param>
        /// <param name="startTimeUtc">The start time of the subscription in utc</param>
        /// <param name="endTimeUtc">The end time of the subscription in utc</param>
        protected virtual Subscription CreateUniverseSubscription(
            IUniverse universe,
            DateTime startTimeUtc,
            DateTime endTimeUtc
            )
        {
            // grab the relevant exchange hours
            var config = universe.Configuration;

            var exchangeHours = SecurityExchangeHoursProvider.FromDataFolder()
                .GetExchangeHours(config.Market, null, config.SecurityType);

            // create a canonical security object
            var security = new Security(exchangeHours, config, universe.SubscriptionSettings.Leverage);

            IEnumerator<BaseData> enumerator;
            if (config.Type == typeof (CoarseFundamental))
            {
                var enqueable = new EnqueableEnumerator<BaseData>(int.MaxValue);
                _exchange.SetHandler(config.Symbol, dto =>
                {
                    var universeData = dto as BaseDataCollection;
                    if (universeData != null)
                    {
                        enqueable.Enqueue(universeData);
                    }
                });
                enumerator = enqueable;
            }
            else
            {
                var localStartTime = startTimeUtc.ConvertFromUtc(config.TimeZone);
                var localEndTime = endTimeUtc.ConvertFromUtc(config.TimeZone);

                // define our data enumerator
                var tradeableDates = Time.EachTradeableDay(security, localStartTime, localEndTime);
                var reader = new SubscriptionDataReader(config, localStartTime, localEndTime, _resultHandler, tradeableDates, true);
                _customExchange.AddEnumerator(reader);

                var enqueable = new EnqueableEnumerator<BaseData>();
                _customExchange.SetHandler(config.Symbol, data =>
                {
                    enqueable.Enqueue(data);
                });
                enumerator = enqueable;
            }

            // create the subscription
            var subscription = new Subscription(universe, security, enumerator, new TimeZoneOffsetProvider(security.SubscriptionDataConfig.TimeZone, startTimeUtc, endTimeUtc), startTimeUtc, endTimeUtc);

            return subscription;
        }

        protected virtual void OnUniverseSelection(IUniverse universe, SubscriptionDataConfig config, DateTime dateTimeUtc, IReadOnlyList<BaseData> data)
        {
            var handler = UniverseSelection;
            if (handler != null) handler(this, new UniverseSelectionEventArgs(universe, config, dateTimeUtc, data));
        }


        private IEnumerator<BaseData> GetNextTicksEnumerator()
        {
            while (!_cancellationTokenSource.IsCancellationRequested)
            {
                foreach (var data in _dataQueueHandler.GetNextTicks())
                {
                    yield return data;
                }
            }
        }
    }
}

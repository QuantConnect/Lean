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
using System.Threading.Tasks;
using QuantConnect.Data;
using QuantConnect.Data.Fundamental;
using QuantConnect.Interfaces;
using QuantConnect.Logging;
using QuantConnect.Data.Market;
using QuantConnect.Lean.Engine.Results;
using QuantConnect.Packets;
using QuantConnect.Securities;
using QuantConnect.Util;

namespace QuantConnect.Lean.Engine.DataFeeds
{
    /// <summary>
    /// Live Data Feed Streamed From QC Source.
    /// </summary>
    public class LiveTradingDataFeed : IDataFeed
    {
        private LiveNodePacket _job;
        private bool _endOfBridges;
        private bool _isActive;
        private IAlgorithm _algorithm;
        private IDataQueueHandler _dataQueue;
        private IResultHandler _resultHandler;
        private UniverseSelection _universeSelection;
        private ConcurrentDictionary<SymbolSecurityType, LiveSubscription> _subscriptions;
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

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
            get { return _isActive; }
        }

        /// <summary>
        /// Live trading datafeed handler provides a base implementation of a live trading datafeed. Derived types
        /// need only implement the GetNextTicks() function to return unprocessed ticks from a data source.
        /// This creates a new data feed with a DataFeedEndpoint of LiveTrading.
        /// </summary>
        public void Initialize(IAlgorithm algorithm, AlgorithmNodePacket job, IResultHandler resultHandler)
        {
            if (!(job is LiveNodePacket))
            {
                throw new ArgumentException("The LiveTradingDataFeed requires a LiveNodePacket.");
            }
            _job = (LiveNodePacket)job;

            _isActive = true;
            _algorithm = algorithm;
            _resultHandler = resultHandler;
            _cancellationTokenSource = new CancellationTokenSource();
            _universeSelection = new UniverseSelection(this, algorithm, true);
            _dataQueue = Composer.Instance.GetExportedValueByTypeName<IDataQueueHandler>(Configuration.Config.Get("data-queue-handler", "LiveDataQueue"));
            
            Bridge = new BusyBlockingCollection<TimeSlice>();
            _subscriptions = new ConcurrentDictionary<SymbolSecurityType, LiveSubscription>();

            var periodStart = DateTime.UtcNow.ConvertFromUtc(algorithm.TimeZone).AddDays(-7);
            var periodEnd = Time.EndOfTime;
            foreach (var security in algorithm.Securities.Values)
            {
                var subscription = CreateSubscription(algorithm, resultHandler, security, periodStart, periodEnd);
                _subscriptions.AddOrUpdate(new SymbolSecurityType(subscription),  subscription);
            }

            // request for data from these symbols
            var symbols = BuildTypeSymbolList(algorithm.Securities.Values);
            if (symbols.Any())
            {
                // don't subscribe if there's nothing there, this allows custom data to
                // work without an IDataQueueHandler implementation by specifying LiveDataQueue
                // in the configuration, that implementation throws on every method, but we actually
                // don't need it if we're only doing custom data
                _dataQueue.Subscribe(_job, symbols);
            }
        }

        /// <summary>
        /// Adds a new subscription to provide data for the specified security.
        /// </summary>
        /// <param name="security">The security to add a subscription for</param>
        /// <param name="utcStartTime">The start time of the subscription</param>
        /// <param name="utcEndTime">The end time of the subscription</param>
        public void AddSubscription(Security security, DateTime utcStartTime, DateTime utcEndTime)
        {
            var symbols = BuildTypeSymbolList(new[] {security});
            _dataQueue.Subscribe(_job, symbols);
            var subscription = CreateSubscription(_algorithm, _resultHandler, security, DateTime.UtcNow.ConvertFromUtc(_algorithm.TimeZone).Date, Time.EndOfTime);
            _subscriptions.AddOrUpdate(new SymbolSecurityType(subscription), subscription);
        }

        /// <summary>
        /// Removes the subscription from the data feed, if it exists
        /// </summary>
        /// <param name="security">The security to remove subscriptions for</param>
        public void RemoveSubscription(Security security)
        {
            var symbols = BuildTypeSymbolList(new[] {security});
            _dataQueue.Unsubscribe(_job, symbols);

            LiveSubscription subscription;
            _subscriptions.TryRemove(new SymbolSecurityType(security), out subscription);
        }

        /// <summary>
        /// Execute the primary thread for retrieving stock data.
        /// 1. Subscribe to the streams requested.
        /// 2. Build bars or tick data requested, primary loop increment smallest possible.
        /// </summary>
        public void Run()
        {
            //Initialize:

            // Set up separate thread to handle stream and building packets:
            var streamThread = new Thread(StreamStoreConsumer);
            streamThread.Start();
            Thread.Sleep(5); // Wait a little for the other thread to init.

            // This thread converts data into bars "on" the second - assuring the bars are close as 
            // possible to a second unit tradebar (starting at 0 milliseconds).
            var realtime = new RealTimeSynchronizedTimer(TimeSpan.FromSeconds(1), utcTriggerTime =>
            {
                // determine if we're on even time boundaries for data emit
                var onMinute = utcTriggerTime.Second == 0;
                var onHour = onMinute && utcTriggerTime.Minute == 0;

                // Determine if this subscription needs to be archived:
                var items = new List<KeyValuePair<Security, List<BaseData>>>();

                var changes = SecurityChanges.None;

                var performedUniverseSelection = new HashSet<string>();
                foreach (var kvp in _subscriptions)
                {
                    var subscription = kvp.Value;

                    if (subscription.Configuration.Resolution == Resolution.Tick) continue;

                    var localTime = new DateTime(utcTriggerTime.Ticks - subscription.OffsetProvider.GetOffsetTicks(utcTriggerTime));
                    var onDay = onHour && localTime.Hour == 0;

                    // perform universe selection if requested on day changes (don't perform multiple times per market)
                    if (onDay && _algorithm.Universe != null && !performedUniverseSelection.Contains(subscription.Configuration.Symbol))
                    {
                        performedUniverseSelection.Add(subscription.Configuration.Symbol);
                        var coarse = UniverseSelection.GetCoarseFundamentals(subscription.Configuration.Market, subscription.TimeZone, localTime.Date, true);
                        changes = _universeSelection.ApplyUniverseSelection(localTime.Date, coarse);
                    }

                    var triggerArchive = false;
                    switch (subscription.Configuration.Resolution)
                    {
                        case Resolution.Second:
                            triggerArchive = true;
                            break;
                        case Resolution.Minute:
                            triggerArchive = onMinute;
                            break;
                        case Resolution.Hour:
                            triggerArchive = onHour;
                            break;
                        case Resolution.Daily:
                            triggerArchive = onDay;
                            break;
                    }

                    if (triggerArchive)
                    {
                        subscription.StreamStore.TriggerArchive(utcTriggerTime, subscription.Configuration.FillDataForward);

                        BaseData data;
                        var dataPoints = new List<BaseData>();
                        while (subscription.StreamStore.Queue.TryDequeue(out data))
                        {
                            dataPoints.Add(data);
                        }
                        items.Add(new KeyValuePair<Security, List<BaseData>>(subscription.Security, dataPoints));
                    }
                }

                // don't try to add if we're already cancelling
                if (_cancellationTokenSource.IsCancellationRequested) return;
                Bridge.Add(TimeSlice.Create(_algorithm, utcTriggerTime, items, changes));
            });

            //Start the realtime sampler above
            realtime.Start();

            while (!_cancellationTokenSource.IsCancellationRequested && !_endOfBridges)
            {
                // main work of this class is done in the realtime and stream store consumer threads
                Thread.Sleep(1000);
            }

            //Dispose of the realtime clock.
            realtime.Stop();

            //Stop thread
            _isActive = false;

            //Exit Live DataStream Feed:
            Log.Trace("LiveTradingDataFeed.Run(): Exiting LiveTradingDataFeed Run Method");
        }

        /// <summary>
        /// Stream Store Consumer uses the GetNextTicks() function to get current ticks from a data source and
        /// then uses the stream store to compile them into trade bars.
        /// </summary>
        public void StreamStoreConsumer()
        {
            //Scan for the required time period to stream:
            Log.Trace("LiveTradingDataFeed.Stream(): Waiting for updated market hours...", true);

            var symbols = (from security in _algorithm.Securities.Values
                           where !security.IsDynamicallyLoadedData && (security.Type == SecurityType.Equity || security.Type == SecurityType.Forex)
                           select security.Symbol).ToList<string>();

            Log.Trace("LiveTradingDataFeed.Stream(): Market open, starting stream for " + string.Join(",", symbols));

            //Micro-thread for polling for new data from data source:
            var liveThreadTask = new Task(()=> 
            {
                if (_subscriptions.All(x => x.Value.IsCustomData))
                {
                    // if we're all custom data data don't waste CPU cycle with this thread
                    return;
                }

                //Blocking ForEach - Should stay within this loop as long as there is a data-connection
                while (true)
                {
                    var dataCollection = _dataQueue.GetNextTicks();

                    int ticksCount = 0;
                    foreach (var point in dataCollection)
                    {
                        ticksCount++;

                        foreach (var kvp in _subscriptions)
                        {
                            var subscription = kvp.Value;

                            if (subscription.Configuration.Symbol != point.Symbol) continue;

                            var tick = point as Tick;
                            if (tick != null)
                            {
                                // Update the realtime price stream value
                                subscription.SetRealtimePrice(point.Value);

                                if (subscription.Configuration.Resolution == Resolution.Tick)
                                {
                                    // put ticks directly into the bridge
                                    AddSingleItemToBridge(subscription, tick);
                                }
                                else
                                {
                                    // Update our internal counter
                                    subscription.StreamStore.Update(tick);
                                }
                            }
                            else
                            {
                                // reset the start time so it goes in sync with other data
                                point.Time = DateTime.UtcNow.ConvertFromUtc(subscription.TimeZone).RoundDown(subscription.Configuration.Increment);

                                //If its not a tick, inject directly into bridge for this symbol:
                                //Bridge[i].Enqueue(new List<BaseData> {point});
                                AddSingleItemToBridge(subscription, point);
                            }
                        }
                    }

                    if (_cancellationTokenSource.IsCancellationRequested) return;
                    if (ticksCount == 0) Thread.Sleep(5);
                }
            }, TaskCreationOptions.LongRunning);

            // Micro-thread for custom data/feeds. This only supports polling at this time. todo: Custom data sockets
            var customFeedsTask = new Task(() =>
            {
                while(true)
                {
                    foreach (var kvp in _subscriptions)
                    {
                        var subscription = kvp.Value;

                        // custom only thread
                        if (!subscription.IsCustomData) continue;
                        // wait for when it's time to update
                        if (!subscription.NeedsUpdate) continue;

                        var repeat = true;
                        BaseData data = null;
                        while (repeat && TryMoveNext(subscription, out data))
                        {
                            if (data == null)
                            {
                                break;
                            }

                            // check to see if the data is too far in the past
                            // this is useful when using custom remote files that may stretch far into the past,
                            // so this if block will cause us to fast forward the reader until its recent increment
                            var earliestExpectedFirstPoint = DateTime.UtcNow.Subtract(subscription.Configuration.Increment.Add(Time.OneSecond));
                            repeat = data.EndTime.ConvertToUtc(subscription.TimeZone) < earliestExpectedFirstPoint;
                        }

                        if (data == null)
                        {
                            continue;
                        }

                        // don't emit data in the future
                        // TODO : Move this concern into LiveSubscription, maybe a CustomLiveSubscription, end goal just enumerate the damn thing at it works
                        if (data.EndTime.ConvertToUtc(subscription.TimeZone) < DateTime.UtcNow)
                        {
                            if (subscription.Configuration.Resolution == Resolution.Tick)
                            {
                                // put ticks directly into the bridge
                                AddSingleItemToBridge(subscription, data);
                            }
                            else
                            {
                                Log.Trace("LiveTradingDataFeed.Custom(): Add to stream store.");
                                subscription.StreamStore.Update(data); //Update bar builder.
                                subscription.SetRealtimePrice(data.Value); //Update realtime price value.
                                subscription.NeedsMoveNext = true;
                            }
                        }
                        else
                        {
                            // since this data is in the future and we didn't emit it,
                            // don't call MoveNext again and we'll keep performing time checks
                            // until its end time has passed and we can emit it into the bridge
                            subscription.NeedsMoveNext = false;
                        }
                    }

                    if (_cancellationTokenSource.IsCancellationRequested) return;
                    Thread.Sleep(10);
                }
            }, TaskCreationOptions.LongRunning);

            //Wait for micro-threads to break before continuing
            liveThreadTask.Start();

            // define what tasks we're going to wait on, we use a task from result in place of the custom task, just in case we never start it
            var tasks = new [] {liveThreadTask, Task.FromResult(1)};

            // if we have any dynamically loaded data, start the custom thread
            if (_subscriptions.Any(x => x.Value.IsCustomData))
            {
                //Start task and set it as the second one we want to monitor:
                customFeedsTask.Start();
                tasks[1] = customFeedsTask;
            }
                
            Task.WaitAll(tasks);

            //Once we're here the tasks have died, signal 
            if (!_cancellationTokenSource.IsCancellationRequested) _endOfBridges = true;

            Log.Trace(string.Format("LiveTradingDataFeed.Stream(): Stream Task Completed. Exit Signal: {0}", _cancellationTokenSource.IsCancellationRequested));
        }

        private static bool TryMoveNext(LiveSubscription subscription, out BaseData data)
        {
            data = null;
            if (subscription.NeedsMoveNext)
            {
                // if we didn't emit the previous value it's because it was in
                // the future, so don't call MoveNext, just perform the date range
                // checks below again

                // in live mode subscription reader will move next but return null for current since nothing is there
                if (!subscription.MoveNext())
                {
                    // we've exhaused this source for now, the only source that would do this is
                    // a remote file, let's wait for five minutes and check again to see if we can
                    // get another piece of data. sadly, in this case it does mean redownloading the
                    // entire file and reading through all the data before getting to the end to see
                    // if there's a new line
                    subscription.SetNextUpdateTime(TimeSpan.FromMinutes(5));
                    return false;
                }

                // true success defined as if we got a non-null value
                if (subscription.Current == null)
                {
                    Log.Trace("LiveTradingDataFeed.Custom(): Current == null");
                    return false;
                }
            }

            // if we didn't get anything keep going
            data = subscription.Current;
            if (data == null)
            {
                // heuristically speaking this should already be true, but no harm in explicitly setting it
                subscription.NeedsMoveNext = true;
                return false;
            }
            return true;
        }

        /// <summary>
        /// Trigger the live trading datafeed thread to abort and stop looping.
        /// </summary>
        public void Exit()
        {
            // Unsubscribe from these symbols
            var symbols = BuildTypeSymbolList(_algorithm.Securities.Values);
            if (symbols.Any())
            {
                // don't unsubscribe if there's nothing there, this allows custom data to
                // work with the LiveDataQueue default LEAN implemetation that just throws on every method.
                _dataQueue.Unsubscribe(_job, symbols);
            }
            _cancellationTokenSource.Cancel();
            Bridge.Dispose();
        }

        private static LiveSubscription CreateSubscription(IAlgorithm algorithm,
            IResultHandler resultHandler,
            Security security,
            DateTime periodStart,
            DateTime periodEnd)
        {
            IEnumerator<BaseData> enumerator = null;
            if (security.IsDynamicallyLoadedData)
            {
                //Subscription managers for downloading user data:
                // TODO: Update this when warmup comes in, we back up so we can get data that should have emitted at midnight today
                var subscriptionDataReader = new SubscriptionDataReader(
                    security.SubscriptionDataConfig,
                    security,
                    periodStart, Time.EndOfTime,
                    resultHandler,
                    Time.EachTradeableDay(algorithm.Securities.Values, periodStart, periodEnd),
                    true,
                    DateTime.UtcNow.ConvertFromUtc(algorithm.TimeZone).Date
                    );

                // wrap the subscription data reader with a filter enumerator
                enumerator = SubscriptionFilterEnumerator.WrapForDataFeed(resultHandler, subscriptionDataReader, security, periodEnd);
            }
            return new LiveSubscription(security, enumerator, periodStart, periodEnd, true, false);
        }

        /// <summary>
        /// Create list of symbols grouped by security type.
        /// </summary>
        private Dictionary<SecurityType, List<string>> BuildTypeSymbolList(IEnumerable<Security> securities)
        {
            // create a lookup keyed by SecurityType
            var symbols = new Dictionary<SecurityType, List<string>>();

            // Only subscribe equities and forex symbols
            foreach (var security in securities)
            {
                if (security.Type == SecurityType.Equity || security.Type == SecurityType.Forex)
                {
                    if (!symbols.ContainsKey(security.Type)) symbols.Add(security.Type, new List<string>());
                    symbols[security.Type].Add(security.Symbol);
                }
            }
            return symbols;
        }

        private void AddSingleItemToBridge(Subscription subscription, BaseData tick)
        {
            // don't try to add if we're already cancelling
            if (_cancellationTokenSource.IsCancellationRequested) return;
            Bridge.Add(TimeSlice.Create(_algorithm, tick.EndTime.ConvertToUtc(subscription.TimeZone), new List<KeyValuePair<Security, List<BaseData>>>
            {
                new KeyValuePair<Security, List<BaseData>>(subscription.Security, new List<BaseData> {tick})
            }, SecurityChanges.None));
        }
    }
}
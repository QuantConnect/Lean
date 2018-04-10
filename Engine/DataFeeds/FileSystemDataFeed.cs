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
using QuantConnect.Data;
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
    /// Historical datafeed stream reader for processing files on a local disk.
    /// </summary>
    /// <remarks>Filesystem datafeeds are incredibly fast</remarks>
    public class FileSystemDataFeed : IDataFeed
    {
        private IAlgorithm _algorithm;
        private ParallelRunnerController _controller;
        private IResultHandler _resultHandler;
        private Ref<TimeSpan> _fillForwardResolution;
        private IMapFileProvider _mapFileProvider;
        private IFactorFileProvider _factorFileProvider;
        private IDataProvider _dataProvider;
        private SubscriptionCollection _subscriptions;
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private UniverseSelection _universeSelection;
        private SubscriptionDataReaderSubscriptionEnumeratorFactory _subscriptionfactory;

        /// <summary>
        /// Gets all of the current subscriptions this data feed is processing
        /// </summary>
        public IEnumerable<Subscription> Subscriptions
        {
            get { return _subscriptions; }
        }

        /// <summary>
        /// Flag indicating the hander thread is completely finished and ready to dispose.
        /// </summary>
        public bool IsActive { get; private set; }

        /// <summary>
        /// Initializes the data feed for the specified job and algorithm
        /// </summary>
        public void Initialize(IAlgorithm algorithm, AlgorithmNodePacket job, IResultHandler resultHandler, IMapFileProvider mapFileProvider, IFactorFileProvider factorFileProvider, IDataProvider dataProvider)
        {
            _algorithm = algorithm;
            _resultHandler = resultHandler;
            _mapFileProvider = mapFileProvider;
            _factorFileProvider = factorFileProvider;
            _dataProvider = dataProvider;
            _subscriptions = new SubscriptionCollection();
            _universeSelection = new UniverseSelection(this, algorithm);
            _cancellationTokenSource = new CancellationTokenSource();
            _subscriptionfactory = new SubscriptionDataReaderSubscriptionEnumeratorFactory(_resultHandler, _mapFileProvider, _factorFileProvider, _dataProvider, false, true);

            IsActive = true;
            var threadCount = Math.Max(1, Math.Min(4, Environment.ProcessorCount - 3));
            _controller = new ParallelRunnerController(threadCount);
            _controller.Start(_cancellationTokenSource.Token);

            var ffres = Time.OneMinute;
            _fillForwardResolution = Ref.Create(() => ffres, res => ffres = res);

            // wire ourselves up to receive notifications when universes are added/removed
            algorithm.UniverseManager.CollectionChanged += (sender, args) =>
            {
                switch (args.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                        foreach (var universe in args.NewItems.OfType<Universe>())
                        {
                            var config = universe.Configuration;
                            var start = _algorithm.UtcTime;

                            var marketHoursDatabase = MarketHoursDatabase.FromDataFolder();
                            var exchangeHours = marketHoursDatabase.GetExchangeHours(config);

                            Security security;
                            if (!_algorithm.Securities.TryGetValue(config.Symbol, out security))
                            {
                                // create a canonical security object if it doesn't exist
                                security = new Security(exchangeHours, config, _algorithm.Portfolio.CashBook[CashBook.AccountCurrency], SymbolProperties.GetDefault(CashBook.AccountCurrency));
                            }

                            var end = _algorithm.EndDate.ConvertToUtc(_algorithm.TimeZone);
                            AddSubscription(new SubscriptionRequest(true, universe, security, config, start, end));
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

        private Subscription CreateSubscription(SubscriptionRequest request)
        {
            // ReSharper disable once PossibleMultipleEnumeration
            if (!request.TradableDays.Any())
            {
                _algorithm.Error(string.Format("No data loaded for {0} because there were no tradeable dates for this security.", request.Security.Symbol));
                return null;
            }

            // ReSharper disable once PossibleMultipleEnumeration
            var enumeratorFactory = GetEnumeratorFactory(request);
            var enumerator = enumeratorFactory.CreateEnumerator(request, _dataProvider);
            enumerator = ConfigureEnumerator(request, false, enumerator);

            var enqueueable = new EnqueueableEnumerator<SubscriptionData>(true);
            var timeZoneOffsetProvider = new TimeZoneOffsetProvider(request.Security.Exchange.TimeZone, request.StartTimeUtc, request.EndTimeUtc);
            var subscription = new Subscription(request.Universe, request.Security, request.Configuration, enqueueable, timeZoneOffsetProvider, request.StartTimeUtc, request.EndTimeUtc, false);

            // add this enumerator to our exchange
            ScheduleEnumerator(subscription, enumerator, enqueueable, GetLowerThreshold(request.Configuration.Resolution), GetUpperThreshold(request.Configuration.Resolution));

            return subscription;
        }

        private void ScheduleEnumerator(Subscription subscription, IEnumerator<BaseData> enumerator, EnqueueableEnumerator<SubscriptionData> enqueueable,
            int lowerThreshold, int upperThreshold, int firstLoopCount = 5)
        {
            // schedule the work on the controller
            var security = subscription.Security;
            var configuration = subscription.Configuration;

            var firstLoop = true;
            FuncParallelRunnerWorkItem workItem = null;
            workItem = new FuncParallelRunnerWorkItem(() => enqueueable.Count < lowerThreshold, () =>
            {
                var count = 0;
                while (enumerator.MoveNext())
                {
                    // subscription has been removed, no need to continue enumerating
                    if (enqueueable.HasFinished)
                    {
                        enumerator.Dispose();
                        return;
                    }

                    var subscriptionData = SubscriptionData.Create(configuration, security.Exchange.Hours, subscription.OffsetProvider, enumerator.Current);

                    // drop the data into the back of the enqueueable
                    enqueueable.Enqueue(subscriptionData);

                    count++;

                    // special behavior for first loop to spool up quickly
                    if (firstLoop && count > firstLoopCount)
                    {
                        // there's more data in the enumerator, reschedule to run again
                        firstLoop = false;
                        _controller.Schedule(workItem);
                        return;
                    }

                    // stop executing if we've dequeued more than the lower threshold or have
                    // more total that upper threshold in the enqueueable's queue
                    if (count > lowerThreshold || enqueueable.Count > upperThreshold)
                    {
                        // there's more data in the enumerator, reschedule to run again
                        _controller.Schedule(workItem);
                        return;
                    }
                }

                // we made it here because MoveNext returned false, stop the enqueueable and don't reschedule
                enqueueable.Stop();
            });
            _controller.Schedule(workItem);
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

            var subscription = request.IsUniverseSubscription
                ? CreateUniverseSubscription(request)
                : CreateSubscription(request);

            if (subscription == null)
            {
                // subscription will be null when there's no tradeable dates for the security between the requested times, so
                // don't even try to load the data
                return false;
            }

            Log.Debug("FileSystemDataFeed.AddSubscription(): Added " + request.Configuration + " Start: " + request.StartTimeUtc + " End: " + request.EndTimeUtc);

            if (_subscriptions.TryAdd(subscription))
            {
                UpdateFillForwardResolution();
            }

            return true;
        }

        /// <summary>
        /// Removes the subscription from the data feed, if it exists
        /// </summary>
        /// <param name="configuration">The configuration of the subscription to remove</param>
        /// <returns>True if the subscription was successfully removed, false otherwise</returns>
        public bool RemoveSubscription(SubscriptionDataConfig configuration)
        {
            // remove the subscription from our collection, if it exists
            Subscription subscription;

            if (_subscriptions.TryGetValue(configuration, out subscription))
            {
                if (!_subscriptions.TryRemove(configuration, out subscription))
                {
                    Log.Error("FileSystemDataFeed.RemoveSubscription(): Unable to remove: " + configuration);
                    return false;
                }

                subscription.Dispose();
                Log.Debug("FileSystemDataFeed.RemoveSubscription(): Removed " + configuration);

                UpdateFillForwardResolution();
            }

            return true;
        }

        /// <summary>
        /// Main routine for datafeed analysis.
        /// </summary>
        /// <remarks>This is a hot-thread and should be kept extremely lean. Modify with caution.</remarks>
        public void Run()
        {
            try
            {
                _controller.WaitHandle.WaitOne();
            }
            catch (Exception err)
            {
                Log.Error("FileSystemDataFeed.Run(): Encountered an error: " + err.Message);
                if (!_cancellationTokenSource.IsCancellationRequested)
                {
                    _cancellationTokenSource.Cancel();
                }
            }
            finally
            {
                Log.Trace("FileSystemDataFeed.Run(): Ending Thread... ");
                if (_controller != null) _controller.Dispose();
                IsActive = false;
            }
        }

        private DateTime GetInitialFrontierTime()
        {
            var frontier = DateTime.MaxValue;
            foreach (var subscription in Subscriptions)
            {
                var current = subscription.Current;
                if (current == null)
                {
                    continue;
                }

                // we need to initialize both the frontier time and the offset provider, in order to do
                // this we'll first convert the current.EndTime to UTC time, this will allow us to correctly
                // determine the offset in ticks using the OffsetProvider, we can then use this to recompute
                // the UTC time. This seems odd, but is necessary given Noda time's lenient mapping, the
                // OffsetProvider exists to give forward marching mapping

                // compute the initial frontier time
                if (current.EmitTimeUtc < frontier)
                {
                    frontier = current.EmitTimeUtc;
                }
            }

            if (frontier == DateTime.MaxValue)
            {
                frontier = _algorithm.StartDate.ConvertToUtc(_algorithm.TimeZone);
            }
            return frontier;
        }

        /// <summary>
        /// Adds a new subscription for universe selection
        /// </summary>
        /// <param name="request">The subscription request</param>
        private Subscription CreateUniverseSubscription(SubscriptionRequest request)
        {
            // grab the relevant exchange hours
            var config = request.Configuration;

            // define our data enumerator
            var enumerator = GetEnumeratorFactory(request).CreateEnumerator(request, _dataProvider);

            var firstLoopCount = 5;
            var lowerThreshold = GetLowerThreshold(config.Resolution);
            var upperThreshold = GetUpperThreshold(config.Resolution);
            if (config.Type == typeof (CoarseFundamental))
            {
                firstLoopCount = 2;
                lowerThreshold = 5;
                upperThreshold = 100000;
            }

            var enqueueable = new EnqueueableEnumerator<SubscriptionData>(true);
            var timeZoneOffsetProvider = new TimeZoneOffsetProvider(request.Security.Exchange.TimeZone, request.StartTimeUtc, request.EndTimeUtc);
            var subscription = new Subscription(request.Universe, request.Security, config, enqueueable, timeZoneOffsetProvider, request.StartTimeUtc, request.EndTimeUtc, true);

            // add this enumerator to our exchange
            ScheduleEnumerator(subscription, enumerator, enqueueable, lowerThreshold, upperThreshold, firstLoopCount);

            return subscription;
        }

        /// <summary>
        /// Creates the correct enumerator factory for the given request
        /// </summary>
        private ISubscriptionEnumeratorFactory GetEnumeratorFactory(SubscriptionRequest request)
        {
            if (request.IsUniverseSubscription)
            {
                if (request.Universe is ITimeTriggeredUniverse)
                {
                    var universe = request.Universe as UserDefinedUniverse;
                    if (universe != null)
                    {
                        // Trigger universe selection when security added/removed after Initialize
                        universe.CollectionChanged += (sender, args) =>
                        {
                            var items =
                                args.Action == NotifyCollectionChangedAction.Add ? args.NewItems :
                                args.Action == NotifyCollectionChangedAction.Remove ? args.OldItems : null;

                            if (items == null) return;

                            var symbol = items.OfType<Symbol>().FirstOrDefault();
                            if (symbol == null) return;

                            var collection = new BaseDataCollection(_algorithm.UtcTime, symbol);
                            var changes = _universeSelection.ApplyUniverseSelection(universe, _algorithm.UtcTime, collection);
                            _algorithm.OnSecuritiesChanged(changes);
                        };
                    }

                    return new TimeTriggeredUniverseSubscriptionEnumeratorFactory(request.Universe as ITimeTriggeredUniverse, MarketHoursDatabase.FromDataFolder());
                }
                if (request.Configuration.Type == typeof (CoarseFundamental))
                {
                    return new BaseDataCollectionSubscriptionEnumeratorFactory();
                }
                if (request.Universe is OptionChainUniverse)
                {
                    return new OptionChainUniverseSubscriptionEnumeratorFactory((req, e) => ConfigureEnumerator(req, true, e),
                        _mapFileProvider.Get(request.Security.Symbol.ID.Market), _factorFileProvider);
                }
                if (request.Universe is FuturesChainUniverse)
                {
                    return new FuturesChainUniverseSubscriptionEnumeratorFactory((req, e) => ConfigureEnumerator(req, true, e));
                }
            }

            return _subscriptionfactory;
        }

        /// <summary>
        /// Send an exit signal to the thread.
        /// </summary>
        public void Exit()
        {
            Log.Trace("FileSystemDataFeed.Exit(): Exit triggered.");
            _cancellationTokenSource.Cancel();
        }

        /// <summary>
        /// Updates the fill forward resolution by checking all existing subscriptions and
        /// selecting the smallest resoluton not equal to tick
        /// </summary>
        private void UpdateFillForwardResolution()
        {
            UpdateFillForwardResolution(_subscriptions.Select( x => x.Configuration ));
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
            // compute initial frontier time
            var frontierUtc = GetInitialFrontierTime();
            Log.Trace(string.Format("FileSystemDataFeed.GetEnumerator(): Begin: {0} UTC", frontierUtc));

            var syncer = new SubscriptionSynchronizer(_universeSelection, _algorithm.TimeZone, _algorithm.Portfolio.CashBook, frontierUtc);
            syncer.SubscriptionFinished += (sender, subscription) =>
            {
                RemoveSubscription(subscription.Configuration);
                Log.Debug(string.Format("FileSystemDataFeed.GetEnumerator(): Finished subscription: {0} at {1} UTC", subscription.Configuration, _algorithm.UtcTime));
            };

            while (!_cancellationTokenSource.IsCancellationRequested)
            {
                TimeSlice timeSlice;

                try
                {
                    timeSlice = syncer.Sync(Subscriptions);
                }
                catch (Exception err)
                {
                    Log.Error(err);

                    // notify the algorithm about the error, so it can be reported to the user
                    _algorithm.RunTimeError = err;
                    _algorithm.Status = AlgorithmStatus.RuntimeError;

                    break;
                }

                // syncer returns MaxValue on failure/end of data
                if (timeSlice.Time != DateTime.MaxValue)
                {
                    yield return timeSlice;
                }
                else if (timeSlice.SecurityChanges == SecurityChanges.None)
                {
                    // there's no more data to pull off, we're done (frontier is max value and no security changes)
                    break;
                }
            }

            //Close up all streams:
            foreach (var subscription in Subscriptions)
            {
                subscription.Dispose();
            }

            if (_subscriptionfactory != null)
                _subscriptionfactory.Dispose();

            Log.Trace(string.Format("FileSystemDataFeed.Run(): Data Feed Completed at {0} UTC", _algorithm.UtcTime));
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
        /// Configure the enumerator with aggregation/fill-forward/filter behaviors. Returns new instance if re-configured
        /// </summary>
        private IEnumerator<BaseData> ConfigureEnumerator(SubscriptionRequest request, bool aggregate, IEnumerator<BaseData> enumerator)
        {
            if (aggregate)
            {
                enumerator = new BaseDataCollectionAggregatorEnumerator(enumerator, request.Configuration.Symbol);
            }

            // optionally apply fill forward logic, but never for tick data
            if (request.Configuration.FillDataForward && request.Configuration.Resolution != Resolution.Tick)
            {
                // copy forward Bid/Ask bars for QuoteBars
                if (request.Configuration.Type == typeof(QuoteBar))
                {
                    enumerator = new QuoteBarFillForwardEnumerator(enumerator);
                }

                var subscriptionConfigs = _subscriptions.Select(x => x.Configuration).Concat(new[] { request.Configuration });

                UpdateFillForwardResolution(subscriptionConfigs);

                enumerator = new FillForwardEnumerator(enumerator, request.Security.Exchange, _fillForwardResolution,
                    request.Security.IsExtendedMarketHours, request.EndTimeLocal, request.Configuration.Resolution.ToTimeSpan(), request.Configuration.DataTimeZone);
            }

            // optionally apply exchange/user filters
            if (request.Configuration.IsFilteredSubscription)
            {
                enumerator = SubscriptionFilterEnumerator.WrapForDataFeed(_resultHandler, enumerator, request.Security, request.EndTimeLocal);
            }

            return enumerator;
        }

        private static int GetLowerThreshold(Resolution resolution)
        {
            switch (resolution)
            {
                case Resolution.Tick:
                    return 500;

                case Resolution.Second:
                case Resolution.Minute:
                case Resolution.Hour:
                case Resolution.Daily:
                    return 250;

                default:
                    throw new ArgumentOutOfRangeException("resolution", resolution, null);
            }
        }

        private static int GetUpperThreshold(Resolution resolution)
        {
            switch (resolution)
            {
                case Resolution.Tick:
                    return 10000;

                case Resolution.Second:
                case Resolution.Minute:
                case Resolution.Hour:
                case Resolution.Daily:
                    return 5000;

                default:
                    throw new ArgumentOutOfRangeException("resolution", resolution, null);
            }
        }
    }
}

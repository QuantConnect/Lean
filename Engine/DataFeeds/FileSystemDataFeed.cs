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
        private IResultHandler _resultHandler;
        private IMapFileProvider _mapFileProvider;
        private IFactorFileProvider _factorFileProvider;
        private IDataProvider _dataProvider;
        private SubscriptionCollection _subscriptions;
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private UniverseSelection _universeSelection;
        private SubscriptionDataReaderSubscriptionEnumeratorFactory _subscriptionFactory;

        /// <summary>
        /// Flag indicating the hander thread is completely finished and ready to dispose.
        /// </summary>
        public bool IsActive { get; private set; }

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
            _algorithm = algorithm;
            _resultHandler = resultHandler;
            _mapFileProvider = mapFileProvider;
            _factorFileProvider = factorFileProvider;
            _dataProvider = dataProvider;
            _subscriptions = subscriptionManager.DataFeedSubscriptions;
            _universeSelection = subscriptionManager.UniverseSelection;
            _cancellationTokenSource = new CancellationTokenSource();
            _subscriptionFactory = new SubscriptionDataReaderSubscriptionEnumeratorFactory(
                _resultHandler,
                _mapFileProvider,
                _factorFileProvider,
                _dataProvider,
                includeAuxiliaryData: true);

            IsActive = true;
            var threadCount = Math.Max(1, Math.Min(4, Environment.ProcessorCount - 3));
        }

        private Subscription CreateDataSubscription(SubscriptionRequest request)
        {
            // ReSharper disable once PossibleMultipleEnumeration
            if (!request.TradableDays.Any())
            {
                _algorithm.Error(
                    $"No data loaded for {request.Security.Symbol} because there were no tradeable dates for this security."
                );
                return null;
            }

            // ReSharper disable once PossibleMultipleEnumeration
            var enumeratorFactory = GetEnumeratorFactory(request);
            var enumerator = enumeratorFactory.CreateEnumerator(request, _dataProvider);
            enumerator = ConfigureEnumerator(request, false, enumerator);

            var enqueueable = new EnqueueableEnumerator<SubscriptionData>(true);
            var timeZoneOffsetProvider = new TimeZoneOffsetProvider(request.Security.Exchange.TimeZone, request.StartTimeUtc, request.EndTimeUtc);
            var subscription = new Subscription(request, enqueueable, timeZoneOffsetProvider);

            // add this enumerator to our exchange
            ScheduleEnumerator(subscription,
                enumerator,
                enqueueable,
                GetLowerThreshold(request.Configuration.Resolution),
                GetUpperThreshold(request.Configuration.Resolution),
                request.Security.Exchange.Hours);

            return subscription;
        }

        private void ScheduleEnumerator(Subscription subscription, IEnumerator<BaseData> enumerator, EnqueueableEnumerator<SubscriptionData> enqueueable,
            int lowerThreshold, int upperThreshold, SecurityExchangeHours exchangeHours, int firstLoopCount = 5)
        {
            Action produce = () =>
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

                    var subscriptionData = SubscriptionData.Create(subscription.Configuration, exchangeHours, subscription.OffsetProvider, enumerator.Current);

                    // drop the data into the back of the enqueueable
                    enqueueable.Enqueue(subscriptionData);

                    count++;

                    // stop executing if we have more data than the upper threshold in the enqueueable
                    if (count > upperThreshold)
                    {
                        // we use local count for the outside if, for performance, and adjust here
                        count = enqueueable.Count;
                        if (count > upperThreshold)
                        {
                            return;
                        }
                    }
                }

                // we made it here because MoveNext returned false, stop the enqueueable
                enqueueable.Stop();
            };

            enqueueable.SetProducer(produce, lowerThreshold);
        }

        /// <summary>
        /// Creates a new subscription to provide data for the specified security.
        /// </summary>
        /// <param name="request">Defines the subscription to be added, including start/end times the universe and security</param>
        /// <returns>The created <see cref="Subscription"/> if successful, null otherwise</returns>
        public Subscription CreateSubscription(SubscriptionRequest request)
        {
            return request.IsUniverseSubscription
                ? CreateUniverseSubscription(request)
                : CreateDataSubscription(request);
        }

        /// <summary>
        /// Removes the subscription from the data feed, if it exists
        /// </summary>
        /// <param name="subscription">The subscription to remove</param>
        public void RemoveSubscription(Subscription subscription)
        {
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
                // the lower threshold will be when we start the worker again, if he is stopped
                lowerThreshold = 200;
                // the upper threshold will stop the worker from loading more data. This is roughly 1 GB
                upperThreshold = 500;
            }

            var enqueueable = new EnqueueableEnumerator<SubscriptionData>(true);
            var timeZoneOffsetProvider = new TimeZoneOffsetProvider(request.Security.Exchange.TimeZone, request.StartTimeUtc, request.EndTimeUtc);
            var subscription = new Subscription(request, enqueueable, timeZoneOffsetProvider);

            // add this enumerator to our exchange
            ScheduleEnumerator(subscription, enumerator, enqueueable, lowerThreshold, upperThreshold, request.Security.Exchange.Hours, firstLoopCount);

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

            return _subscriptionFactory;
        }

        /// <summary>
        /// Send an exit signal to the thread.
        /// </summary>
        public void Exit()
        {
            if (IsActive)
            {
                IsActive = false;
                Log.Trace("FileSystemDataFeed.Exit(): Start. Setting cancellation token...");
                _cancellationTokenSource.Cancel();
                _subscriptionFactory?.DisposeSafely();
                Log.Trace("FileSystemDataFeed.Exit(): Exit Finished.");
            }
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

                var fillForwardResolution = _subscriptions.UpdateAndGetFillForwardResolution(request.Configuration);

                enumerator = new FillForwardEnumerator(enumerator, request.Security.Exchange, fillForwardResolution,
                    request.Security.IsExtendedMarketHours, request.EndTimeLocal, request.Configuration.Resolution.ToTimeSpan(), request.Configuration.DataTimeZone, request.StartTimeLocal);
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

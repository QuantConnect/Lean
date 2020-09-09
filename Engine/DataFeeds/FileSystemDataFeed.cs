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

using System.Collections.Generic;
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
        private ITimeProvider _timeProvider;
        private IResultHandler _resultHandler;
        private IMapFileProvider _mapFileProvider;
        private IFactorFileProvider _factorFileProvider;
        private IDataProvider _dataProvider;
        private SubscriptionCollection _subscriptions;
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
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
            IDataFeedTimeProvider dataFeedTimeProvider,
            IDataChannelProvider dataChannelProvider)
        {
            _algorithm = algorithm;
            _resultHandler = resultHandler;
            _mapFileProvider = mapFileProvider;
            _factorFileProvider = factorFileProvider;
            _dataProvider = dataProvider;
            _timeProvider = dataFeedTimeProvider.FrontierTimeProvider;
            _subscriptions = subscriptionManager.DataFeedSubscriptions;
            _cancellationTokenSource = new CancellationTokenSource();
            _subscriptionFactory = new SubscriptionDataReaderSubscriptionEnumeratorFactory(
                _resultHandler,
                _mapFileProvider,
                _factorFileProvider,
                _dataProvider,
                includeAuxiliaryData: true,
                enablePriceScaling: false);

            IsActive = true;
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
            var enumerator = _subscriptionFactory.CreateEnumerator(request, _dataProvider);
            enumerator = ConfigureEnumerator(request, false, enumerator);

            return SubscriptionUtils.CreateAndScheduleWorker(request, enumerator, _factorFileProvider, true);
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
            ISubscriptionEnumeratorFactory factory = _subscriptionFactory;
            if (request.Universe is ITimeTriggeredUniverse)
            {
                factory = new TimeTriggeredUniverseSubscriptionEnumeratorFactory(request.Universe as ITimeTriggeredUniverse,
                    MarketHoursDatabase.FromDataFolder(),
                    _timeProvider);

                if (request.Universe is UserDefinedUniverse)
                {
                    // for user defined universe we do not use a worker task, since calls to AddData can happen in any moment
                    // and we have to be able to inject selection data points into the enumerator
                    return SubscriptionUtils.Create(request, factory.CreateEnumerator(request, _dataProvider));
                }
            }
            if (request.Configuration.Type == typeof(CoarseFundamental))
            {
                factory = new BaseDataCollectionSubscriptionEnumeratorFactory();
            }
            if (request.Universe is OptionChainUniverse)
            {
                factory = new OptionChainUniverseSubscriptionEnumeratorFactory((req) =>
                {
                    var underlyingFactory = new BaseDataSubscriptionEnumeratorFactory(false, _mapFileProvider.Get(req.Configuration.Market), _factorFileProvider);
                    return ConfigureEnumerator(req, true, underlyingFactory.CreateEnumerator(req, _dataProvider));
                });
            }
            if (request.Universe is FuturesChainUniverse)
            {
                factory = new FuturesChainUniverseSubscriptionEnumeratorFactory((req, e) => ConfigureEnumerator(req, true, e));
            }

            // define our data enumerator
            var enumerator = factory.CreateEnumerator(request, _dataProvider);
            return SubscriptionUtils.CreateAndScheduleWorker(request, enumerator, _factorFileProvider, true);
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
                    request.Configuration.ExtendedMarketHours, request.EndTimeLocal, request.Configuration.Resolution.ToTimeSpan(), request.Configuration.DataTimeZone, request.StartTimeLocal);
            }

            // optionally apply exchange/user filters
            if (request.Configuration.IsFilteredSubscription)
            {
                enumerator = SubscriptionFilterEnumerator.WrapForDataFeed(_resultHandler, enumerator, request.Security, request.EndTimeLocal, request.Configuration.ExtendedMarketHours, false);
            }

            return enumerator;
        }
    }
}

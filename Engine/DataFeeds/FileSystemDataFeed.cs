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
using System.Linq;
using System.Threading;
using QuantConnect.Data;
using QuantConnect.Data.Auxiliary;
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
        private IDataCacheProvider _cacheProvider;
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
        public virtual void Initialize(IAlgorithm algorithm,
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
            _cacheProvider = new ZipDataCacheProvider(dataProvider, isDataEphemeral: false);
            _subscriptionFactory = new SubscriptionDataReaderSubscriptionEnumeratorFactory(
                _resultHandler,
                _mapFileProvider,
                _factorFileProvider,
                _cacheProvider,
                enablePriceScaling: false);

            IsActive = true;
        }

        /// <summary>
        /// Creates a file based data enumerator for the given subscription request
        /// </summary>
        /// <remarks>Protected so it can be used by the <see cref="LiveTradingDataFeed"/> to warmup requests</remarks>
        protected IEnumerator<BaseData> CreateEnumerator(SubscriptionRequest request)
        {
            return request.IsUniverseSubscription ? CreateUniverseEnumerator(request, CreateDataEnumerator) : CreateDataEnumerator(request);
        }

        private IEnumerator<BaseData> CreateDataEnumerator(SubscriptionRequest request)
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

            return enumerator;
        }

        /// <summary>
        /// Creates a new subscription to provide data for the specified security.
        /// </summary>
        /// <param name="request">Defines the subscription to be added, including start/end times the universe and security</param>
        /// <returns>The created <see cref="Subscription"/> if successful, null otherwise</returns>
        public virtual Subscription CreateSubscription(SubscriptionRequest request)
        {
            var enumerator = CreateEnumerator(request);

            if (request.IsUniverseSubscription && request.Universe is UserDefinedUniverse)
            {
                // for user defined universe we do not use a worker task, since calls to AddData can happen in any moment
                // and we have to be able to inject selection data points into the enumerator
                return SubscriptionUtils.Create(request, enumerator);
            }
            return SubscriptionUtils.CreateAndScheduleWorker(request, enumerator, _factorFileProvider, true);
        }

        /// <summary>
        /// Removes the subscription from the data feed, if it exists
        /// </summary>
        /// <param name="subscription">The subscription to remove</param>
        public virtual void RemoveSubscription(Subscription subscription)
        {
        }

        protected IEnumerator<BaseData> CreateUniverseEnumerator(SubscriptionRequest request, Func<SubscriptionRequest, IEnumerator<BaseData>> createUnderlyingEnumerator)
        {
            ISubscriptionEnumeratorFactory factory = _subscriptionFactory;
            if (request.Universe is ITimeTriggeredUniverse)
            {
                factory = new TimeTriggeredUniverseSubscriptionEnumeratorFactory(request.Universe as ITimeTriggeredUniverse,
                    MarketHoursDatabase.FromDataFolder(),
                    _timeProvider);

                if (request.Universe is UserDefinedUniverse)
                {
                    return factory.CreateEnumerator(request, _dataProvider);
                }
            }
            else if (request.Configuration.Type == typeof(CoarseFundamental))
            {
                factory = new BaseDataCollectionSubscriptionEnumeratorFactory();
            }
            else if (request.Configuration.Type == typeof(ZipEntryName))
            {
                // TODO: subscription should already come in correctly built
                var resolution = request.Configuration.Resolution == Resolution.Tick ? Resolution.Second : request.Configuration.Resolution;

                // TODO: subscription should already come in as fill forward true
                request = new SubscriptionRequest(request, configuration: new SubscriptionDataConfig(request.Configuration, fillForward: true, resolution: resolution));

                var result = new BaseDataSubscriptionEnumeratorFactory(_algorithm.OptionChainProvider, _algorithm.FutureChainProvider)
                    .CreateEnumerator(request, _dataProvider);
                result = ConfigureEnumerator(request, true, result);
                return TryAppendUnderlyingEnumerator(request, result, createUnderlyingEnumerator);
            }

            // define our data enumerator
            var enumerator = factory.CreateEnumerator(request, _dataProvider);
            return enumerator;
        }

        /// <summary>
        /// If required will add a new enumerator for the underlying symbol
        /// </summary>
        protected IEnumerator<BaseData> TryAppendUnderlyingEnumerator(SubscriptionRequest request, IEnumerator<BaseData> parent, Func<SubscriptionRequest, IEnumerator<BaseData>> createEnumerator)
        {
            if (request.Configuration.Symbol.SecurityType.IsOption() && request.Configuration.Symbol.HasUnderlying)
            {
                // TODO: creating this subscription request/config is bad
                var underlyingRequests = new SubscriptionRequest(request,
                    isUniverseSubscription: false,
                    configuration: new SubscriptionDataConfig(request.Configuration, symbol: request.Configuration.Symbol.Underlying, objectType: typeof(TradeBar), tickType: TickType.Trade));

                var underlying = createEnumerator(underlyingRequests);
                underlying = new FilterEnumerator<BaseData>(underlying, data => data.DataType != MarketDataType.Auxiliary);

                parent = new SynchronizingBaseDataEnumerator(parent, underlying);
                // we aggregate both underlying and chain data
                parent = new BaseDataCollectionAggregatorEnumerator(parent, request.Configuration.Symbol);
                // only let through if underlying and chain data present
                parent = new FilterEnumerator<BaseData>(parent, data => (data as BaseDataCollection).Underlying != null);
                parent = ConfigureEnumerator(request, false, parent);
            }

            return parent;
        }

        /// <summary>
        /// Send an exit signal to the thread.
        /// </summary>
        public virtual void Exit()
        {
            if (IsActive)
            {
                IsActive = false;
                Log.Trace("FileSystemDataFeed.Exit(): Start. Setting cancellation token...");
                _cancellationTokenSource.Cancel();
                _subscriptionFactory?.DisposeSafely();
                _cacheProvider.DisposeSafely();
                Log.Trace("FileSystemDataFeed.Exit(): Exit Finished.");
            }
        }

        /// <summary>
        /// Configure the enumerator with aggregation/fill-forward/filter behaviors. Returns new instance if re-configured
        /// </summary>
        protected IEnumerator<BaseData> ConfigureEnumerator(SubscriptionRequest request, bool aggregate, IEnumerator<BaseData> enumerator)
        {
            if (aggregate)
            {
                enumerator = new BaseDataCollectionAggregatorEnumerator(enumerator, request.Configuration.Symbol);
            }

            enumerator = TryAddFillForwardEnumerator(request, enumerator, request.Configuration.FillDataForward);

            // optionally apply exchange/user filters
            if (request.Configuration.IsFilteredSubscription)
            {
                enumerator = SubscriptionFilterEnumerator.WrapForDataFeed(_resultHandler, enumerator, request.Security,
                    request.EndTimeLocal, request.Configuration.ExtendedMarketHours, false, request.ExchangeHours);
            }

            return enumerator;
        }

        /// <summary>
        /// Will add a fill forward enumerator if requested
        /// </summary>
        protected IEnumerator<BaseData> TryAddFillForwardEnumerator(SubscriptionRequest request, IEnumerator<BaseData> enumerator, bool fillForward)
        {
            // optionally apply fill forward logic, but never for tick data
            if (fillForward && request.Configuration.Resolution != Resolution.Tick)
            {
                // copy forward Bid/Ask bars for QuoteBars
                if (request.Configuration.Type == typeof(QuoteBar))
                {
                    enumerator = new QuoteBarFillForwardEnumerator(enumerator);
                }

                var fillForwardResolution = _subscriptions.UpdateAndGetFillForwardResolution(request.Configuration);

                enumerator = new FillForwardEnumerator(enumerator, request.Security.Exchange, fillForwardResolution,
                    request.Configuration.ExtendedMarketHours, request.EndTimeLocal, request.Configuration.Resolution.ToTimeSpan(), request.Configuration.DataTimeZone);
            }

            return enumerator;
        }
    }
}

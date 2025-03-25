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
using QuantConnect.Data;
using QuantConnect.Data.Auxiliary;
using QuantConnect.Data.Fundamental;
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
        private MarketHoursDatabase _marketHoursDatabase;
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
            _cacheProvider = new ZipDataCacheProvider(dataProvider, isDataEphemeral: false);
            _subscriptionFactory = new SubscriptionDataReaderSubscriptionEnumeratorFactory(
                _resultHandler,
                _mapFileProvider,
                _factorFileProvider,
                _cacheProvider,
                algorithm,
                enablePriceScaling: false);

            IsActive = true;
            _marketHoursDatabase = MarketHoursDatabase.FromDataFolder();
        }

        /// <summary>
        /// Creates a file based data enumerator for the given subscription request
        /// </summary>
        /// <remarks>Protected so it can be used by the <see cref="LiveTradingDataFeed"/> to warmup requests</remarks>
        protected IEnumerator<BaseData> CreateEnumerator(SubscriptionRequest request, Resolution? fillForwardResolution = null)
        {
            return request.IsUniverseSubscription ? CreateUniverseEnumerator(request, CreateDataEnumerator, fillForwardResolution) : CreateDataEnumerator(request, fillForwardResolution);
        }

        private IEnumerator<BaseData> CreateDataEnumerator(SubscriptionRequest request, Resolution? fillForwardResolution)
        {
            // ReSharper disable once PossibleMultipleEnumeration
            var enumerator = _subscriptionFactory.CreateEnumerator(request, _dataProvider);
            enumerator = ConfigureEnumerator(request, false, enumerator, fillForwardResolution);

            return enumerator;
        }

        /// <summary>
        /// Creates a new subscription to provide data for the specified security.
        /// </summary>
        /// <param name="request">Defines the subscription to be added, including start/end times the universe and security</param>
        /// <returns>The created <see cref="Subscription"/> if successful, null otherwise</returns>
        public virtual Subscription CreateSubscription(SubscriptionRequest request)
        {
            IEnumerator<BaseData> enumerator;
            if(_algorithm.IsWarmingUp)
            {
                var pivotTimeUtc = _algorithm.StartDate.ConvertToUtc(_algorithm.TimeZone);

                var warmupRequest = new SubscriptionRequest(request, endTimeUtc: pivotTimeUtc,
                    configuration: new SubscriptionDataConfig(request.Configuration, resolution: _algorithm.Settings.WarmupResolution));
                IEnumerator<BaseData> warmupEnumerator = null;
                if (warmupRequest.TradableDaysInDataTimeZone.Any()
                    // since we change the resolution, let's validate it's still valid configuration (example daily equity quotes are not!)
                    && LeanData.IsValidConfiguration(warmupRequest.Configuration.SecurityType, warmupRequest.Configuration.Resolution, warmupRequest.Configuration.TickType))
                {
                    // let them overlap a day if possible to avoid data gaps since each request will FFed it's own since they are different resolutions
                    pivotTimeUtc = Time.GetStartTimeForTradeBars(request.Security.Exchange.Hours,
                        _algorithm.StartDate.ConvertTo(_algorithm.TimeZone, request.Security.Exchange.TimeZone),
                        Time.OneDay,
                        1,
                        false,
                        warmupRequest.Configuration.DataTimeZone,
                        LeanData.UseDailyStrictEndTimes(_algorithm.Settings, request, request.Security.Symbol, Time.OneDay))
                        .ConvertToUtc(request.Security.Exchange.TimeZone);
                    if (pivotTimeUtc < warmupRequest.StartTimeUtc)
                    {
                        pivotTimeUtc = warmupRequest.StartTimeUtc;
                    }

                    warmupEnumerator = CreateEnumerator(warmupRequest, _algorithm.Settings.WarmupResolution);
                    // don't let future data past
                    warmupEnumerator = new FilterEnumerator<BaseData>(warmupEnumerator, data => data == null || data.EndTime <= warmupRequest.EndTimeLocal);
                }

                var normalEnumerator = CreateEnumerator(new SubscriptionRequest(request, startTimeUtc: pivotTimeUtc));
                // don't let pre start data pass, since we adjust start so they overlap 1 day let's not let this data pass, we just want it for fill forwarding after the target start
                // this is also useful to drop any initial selection point which was already emitted during warmup
                normalEnumerator = new FilterEnumerator<BaseData>(normalEnumerator, data => data == null || data.EndTime >= warmupRequest.EndTimeLocal);

                // after the warmup enumerator we concatenate the 'normal' one
                enumerator = new ConcatEnumerator(true, warmupEnumerator, normalEnumerator);
            }
            else
            {
                enumerator = CreateEnumerator(request);
            }

            enumerator = AddScheduleWrapper(request, enumerator, null);

            if (request.IsUniverseSubscription && request.Universe is UserDefinedUniverse)
            {
                // for user defined universe we do not use a worker task, since calls to AddData can happen in any moment
                // and we have to be able to inject selection data points into the enumerator
                return SubscriptionUtils.Create(request, enumerator, _algorithm.Settings.DailyPreciseEndTime);
            }
            return SubscriptionUtils.CreateAndScheduleWorker(request, enumerator, _factorFileProvider, true, _algorithm.Settings.DailyPreciseEndTime);
        }

        /// <summary>
        /// Removes the subscription from the data feed, if it exists
        /// </summary>
        /// <param name="subscription">The subscription to remove</param>
        public virtual void RemoveSubscription(Subscription subscription)
        {
        }

        /// <summary>
        /// Creates a universe enumerator from the Subscription request, the underlying enumerator func and the fill forward resolution (in some cases)
        /// </summary>
        protected IEnumerator<BaseData> CreateUniverseEnumerator(SubscriptionRequest request, Func<SubscriptionRequest, Resolution?, IEnumerator<BaseData>> createUnderlyingEnumerator, Resolution? fillForwardResolution = null)
        {
            ISubscriptionEnumeratorFactory factory = _subscriptionFactory;
            if (request.Universe is ITimeTriggeredUniverse)
            {
                factory = new TimeTriggeredUniverseSubscriptionEnumeratorFactory(request.Universe as ITimeTriggeredUniverse,
                    _marketHoursDatabase,
                    _timeProvider);
            }
            else if (request.Configuration.Type == typeof(FundamentalUniverse))
            {
                factory = new BaseDataCollectionSubscriptionEnumeratorFactory(_algorithm.ObjectStore);
            }

            // define our data enumerator
            var enumerator = factory.CreateEnumerator(request, _dataProvider);
            return enumerator;
        }

        /// <summary>
        /// Returns a scheduled enumerator from the given arguments. It can also return the given underlying enumerator
        /// </summary>
        protected IEnumerator<BaseData> AddScheduleWrapper(SubscriptionRequest request, IEnumerator<BaseData> underlying, ITimeProvider timeProvider)
        {
            if (!request.IsUniverseSubscription || !request.Universe.UniverseSettings.Schedule.Initialized)
            {
                return underlying;
            }

            var schedule = request.Universe.UniverseSettings.Schedule.Get(request.StartTimeLocal, request.EndTimeLocal);
            if (schedule != null)
            {
                return new ScheduledEnumerator(underlying, schedule, timeProvider, request.Configuration.ExchangeTimeZone, request.StartTimeLocal);
            }
            return underlying;
        }

        /// <summary>
        /// If required will add a new enumerator for the underlying symbol
        /// </summary>
        protected IEnumerator<BaseData> TryAppendUnderlyingEnumerator(SubscriptionRequest request, IEnumerator<BaseData> parent, Func<SubscriptionRequest, Resolution?, IEnumerator<BaseData>> createEnumerator, Resolution? fillForwardResolution)
        {
            if (request.Configuration.Symbol.SecurityType.IsOption() && request.Configuration.Symbol.HasUnderlying)
            {
                var underlyingSymbol = request.Configuration.Symbol.Underlying;
                var underlyingMarketHours = _marketHoursDatabase.GetEntry(underlyingSymbol.ID.Market, underlyingSymbol, underlyingSymbol.SecurityType);

                // TODO: creating this subscription request/config is bad
                var underlyingRequests = new SubscriptionRequest(request,
                    isUniverseSubscription: false,
                    configuration: new SubscriptionDataConfig(request.Configuration, symbol: underlyingSymbol, objectType: typeof(TradeBar), tickType: TickType.Trade,
                    // there's no guarantee the TZ are the same, specially the data timezone (index & index options)
                    dataTimeZone: underlyingMarketHours.DataTimeZone,
                    exchangeTimeZone: underlyingMarketHours.ExchangeHours.TimeZone));

                var underlying = createEnumerator(underlyingRequests, fillForwardResolution);
                underlying = new FilterEnumerator<BaseData>(underlying, data => data.DataType != MarketDataType.Auxiliary);

                parent = new SynchronizingBaseDataEnumerator(parent, underlying);
                // we aggregate both underlying and chain data
                parent = new BaseDataCollectionAggregatorEnumerator(parent, request.Configuration.Symbol);
                // only let through if underlying and chain data present
                parent = new FilterEnumerator<BaseData>(parent, data => (data as BaseDataCollection).Underlying != null);
                parent = ConfigureEnumerator(request, false, parent, fillForwardResolution);
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
                _subscriptionFactory?.DisposeSafely();
                _cacheProvider.DisposeSafely();
                Log.Trace("FileSystemDataFeed.Exit(): Exit Finished.");
            }
        }

        /// <summary>
        /// Configure the enumerator with aggregation/fill-forward/filter behaviors. Returns new instance if re-configured
        /// </summary>
        protected IEnumerator<BaseData> ConfigureEnumerator(SubscriptionRequest request, bool aggregate, IEnumerator<BaseData> enumerator, Resolution? fillForwardResolution)
        {
            if (aggregate)
            {
                enumerator = new BaseDataCollectionAggregatorEnumerator(enumerator, request.Configuration.Symbol);
            }

            enumerator = TryAddFillForwardEnumerator(request, enumerator, request.Configuration.FillDataForward, fillForwardResolution);

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
        protected IEnumerator<BaseData> TryAddFillForwardEnumerator(SubscriptionRequest request, IEnumerator<BaseData> enumerator, bool fillForward, Resolution? fillForwardResolution)
        {
            // optionally apply fill forward logic, but never for tick data
            if (fillForward && request.Configuration.Resolution != Resolution.Tick)
            {
                // copy forward Bid/Ask bars for QuoteBars
                if (request.Configuration.Type == typeof(QuoteBar))
                {
                    enumerator = new QuoteBarFillForwardEnumerator(enumerator);
                }

                var fillForwardSpan = _subscriptions.UpdateAndGetFillForwardResolution(request.Configuration);
                if (fillForwardResolution != null && fillForwardResolution != Resolution.Tick)
                {
                    // if we are giving a FFspan we use it instead of the collection based one. This is useful during warmup when the warmup resolution has been set
                    fillForwardSpan = Ref.Create(fillForwardResolution.Value.ToTimeSpan());
                }

                // Pass the security exchange hours explicitly to avoid using the ones in the request, since
                // those could be different. e.g. when requests are created for open interest data the exchange
                // hours are set to always open to avoid OI data being filtered out due to the exchange being closed.
                // This way we allow OI data to be fill-forwarded to the market close time when strict end times is enabled,
                // so that OI data is available at the same time as trades and quotes.
                var useDailyStrictEndTimes = LeanData.UseDailyStrictEndTimes(_algorithm.Settings, request, request.Configuration.Symbol,
                    request.Configuration.Increment, request.Security.Exchange.Hours);
                enumerator = new FillForwardEnumerator(enumerator, request.Security.Exchange, fillForwardSpan,
                    request.Configuration.ExtendedMarketHours, request.EndTimeLocal, request.Configuration.Increment,
                    request.Configuration.DataTimeZone, useDailyStrictEndTimes, request.Configuration.Type);
            }

            return enumerator;
        }
    }
}

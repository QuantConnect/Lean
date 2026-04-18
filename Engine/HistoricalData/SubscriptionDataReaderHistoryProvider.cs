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
using NodaTime;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Lean.Engine.DataFeeds.Enumerators;
using QuantConnect.Lean.Engine.DataFeeds.Enumerators.Factories;
using QuantConnect.Securities;
using QuantConnect.Util;
using HistoryRequest = QuantConnect.Data.HistoryRequest;

namespace QuantConnect.Lean.Engine.HistoricalData
{
    /// <summary>
    /// Provides an implementation of <see cref="IHistoryProvider"/> that uses <see cref="BaseData"/>
    /// instances to retrieve historical data
    /// </summary>
    public class SubscriptionDataReaderHistoryProvider : SynchronizingHistoryProvider
    {
        private SymbolProperties _nullSymbolProperties;
        private SecurityCache _nullCache;
        private Cash _nullCash;

        private IDataProvider _dataProvider;
        private IMapFileProvider _mapFileProvider;
        private IFactorFileProvider _factorFileProvider;
        private IDataCacheProvider _dataCacheProvider;
        private IObjectStore _objectStore;
        private bool _parallelHistoryRequestsEnabled;
        private bool _initialized;

        /// <summary>
        /// Manager used to allow or deny access to a requested datasource for specific users
        /// </summary>
        protected IDataPermissionManager DataPermissionManager { get; set; }

        /// <summary>
        /// Initializes this history provider to work for the specified job
        /// </summary>
        /// <param name="parameters">The initialization parameters</param>
        public override void Initialize(HistoryProviderInitializeParameters parameters)
        {
            if (_initialized)
            {
                return;
            }
            _initialized = true;
            _dataProvider = parameters.DataProvider;
            _mapFileProvider = parameters.MapFileProvider;
            _dataCacheProvider = parameters.DataCacheProvider;
            _factorFileProvider = parameters.FactorFileProvider;
            _objectStore = parameters.ObjectStore;
            AlgorithmSettings = parameters.AlgorithmSettings;
            DataPermissionManager = parameters.DataPermissionManager;
            _parallelHistoryRequestsEnabled = parameters.ParallelHistoryRequestsEnabled;

            _nullCache = new SecurityCache();
            _nullCash = new Cash(Currencies.NullCurrency, 0, 1m);
            _nullSymbolProperties = SymbolProperties.GetDefault(Currencies.NullCurrency);
        }

        /// <summary>
        /// Gets the history for the requested securities
        /// </summary>
        /// <param name="requests">The historical data requests</param>
        /// <param name="sliceTimeZone">The time zone used when time stamping the slice instances</param>
        /// <returns>An enumerable of the slices of data covering the span specified in each request</returns>
        public override IEnumerable<Slice> GetHistory(IEnumerable<HistoryRequest> requests, DateTimeZone sliceTimeZone)
        {
            // create subscription objects from the configs
            var subscriptions = new List<Subscription>();
            foreach (var request in requests)
            {
                var subscription = CreateSubscription(request);
                subscriptions.Add(subscription);
            }

            return CreateSliceEnumerableFromSubscriptions(subscriptions, sliceTimeZone);
        }

        /// <summary>
        /// Creates a subscription to process the request
        /// </summary>
        private Subscription CreateSubscription(HistoryRequest request)
        {
            var config = request.ToSubscriptionDataConfig();

            // this security is internal only we do not need to worry about a few of it's properties
            // TODO: we don't need fee/fill/BPM/etc either. Even better we should refactor & remove the need for the security
            var security = new Security(
                request.ExchangeHours,
                config,
                _nullCash,
                _nullSymbolProperties,
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null,
                _nullCache
            );

            var dataReader = new SubscriptionDataReader(config,
                request,
                _mapFileProvider,
                _factorFileProvider,
                _dataCacheProvider,
                _dataProvider,
                _objectStore);

            dataReader.InvalidConfigurationDetected += (sender, args) => { OnInvalidConfigurationDetected(args); };
            dataReader.NumericalPrecisionLimited += (sender, args) => { OnNumericalPrecisionLimited(args); };
            dataReader.StartDateLimited += (sender, args) => { OnStartDateLimited(args); };
            dataReader.DownloadFailed += (sender, args) => { OnDownloadFailed(args); };
            dataReader.ReaderErrorDetected += (sender, args) => { OnReaderErrorDetected(args); };

            IEnumerator<BaseData> reader = dataReader;
            var intraday = GetIntradayDataEnumerator(dataReader, request);
            if (intraday != null)
            {
                // we optionally concatenate the intraday data enumerator
                reader = new ConcatEnumerator(true, reader, intraday);
            }

            var useDailyStrictEndTimes = LeanData.UseDailyStrictEndTimes(AlgorithmSettings, request, config.Symbol, config.Increment);
            if (useDailyStrictEndTimes)
            {
                // before corporate events which might yield data and we synchronize both feeds
                reader = new StrictDailyEndTimesEnumerator(reader, request.ExchangeHours, request.StartTimeLocal);
            }

            reader = CorporateEventEnumeratorFactory.CreateEnumerators(
                reader,
                config,
                _factorFileProvider,
                dataReader,
                _mapFileProvider,
                request.StartTimeLocal,
                request.EndTimeLocal);

            // optionally apply fill forward behavior
            if (request.FillForwardResolution.HasValue)
            {
                // copy forward Bid/Ask bars for QuoteBars
                if (request.DataType == typeof(QuoteBar))
                {
                    reader = new QuoteBarFillForwardEnumerator(reader);
                }

                var readOnlyRef = Ref.CreateReadOnly(() => request.FillForwardResolution.Value.ToTimeSpan());
                var exchange = GetSecurityExchange(security.Exchange, request.DataType, request.Symbol);
                reader = new FillForwardEnumerator(reader, exchange, readOnlyRef, request.IncludeExtendedMarketHours, request.StartTimeLocal, request.EndTimeLocal, config.Increment, config.DataTimeZone, useDailyStrictEndTimes, request.DataType);
            }

            // since the SubscriptionDataReader performs an any overlap condition on the trade bar's entire
            // range (time->end time) we can end up passing the incorrect data (too far past, possibly future),
            // so to combat this we deliberately filter the results from the data reader to fix these cases
            // which only apply to non-tick data

            reader = new SubscriptionFilterEnumerator(reader, security, request.EndTimeLocal, config.ExtendedMarketHours, false, request.ExchangeHours);

            // allow all ticks
            if (config.Resolution != Resolution.Tick)
            {
                var timeBasedFilter = new TimeBasedFilter(request);
                reader = new FilterEnumerator<BaseData>(reader, timeBasedFilter.Filter);
            }

            var subscriptionRequest = new SubscriptionRequest(false, null, security, config, request.StartTimeUtc, request.EndTimeUtc);
            if (_parallelHistoryRequestsEnabled)
            {
                return SubscriptionUtils.CreateAndScheduleWorker(subscriptionRequest, reader, _factorFileProvider, false, AlgorithmSettings.DailyPreciseEndTime);
            }
            return SubscriptionUtils.Create(subscriptionRequest, reader, AlgorithmSettings.DailyPreciseEndTime);
        }

        /// <summary>
        /// Gets the intraday data enumerator if any
        /// </summary>
        protected virtual IEnumerator<BaseData> GetIntradayDataEnumerator(IEnumerator<BaseData> rawData, HistoryRequest request)
        {
            return null;
        }

        /// <summary>
        /// Internal helper class to filter data based on requested times
        /// </summary>
        private class TimeBasedFilter
        {
            public Type RequestedType { get; set; }
            public DateTime EndTimeLocal { get; set; }
            public DateTime StartTimeLocal { get; set; }
            public TimeBasedFilter(HistoryRequest request)
            {
                RequestedType = request.DataType;
                EndTimeLocal = request.EndTimeLocal;
                StartTimeLocal = request.StartTimeLocal;
            }
            public bool Filter(BaseData data)
            {
                // filter out all aux data, unless if we are asking for aux data
                if (data.DataType == MarketDataType.Auxiliary && data.GetType() != RequestedType) return false;
                // filter out future data
                if (data.EndTime > EndTimeLocal) return false;
                // filter out data before the start
                return data.EndTime > StartTimeLocal;
            }
        }
    }
}

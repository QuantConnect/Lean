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
using NodaTime;
using QuantConnect.Data;
using QuantConnect.Data.Auxiliary;
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Lean.Engine.DataFeeds.Enumerators;
using QuantConnect.Lean.Engine.Results;
using QuantConnect.Lean.Engine.Setup;
using QuantConnect.Lean.Engine.TransactionHandlers;
using QuantConnect.Orders;
using QuantConnect.Packets;
using QuantConnect.Securities;
using QuantConnect.Statistics;
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
        private IMapFileProvider _mapFileProvider;
        private IFactorFileProvider _factorFileProvider;
        private IDataProvider _dataProvider;
        private IDataCacheProvider _dataCacheProvider;

        /// <summary>
        /// Initializes this history provider to work for the specified job
        /// </summary>
        /// <param name="parameters">The initialization parameters</param>
        public override void Initialize(HistoryProviderInitializeParameters parameters)
        {
            _mapFileProvider = parameters.MapFileProvider;
            _factorFileProvider = parameters.FactorFileProvider;
            _dataProvider = parameters.DataProvider;
            _dataCacheProvider = parameters.DataCacheProvider;
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
                var subscription = CreateSubscription(request, request.StartTimeUtc, request.EndTimeUtc);
                subscription.MoveNext(); // prime pump
                subscriptions.Add(subscription);
            }

            return CreateSliceEnumerableFromSubscriptions(subscriptions, sliceTimeZone);
        }

        /// <summary>
        /// Creates a subscription to process the request
        /// </summary>
        private Subscription CreateSubscription(HistoryRequest request, DateTime start, DateTime end)
        {
            // data reader expects these values in local times
            start = start.ConvertFromUtc(request.ExchangeHours.TimeZone);
            end = end.ConvertFromUtc(request.ExchangeHours.TimeZone);

            var config = new SubscriptionDataConfig(request.DataType,
                request.Symbol,
                request.Resolution,
                request.DataTimeZone,
                request.ExchangeHours.TimeZone,
                request.FillForwardResolution.HasValue,
                request.IncludeExtendedMarketHours,
                false,
                request.IsCustomData,
                request.TickType,
                true,
                request.DataNormalizationMode
                );

            var security = new Security(
                request.ExchangeHours,
                config,
                new Cash(CashBook.AccountCurrency, 0, 1m),
                SymbolProperties.GetDefault(CashBook.AccountCurrency),
                ErrorCurrencyConverter.Instance
            );

            IEnumerator<BaseData> reader = new SubscriptionDataReader(config,
                start,
                end,
                ResultHandlerStub.Instance,
                config.SecurityType == SecurityType.Equity ? _mapFileProvider.Get(config.Market) : MapFileResolver.Empty,
                _factorFileProvider,
                _dataProvider,
                Time.EachTradeableDay(request.ExchangeHours, start, end),
                false,
                _dataCacheProvider,
                false
                );

            // optionally apply fill forward behavior
            if (request.FillForwardResolution.HasValue)
            {
                // copy forward Bid/Ask bars for QuoteBars
                if (request.DataType == typeof(QuoteBar))
                {
                    reader = new QuoteBarFillForwardEnumerator(reader);
                }

                var readOnlyRef = Ref.CreateReadOnly(() => request.FillForwardResolution.Value.ToTimeSpan());
                reader = new FillForwardEnumerator(reader, security.Exchange, readOnlyRef, security.IsExtendedMarketHours, end, config.Increment, config.DataTimeZone);
            }

            // since the SubscriptionDataReader performs an any overlap condition on the trade bar's entire
            // range (time->end time) we can end up passing the incorrect data (too far past, possibly future),
            // so to combat this we deliberately filter the results from the data reader to fix these cases
            // which only apply to non-tick data

            reader = new SubscriptionFilterEnumerator(reader, security, end);
            reader = new FilterEnumerator<BaseData>(reader, data =>
            {
                // allow all ticks
                if (config.Resolution == Resolution.Tick) return true;
                // filter out future data
                if (data.EndTime > end) return false;
                // filter out data before the start
                return data.EndTime > start;
            });

            var timeZoneOffsetProvider = new TimeZoneOffsetProvider(security.Exchange.TimeZone, start, end);
            var subscriptionDataEnumerator = SubscriptionData.Enumerator(config, security, timeZoneOffsetProvider, reader);
            return new Subscription(null, security, config, subscriptionDataEnumerator, timeZoneOffsetProvider, start, end, false);
        }

        // this implementation is provided solely for the data reader's dependency,
        // in the future we can refactor the data reader to not use the result handler
        private class ResultHandlerStub : BaseResultsHandler, IResultHandler
        {
            public static readonly IResultHandler Instance = new ResultHandlerStub();

            private ResultHandlerStub() { }

            #region Implementation of IResultHandler

            public ConcurrentQueue<Packet> Messages { get; set; }
            public ConcurrentDictionary<string, Chart> Charts { get; set; }
            public TimeSpan ResamplePeriod { get; private set; }
            public TimeSpan NotificationPeriod { get; private set; }
            public bool IsActive { get; private set; }

            public void Initialize(AlgorithmNodePacket job,
                IMessagingHandler messagingHandler,
                IApi api,
                IDataFeed dataFeed,
                ISetupHandler setupHandler,
                ITransactionHandler transactionHandler) { }
            public void Run() { }
            public void DebugMessage(string message) { }
            public void SystemDebugMessage(string message) { }
            public void SecurityType(List<SecurityType> types) { }
            public void LogMessage(string message) { }
            public void ErrorMessage(string error, string stacktrace = "") { }
            public void RuntimeError(string message, string stacktrace = "") { }
            public void Sample(string chartName, string seriesName, int seriesIndex, SeriesType seriesType, DateTime time, decimal value, string unit = "$") { }
            public void SampleEquity(DateTime time, decimal value) { }
            public void SamplePerformance(DateTime time, decimal value) { }
            public void SampleBenchmark(DateTime time, decimal value) { }
            public void SampleAssetPrices(Symbol symbol, DateTime time, decimal value) { }
            public void SampleRange(List<Chart> samples) { }
            public void SetAlgorithm(IAlgorithm algorithm) { }
            public void StoreResult(Packet packet, bool async = false) { }
            public void SendFinalResult(AlgorithmNodePacket job, Dictionary<int, Order> orders, Dictionary<DateTime, decimal> profitLoss, Dictionary<string, Holding> holdings, CashBook cashbook, StatisticsResults statisticsResults, Dictionary<string, string> banner) { }
            public void SendStatusUpdate(AlgorithmStatus status, string message = "") { }
            public void SetChartSubscription(string symbol) { }
            public void RuntimeStatistic(string key, string value) { }
            public void OrderEvent(OrderEvent newEvent) { }
            public void Exit() { }
            public void PurgeQueue() { }
            public void ProcessSynchronousEvents(bool forceProcess = false) { }

            #endregion
        }

        private class FilterEnumerator<T> : IEnumerator<T>
        {
            private readonly IEnumerator<T> _enumerator;
            private readonly Func<T, bool> _filter;

            public FilterEnumerator(IEnumerator<T> enumerator, Func<T, bool> filter)
            {
                _enumerator = enumerator;
                _filter = filter;
            }

            #region Implementation of IDisposable

            public void Dispose()
            {
                _enumerator.Dispose();
            }

            #endregion

            #region Implementation of IEnumerator

            public bool MoveNext()
            {
                // run the enumerator until it passes the specified filter
                while (_enumerator.MoveNext())
                {
                    if (_filter(_enumerator.Current))
                    {
                        return true;
                    }
                }
                return false;
            }

            public void Reset()
            {
                _enumerator.Reset();
            }

            public T Current
            {
                get { return _enumerator.Current; }
            }

            object IEnumerator.Current
            {
                get { return _enumerator.Current; }
            }

            #endregion
        }
    }
}

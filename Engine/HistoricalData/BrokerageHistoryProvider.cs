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
*/

using System.Collections.Generic;
using NodaTime;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Lean.Engine.DataFeeds.Enumerators;
using QuantConnect.Securities;
using QuantConnect.Util;
using HistoryRequest = QuantConnect.Data.HistoryRequest;

namespace QuantConnect.Lean.Engine.HistoricalData
{
    /// <summary>
    /// Provides an implementation of <see cref="IHistoryProvider"/> that relies on
    /// a brokerage connection to retrieve historical data
    /// </summary>
    public class BrokerageHistoryProvider : SynchronizingHistoryProvider
    {
        private IBrokerage _brokerage;

        /// <summary>
        /// Sets the brokerage to be used for historical requests
        /// </summary>
        /// <param name="brokerage">The brokerage instance</param>
        public void SetBrokerage(IBrokerage brokerage)
        {
            _brokerage = brokerage;
        }

        /// <summary>
        /// Initializes this history provider to work for the specified job
        /// </summary>
        /// <param name="parameters">The initialization parameters</param>
        public override void Initialize(HistoryProviderInitializeParameters parameters)
        {
            _brokerage.Connect();
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
                var history = _brokerage.GetHistory(request);
                var subscription = CreateSubscription(request, history);

                subscription.MoveNext(); // prime pump
                subscriptions.Add(subscription);
            }

            return CreateSliceEnumerableFromSubscriptions(subscriptions, sliceTimeZone);
        }

        /// <summary>
        /// Creates a subscription to process the history request
        /// </summary>
        private static Subscription CreateSubscription(HistoryRequest request, IEnumerable<BaseData> history)
        {
            // data reader expects these values in local times
            var start = request.StartTimeUtc.ConvertFromUtc(request.ExchangeHours.TimeZone);
            var end = request.EndTimeUtc.ConvertFromUtc(request.ExchangeHours.TimeZone);

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
                new Cash(Currencies.NullCurrency, 0, 1m),
                SymbolProperties.GetDefault(Currencies.NullCurrency),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null
            );

            var reader = history.GetEnumerator();

            // optionally apply fill forward behavior
            if (request.FillForwardResolution.HasValue)
            {
                // copy forward Bid/Ask bars for QuoteBars
                if (request.DataType == typeof(QuoteBar))
                {
                    reader = new QuoteBarFillForwardEnumerator(reader);
                }

                var readOnlyRef = Ref.CreateReadOnly(() => request.FillForwardResolution.Value.ToTimeSpan());
                reader = new FillForwardEnumerator(reader, security.Exchange, readOnlyRef, security.IsExtendedMarketHours, end, config.Increment, config.DataTimeZone, start);
            }

            var timeZoneOffsetProvider = new TimeZoneOffsetProvider(security.Exchange.TimeZone, start, end);
            var subscriptionDataEnumerator = new SubscriptionDataEnumerator(config, security.Exchange.Hours, timeZoneOffsetProvider, reader);

            var subscriptionRequest = new SubscriptionRequest(false, null, security, config, start, end);
            return new Subscription(subscriptionRequest, subscriptionDataEnumerator, timeZoneOffsetProvider);
        }
    }
}

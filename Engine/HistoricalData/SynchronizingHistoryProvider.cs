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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using NodaTime;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Lean.Engine.DataFeeds.Enumerators;
using QuantConnect.Securities;
using QuantConnect.Util;

namespace QuantConnect.Lean.Engine.HistoricalData
{
    /// <summary>
    /// Provides an abstract implementation of <see cref="IHistoryProvider"/>
    /// which provides synchronization of multiple history results
    /// </summary>
    public abstract class SynchronizingHistoryProvider : HistoryProviderBase
    {
        private int _dataPointCount;

        /// <summary>
        /// The algorithm settings instance to use
        /// </summary>
        public IAlgorithmSettings AlgorithmSettings { get; set; } = new AlgorithmSettings();

        /// <summary>
        /// Gets the total number of data points emitted by this history provider
        /// </summary>
        public override int DataPointCount => _dataPointCount;

        /// <summary>
        /// Enumerates the subscriptions into slices
        /// </summary>
        protected IEnumerable<Slice> CreateSliceEnumerableFromSubscriptions(List<Subscription> subscriptions, DateTimeZone sliceTimeZone)
        {
            // required by TimeSlice.Create, but we don't need it's behavior
            var frontier = DateTime.MinValue;
            // never changes, there's no selection during a history request
            var universeSelectionData = new Dictionary<Universe, BaseDataCollection>();
            var timeSliceFactory = new TimeSliceFactory(sliceTimeZone);
            while (true)
            {
                var earlyBirdTicks = long.MaxValue;
                var data = new List<DataFeedPacket>();
                foreach (var subscription in subscriptions.Where(subscription => !subscription.EndOfStream))
                {
                    if (subscription.Current == null && !subscription.MoveNext())
                    {
                        // initial pump. We do it here and not when creating the subscriptions so
                        // that parallel workers can all start as fast as possible
                        continue;
                    }

                    DataFeedPacket packet = null;
                    while (subscription.Current.EmitTimeUtc <= frontier)
                    {
                        if (packet == null)
                        {
                            // for performance, lets be selfish about creating a new instance
                            packet = new DataFeedPacket(subscription.Security, subscription.Configuration);

                            // only add if we have data
                            data.Add(packet);
                        }

                        packet.Add(subscription.Current.Data);
                        Interlocked.Increment(ref _dataPointCount);
                        if (!subscription.MoveNext())
                        {
                            break;
                        }
                    }
                    // update our early bird ticks (next frontier time)
                    if (subscription.Current != null)
                    {
                        // take the earliest between the next piece of data or the next tz discontinuity
                        earlyBirdTicks = Math.Min(earlyBirdTicks, subscription.Current.EmitTimeUtc.Ticks);
                    }
                }

                if (data.Count != 0)
                {
                    // reuse the slice construction code from TimeSlice.Create
                    yield return timeSliceFactory.Create(frontier, data, SecurityChanges.None, universeSelectionData).Slice;
                }

                // end of subscriptions, after we emit, else we might drop a data point
                if (earlyBirdTicks == long.MaxValue) break;

                frontier = new DateTime(Math.Max(earlyBirdTicks, frontier.Ticks), DateTimeKind.Utc);
            }

            // make sure we clean up after ourselves
            foreach (var subscription in subscriptions)
            {
                subscription.Dispose();
            }
        }

        /// <summary>
        /// Creates a subscription to process the history request
        /// </summary>
        protected Subscription CreateSubscription(HistoryRequest request, IEnumerable<BaseData> history)
        {
            var config = request.ToSubscriptionDataConfig();
            var security = new Security(
                request.ExchangeHours,
                config,
                new Cash(Currencies.NullCurrency, 0, 1m),
                SymbolProperties.GetDefault(Currencies.NullCurrency),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCache()
            );

            var reader = history.GetEnumerator();

            var useDailyStrictEndTimes = LeanData.UseDailyStrictEndTimes(AlgorithmSettings, request, config.Symbol, config.Increment);
            if (useDailyStrictEndTimes)
            {
                reader = new StrictDailyEndTimesEnumerator(reader, request.ExchangeHours);
            }

            // optionally apply fill forward behavior
            if (request.FillForwardResolution.HasValue)
            {
                // FillForwardEnumerator expects these values in local times
                var start = request.StartTimeUtc.ConvertFromUtc(request.ExchangeHours.TimeZone);
                var end = request.EndTimeUtc.ConvertFromUtc(request.ExchangeHours.TimeZone);

                // copy forward Bid/Ask bars for QuoteBars
                if (request.DataType == typeof(QuoteBar))
                {
                    reader = new QuoteBarFillForwardEnumerator(reader);
                }

                var readOnlyRef = Ref.CreateReadOnly(() => request.FillForwardResolution.Value.ToTimeSpan());
                reader = new FillForwardEnumerator(reader, security.Exchange, readOnlyRef, request.IncludeExtendedMarketHours, end, config.Increment, config.DataTimeZone, useDailyStrictEndTimes);
            }

            var subscriptionRequest = new SubscriptionRequest(false, null, security, config, request.StartTimeUtc, request.EndTimeUtc);

            return SubscriptionUtils.Create(subscriptionRequest, reader, AlgorithmSettings.DailyStrictEndTimeEnabled);
        }
    }
}

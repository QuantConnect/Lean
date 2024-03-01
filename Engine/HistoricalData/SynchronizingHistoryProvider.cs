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
using NodaTime;
using System.Linq;
using System.Threading;
using QuantConnect.Util;
using QuantConnect.Data;
using QuantConnect.Securities;
using QuantConnect.Interfaces;
using QuantConnect.Data.Market;
using System.Collections.Generic;
using QuantConnect.Data.Auxiliary;
using System.Collections.ObjectModel;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Lean.Engine.DataFeeds.Enumerators;

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
                reader = new FillForwardEnumerator(reader, security.Exchange, readOnlyRef, request.IncludeExtendedMarketHours, end, config.Increment, config.DataTimeZone);
            }

            var subscriptionRequest = new SubscriptionRequest(false, null, security, config, request.StartTimeUtc, request.EndTimeUtc);

            return SubscriptionUtils.Create(subscriptionRequest, reader);
        }

#pragma warning disable CA1822
        /// <summary>
        /// Split <see cref="HistoryRequest"/> on several request with update mapped symbol.
        /// </summary>
        /// <param name="mapFileProvider">Provides instances of <see cref="MapFileResolver"/> at run time</param>
        /// <param name="request">Represents a request for historical data</param>
        /// <returns>
        /// Return HistoryRequests with different <see cref="BaseDataRequest.StartTimeUtc"/> - <seealso cref="BaseDataRequest.EndTimeUtc"/> range
        /// and <seealso cref="Symbol.Value"/>
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="mapFileProvider"/> is null.</exception>
        /// <example>
        /// For instances:
        /// request = { StartTimeUtc = 2013/01/01, EndTimeUtc = 2017/02/02, Symbol = "GOOGL" }  split request on:
        /// 1: request = { StartTimeUtc = 2013/01/01, EndTimeUtc = 2014/04/02, Symbol.Value = "GOOG" }
        /// 2: request = { StartTimeUtc = 2014/04/**03**, EndTimeUtc = 2017/02/02, Symbol.Value = "GOOGL" }
        /// > GOOGLE: IPO: August 19, 2004 Name = GOOG then it was restructured: from "GOOG" to "GOOGL" on April 2, 2014
        /// </example>
        protected ReadOnlyCollection<HistoryRequest> SplitHistoryRequestWithUpdatedMappedSymbol(IMapFileProvider mapFileProvider, HistoryRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var historicalSymbolNames = RetrieveSymbolHistoricalDefinitions(mapFileProvider, request.Symbol, request.StartTimeUtc);

            var requests = new List<HistoryRequest>();
            var startDateTime = request.StartTimeUtc;
            foreach (var (ticker, tickerEndSupportDate) in historicalSymbolNames)
            {

                var symbol = request.Symbol.UpdateMappedSymbol(ticker);

                if (tickerEndSupportDate >= request.EndTimeUtc)
                {
                    requests.Add(new HistoryRequest(request, symbol, startDateTime, request.EndTimeUtc));
                    // the request EndDateTime was achieved
                    break;
                }
                else
                {
                    requests.Add(new HistoryRequest(request, symbol, startDateTime, tickerEndSupportDate));
                    startDateTime = tickerEndSupportDate.AddDays(1);
                }
            }

            return requests.AsReadOnly();
        }

        /// <summary>
        /// Some historical provider supports ancient data. In fact, the ticker could be restructured to new one.
        /// </summary>
        /// <param name="mapFileProvider">Provides instances of <see cref="MapFileResolver"/> at run time</param>
        /// <param name="symbol">Represents a unique security identifier</param>
        /// <param name="startDateTime">The date since we began our search for the historical name of the symbol.</param>
        /// <returns>Distinct and immutable HashSet of Ticker names and his last support DateTime </returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="mapFileProvider"/> is null.</exception>
        /// <example>
        /// GOOGLE: IPO: August 19, 2004 Name = GOOG then it was restructured: from "GOOG" to "GOOGL" on April 2, 2014
        /// For instances:
        /// startDateTime = 2013 year, it returns { "GOOG", "GOOGL" }
        /// startDateTime = 2023 year, it returns { "GOOGL" }
        /// </example>
        protected ReadOnlyCollection<(string Ticker, DateTime TickerSupportEndDate)> RetrieveSymbolHistoricalDefinitions(IMapFileProvider mapFileProvider, Symbol symbol, DateTime startDateTime)
        {
            if (mapFileProvider == null)
            {
                throw new ArgumentNullException(nameof(mapFileProvider));
            }

            var mapFileResolver = mapFileProvider.Get(AuxiliaryDataKey.Create(symbol));
            var symbolMapFile = mapFileResolver.ResolveMapFile(symbol);

            return symbolMapFile.Where(x => x.Date >= startDateTime).Select(x => (x.MappedSymbol, x.Date)).ToList().AsReadOnly();
        }
#pragma warning restore CA1822
    }
}

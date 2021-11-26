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
using QuantConnect.Data;
using QuantConnect.Configuration;
using QuantConnect.Packets;
using QuantConnect.Logging;
using Newtonsoft.Json.Linq;
using QuantConnect.Data.Market;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading;
using QuantConnect.Interfaces;
using NodaTime;
using System.Globalization;
using static QuantConnect.StringExtensions;
using QuantConnect.Util;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Lean.Engine.HistoricalData;
using QuantConnect.ToolBox.IEX.Response;

namespace QuantConnect.ToolBox.IEX
{
    /// <summary>
    /// IEX live data handler.
    /// See more at https://iexcloud.io/docs/api/
    /// </summary>
    public class IEXDataQueueHandler : SynchronizingHistoryProvider, IDataQueueHandler
    {
        private static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified);
        private static readonly TimeSpan SubscribeDelay = TimeSpan.FromMilliseconds(1500);
        private static bool _invalidHistDataTypeWarningFired;

        private readonly IEXEventSourceCollection _clients;
        private readonly ManualResetEvent _refreshEvent = new ManualResetEvent(false);
        private readonly string _apiKey = Config.Get("iex-cloud-api-key");

        private readonly ConcurrentDictionary<string, Symbol> _symbols = new ConcurrentDictionary<string, Symbol>(StringComparer.InvariantCultureIgnoreCase);
        private readonly ConcurrentDictionary<string, long> _iexLastTradeTime = new ConcurrentDictionary<string, long>();

        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private int _dataPointCount;

        private readonly IDataAggregator _aggregator = Composer.Instance.GetExportedValueByTypeName<IDataAggregator>(
            Config.Get("data-aggregator", "QuantConnect.Lean.Engine.DataFeeds.AggregationManager"));
        private readonly EventBasedDataQueueHandlerSubscriptionManager _subscriptionManager;

        public bool IsConnected => _clients.IsConnected;


        /// <summary>
        /// Initializes a new instance of the <see cref="IEXDataQueueHandler"/> class.
        /// </summary>
        public IEXDataQueueHandler()
        {
            _subscriptionManager = new EventBasedDataQueueHandlerSubscriptionManager();

            _subscriptionManager.SubscribeImpl += (symbols, t) =>
            {
                symbols.DoForEach(symbol =>
                {
                    if (!_symbols.TryAdd(symbol.Value, symbol))
                    {
                        throw new InvalidOperationException($"Invalid logic, SubscriptionManager tried to subscribe to existing symbol : {symbol.Value}");
                    }
                });

                Refresh();
                return true;
            };

            _subscriptionManager.UnsubscribeImpl += (symbols, t) =>
            {
                symbols.DoForEach(symbol =>
                {
                    Symbol tmp;
                    _symbols.TryRemove(symbol.Value, out tmp);
                });

                Refresh();
                return true;
            };

            if (string.IsNullOrWhiteSpace(_apiKey))
            {
                throw new ArgumentException("Could not read IEX API key from config.json. " +
                    "Please make sure to add \"iex-cloud-api-key\" to your configuration file");
            }

            // Set the sse-clients collection
            _clients = new IEXEventSourceCollection(((o, args) =>
            {
                var message = args.Message.Data;
                ProcessJsonObject(message);

            }), _apiKey);

            // In this thread, we check at each interval whether the client needs to be updated
            // Subscription renewal requests may come in dozens and all at relatively same time - we cannot update them one by one when work with SSE
            var clientUpdateThread = new Thread(() =>
            {
                while (!_cts.Token.IsCancellationRequested)
                {
                    _refreshEvent.WaitOne();
                    Thread.Sleep(SubscribeDelay);

                    _refreshEvent.Reset();

                    try
                    {
                        _clients.UpdateSubscription(_symbols.Keys.ToArray());
                    }
                    catch (Exception e)
                    {
                        Log.Error(e);
                        throw;
                    }
                }

            })
            { IsBackground = true };
            clientUpdateThread.Start();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ProcessJsonObject(string json)
        {
            try
            {
                var dataList = JsonConvert.DeserializeObject<List<StreamResponseStocksUS>>(json);

                foreach (var item in dataList)
                {
                    var symbolString = item.Symbol;
                    Symbol symbol;
                    if (!_symbols.TryGetValue(symbolString, out symbol))
                    {
                        // Symbol is no loner in dictionary, it may be the stream not has been updated yet,
                        // and the old client is still sending messages for unsubscribed symbols -
                        // so there can be residual messages for the symbol, which we must skip
                        continue;
                    }

                    var lastPrice = item.IexRealtimePrice ?? 0;
                    var lastSize = item.IexRealtimeSize ?? 0;

                    // Refers to the last update time of iexRealtimePrice in milliseconds since midnight Jan 1, 1970 UTC or -1 or 0.
                    // If the value is -1 or 0, IEX has not quoted the symbol in the trading day.
                    var lastUpdateMillis = item.IexLastUpdated ?? 0;

                    if (lastUpdateMillis <= 0)
                    {
                        continue;
                    }

                    // (!) Epoch timestamp in milliseconds of the last market hours trade excluding the closing auction trade.
                    var lastTradeMillis = item.LastTradeTime ?? 0;

                    if (lastTradeMillis <= 0)
                    {
                        continue;
                    }

                    // If there is a last trade time but no last price or size - this is an error
                    if (lastPrice == 0 || lastSize == 0)
                    {
                        throw new InvalidOperationException("ProcessJsonObject(): Invalid price & size.");
                    }

                    // Check if there is a kvp entry for a symbol
                    long value;
                    var isInDictionary = _iexLastTradeTime.TryGetValue(symbolString, out value);

                    // We should update with trade-tick if:
                    // - there exist an entry for a symbol and new trade time is different from time in dictionary
                    // - not in dictionary, means the first trade-tick case
                    if (isInDictionary && value != lastTradeMillis || !isInDictionary)
                    {
                        var lastTradeDateTime = UnixEpoch.AddMilliseconds(lastTradeMillis);
                        var lastTradeTimeNewYork = lastTradeDateTime.ConvertFromUtc(TimeZones.NewYork);

                        var tradeTick = new Tick()
                        {
                            Symbol = symbol,
                            Time = lastTradeTimeNewYork,
                            TickType = TickType.Trade,
                            Value = lastPrice,
                            Quantity = lastSize
                        };

                        _aggregator.Update(tradeTick);

                        _iexLastTradeTime[symbolString] = lastTradeMillis;
                    }
                }
            }
            catch (Exception err)
            {
                Log.Error("IEXDataQueueHandler.ProcessJsonObject(): " + err.Message);
            }
        }

        /// <summary>
        /// Subscribe to the specified configuration
        /// </summary>
        /// <param name="dataConfig">defines the parameters to subscribe to a data feed</param>
        /// <param name="newDataAvailableHandler">handler to be fired on new data available</param>
        /// <returns>The new enumerator for this subscription request</returns>
        public IEnumerator<BaseData> Subscribe(SubscriptionDataConfig dataConfig, EventHandler newDataAvailableHandler)
        {
            if (dataConfig.ExtendedMarketHours)
            {
                Log.Error("IEXDataQueueHandler.Subscribe(): Unfortunately no updates could be received from IEX outside the regular exchange open hours. " +
                          "Please be aware that only regular hours updates will be submitted to an algorithm.");
            }

            if (dataConfig.Resolution < Resolution.Second)
            {
                Log.Error($"IEXDataQueueHandler.Subscribe(): Selected data resolution ({dataConfig.Resolution}) " +
                          "is not supported by current implementation of  IEXDataQueueHandler. Sorry. Please try the higher resolution.");
            }

            if (dataConfig.SecurityType != SecurityType.Equity)
            {
                return Enumerable.Empty<BaseData>().GetEnumerator();
            }

            var enumerator = _aggregator.Add(dataConfig, newDataAvailableHandler);
            _subscriptionManager.Subscribe(dataConfig);

            return enumerator;
        }

        /// <summary>
        /// Sets the job we're subscribing for
        /// </summary>
        /// <param name="job">Job we're subscribing for</param>
        public void SetJob(LiveNodePacket job)
        {
        }

        private void Refresh()
        {
            _refreshEvent.Set();
        }

        /// <summary>
        /// Removes the specified configuration
        /// </summary>
        /// <param name="dataConfig">Subscription config to be removed</param>
        public void Unsubscribe(SubscriptionDataConfig dataConfig)
        {
            _subscriptionManager.Unsubscribe(dataConfig);
            _aggregator.Remove(dataConfig);
        }

        /// <summary>
        /// Dispose connection to IEX
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            _aggregator.DisposeSafely();
            _cts.Cancel();

            _clients.Dispose();

            Log.Trace("IEXDataQueueHandler.Dispose(): Disconnected from IEX data provider");
        }

        ~IEXDataQueueHandler()
        {
            Dispose(false);
        }

        #region IHistoryProvider implementation

        /// <summary>
        /// Gets the total number of data points emitted by this history provider
        /// </summary>
        public override int DataPointCount => _dataPointCount;

        /// <summary>
        /// Initializes this history provider to work for the specified job
        /// </summary>
        /// <param name="parameters">The initialization parameters</param>
        public override void Initialize(HistoryProviderInitializeParameters parameters)
        {
        }

        /// <summary>
        /// Gets the history for the requested securities
        /// </summary>
        /// <param name="requests">The historical data requests</param>
        /// <param name="sliceTimeZone">The time zone used when time stamping the slice instances</param>
        /// <returns>An enumerable of the slices of data covering the span specified in each request</returns>
        public override IEnumerable<Slice> GetHistory(IEnumerable<Data.HistoryRequest> requests, DateTimeZone sliceTimeZone)
        {
            if (string.IsNullOrWhiteSpace(_apiKey))
            {
                Log.Error("IEXDataQueueHandler.GetHistory(): History calls for IEX require an API key.");
                return Enumerable.Empty<Slice>();
            }

            // Create subscription objects from the configs
            var subscriptions = new List<Subscription>();
            requests.DoForEach(request =>
            {
                // IEX does return historical TradeBar - give one time warning if inconsistent data type was requested
                if (request.DataType != typeof(TradeBar) && !_invalidHistDataTypeWarningFired)
                {
                    Log.Error($"IEXDataQueueHandler.GetHistory(): Not supported data type - {request.DataType.Name}. " +
                        "Currently available support only for historical of type - TradeBar");
                    _invalidHistDataTypeWarningFired = true;
                    return;
                }

                var history = ProcessHistoryRequests(request);
                var subscription = CreateSubscription(request, history);
                subscriptions.Add(subscription);
            });

            var result = subscriptions.Any() ? CreateSliceEnumerableFromSubscriptions(subscriptions, sliceTimeZone) : Enumerable.Empty<Slice>();
            return result;
        }

        /// <summary>
        /// Populate request data
        /// </summary>
        private IEnumerable<BaseData> ProcessHistoryRequests(Data.HistoryRequest request)
        {
            var ticker = request.Symbol.ID.Symbol;
            var start = request.StartTimeUtc.ConvertFromUtc(TimeZones.NewYork);
            var end = request.EndTimeUtc.ConvertFromUtc(TimeZones.NewYork);

            if (request.Resolution == Resolution.Minute && start <= DateTime.Today.AddDays(-30))
            {
                Log.Error("IEXDataQueueHandler.GetHistory(): History calls with minute resolution for IEX available only for trailing 30 calendar days.");
                yield break;
            }

            if (request.Resolution != Resolution.Daily && request.Resolution != Resolution.Minute)
            {
                Log.Error("IEXDataQueueHandler.GetHistory(): History calls for IEX only support daily & minute resolution.");
                yield break;
            }

            Log.Trace("IEXDataQueueHandler.ProcessHistoryRequests(): Submitting request: " +
                Invariant($"{request.Symbol.SecurityType}-{ticker}: {request.Resolution} {start}->{end}") +
                ". Please wait..");

            const string baseUrl = "https://cloud.iexapis.com/stable/stock";
            var now = DateTime.UtcNow.ConvertFromUtc(TimeZones.NewYork);
            var span = now - start;
            var urls = new List<string>();

            switch (request.Resolution)
            {
                case Resolution.Minute:
                {
                    var begin = start;
                    while (begin < end)
                    {
                        var url =
                            $"{baseUrl}/{ticker}/chart/date/{begin.ToStringInvariant("yyyyMMdd")}?token={_apiKey}";
                        urls.Add(url);
                        begin = begin.AddDays(1);
                    }

                    break;
                }
                case Resolution.Daily:
                {
                    string suffix;
                    if (span.Days < 30)
                    {
                        suffix = "1m";
                    }
                    else if (span.Days < 3 * 30)
                    {
                        suffix = "3m";
                    }
                    else if (span.Days < 6 * 30)
                    {
                        suffix = "6m";
                    }
                    else if (span.Days < 12 * 30)
                    {
                        suffix = "1y";
                    }
                    else if (span.Days < 24 * 30)
                    {
                        suffix = "2y";
                    }
                    else if (span.Days < 60 * 30)
                    {
                        suffix = "5y";
                    }
                    else
                    {
                        suffix = "max";   // max is 15 years
                    }

                    var url =
                        $"{baseUrl}/{ticker}/chart/{suffix}?token={_apiKey}";
                    urls.Add(url);

                    break;
                }
            }

            // Download and parse data
            var requests = new List<Task<string>>();

            urls.DoForEach(url =>
            {
                requests.Add(Task.Run(async () =>
                {
                    using (var client = new WebClient())
                    {
                        return await client.DownloadStringTaskAsync(new Uri(url)).ConfigureAwait(false);
                    }
                }));
            });

            var responses = Task.WhenAll(requests).Result;

            foreach (var response in responses)
            {
                var parsedResponse = JArray.Parse(response);

                // Parse
                foreach (var item in parsedResponse.Children())
                {
                    DateTime date;
                    TimeSpan period;
                    if (item["minute"] != null)
                    {
                        date = DateTime.ParseExact(item["date"].Value<string>(), "yyyy-MM-dd", CultureInfo.InvariantCulture);
                        var minutes = TimeSpan.ParseExact(item["minute"].Value<string>(), "hh\\:mm", CultureInfo.InvariantCulture);
                        date += minutes;
                        period = TimeSpan.FromMinutes(1);
                    }
                    else
                    {
                        date = Parse.DateTime(item["date"].Value<string>());
                        period = TimeSpan.FromDays(1);
                    }

                    if (date < start || date > end)
                    {
                        continue;
                    }

                    Interlocked.Increment(ref _dataPointCount);

                    if (item["open"].Type == JTokenType.Null)
                    {
                        continue;
                    }

                    decimal open, high, low, close, volume;

                    if (request.Resolution == Resolution.Daily &&
                        request.DataNormalizationMode == DataNormalizationMode.Raw)
                    {
                        open = item["uOpen"].Value<decimal>();
                        high = item["uHigh"].Value<decimal>();
                        low = item["uLow"].Value<decimal>();
                        close = item["uClose"].Value<decimal>();
                        volume = item["uVolume"].Value<int>();
                    }
                    else
                    {
                        open = item["open"].Value<decimal>();
                        high = item["high"].Value<decimal>();
                        low = item["low"].Value<decimal>();
                        close = item["close"].Value<decimal>();
                        volume = item["volume"].Value<int>();
                    }

                    var tradeBar = new TradeBar(date, request.Symbol, open, high, low, close, volume, period);

                    yield return tradeBar;
                }
            }
        }

        #endregion
    }
}

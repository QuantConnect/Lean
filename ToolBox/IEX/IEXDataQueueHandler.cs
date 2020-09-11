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
using Quobject.SocketIoClientDotNet.Client;
using QuantConnect.Logging;
using Newtonsoft.Json.Linq;
using QuantConnect.Data.Market;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using QuantConnect.Interfaces;
using NodaTime;
using System.Globalization;
using static QuantConnect.StringExtensions;
using QuantConnect.Util;
using CoinAPI.WebSocket.V1.DataModels;
using System.Linq;

namespace QuantConnect.ToolBox.IEX
{
    /// <summary>
    /// IEX live data handler.
    /// Data provided for free by IEX. See more at https://iextrading.com/developers/docs/#websockets
    /// </summary>
    public class IEXDataQueueHandler : HistoryProviderBase, IDataQueueHandler
    {
        // using SocketIoClientDotNet is a temp solution until IEX implements standard WebSockets protocol
        private Socket _socket;

        // only required for history requests to IEX Cloud
        private readonly string _apiKey = Config.Get("iex-cloud-api-key");

        private readonly ConcurrentDictionary<string, Symbol> _symbols = new ConcurrentDictionary<string, Symbol>(StringComparer.InvariantCultureIgnoreCase);
        private Manager _manager;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private static DateTime _unixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified);
        private readonly TaskCompletionSource<bool> _connected = new TaskCompletionSource<bool>();
        private bool _subscribedToAll;
        private int _dataPointCount;
        private readonly IDataAggregator _aggregator = Composer.Instance.GetExportedValueByTypeName<IDataAggregator>(
            Config.Get("data-aggregator", "QuantConnect.Lean.Engine.DataFeeds.AggregationManager"));
        private readonly EventBasedDataQueueHandlerSubscriptionManager _subscriptionManager;

        public string Endpoint { get; internal set; }

        public bool IsConnected => _manager.ReadyState == Manager.ReadyStateEnum.OPEN;

        public IEXDataQueueHandler() : this(true)
        {
        }

        public IEXDataQueueHandler(bool live)
        {
            _subscriptionManager = new EventBasedDataQueueHandlerSubscriptionManager();
            _subscriptionManager.SubscribeImpl += (s, t) =>
            {
                Subscribe(s);
                return true;
            };

            _subscriptionManager.UnsubscribeImpl += (s, t) =>
            {
                Unsubscribe(s);
                return true;
            };

            Endpoint = "https://ws-api.iextrading.com/1.0/tops";

            if (string.IsNullOrWhiteSpace(_apiKey))
            {
                Log.Trace("IEXDataQueueHandler(): The IEX API key was not provided, history calls will return no data.");
            }

            if (live)
            {
                Reconnect();
            }
        }

        internal void Reconnect()
        {
            try
            {
                _socket = IO.Socket(Endpoint,
                    new IO.Options()
                    {
                        // default is 1000, default attempts is int.MaxValue
                        ReconnectionDelay = 1000
                    });
                _socket.On(Socket.EVENT_CONNECT, () =>
                {
                    _connected.TrySetResult(true);
                    Log.Trace("IEXDataQueueHandler.Reconnect(): Connected to IEX live data");
                    Log.Trace("IEXDataQueueHandler.Reconnect(): IEX Real-Time Price");
                });

                _socket.On("message", message => ProcessJsonObject(JObject.Parse((string)message)));
                _manager = _socket.Io();
            }
            catch (Exception err)
            {
                Log.Error("IEXDataQueueHandler.Reconnect(): " + err.Message);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void ProcessJsonObject(JObject message)
        {
            try
            {
                // https://iextrading.com/developer/#tops-tops-response
                var symbolString = message["symbol"].Value<string>();
                Symbol symbol;
                if (!_symbols.TryGetValue(symbolString, out symbol))
                {
                    if (_subscribedToAll)
                    {
                        symbol = Symbol.Create(symbolString, SecurityType.Equity, Market.USA);
                    }
                    else
                    {
                        Log.Trace("IEXDataQueueHandler.ProcessJsonObject(): Received unexpected symbol '" + symbolString + "' from IEX in IEXDataQueueHandler");
                        return;
                    }
                }
                var bidSize = message["bidSize"].Value<long>();
                var bidPrice = message["bidPrice"].Value<decimal>();
                var askSize = message["askSize"].Value<long>();
                var askPrice = message["askPrice"].Value<decimal>();
                var volume = message["volume"].Value<int>();
                var lastSalePrice = message["lastSalePrice"].Value<decimal>();
                var lastSaleSize = message["lastSaleSize"].Value<int>();
                var lastSaleTime = message["lastSaleTime"].Value<long>();
                var lastSaleDateTime = _unixEpoch.AddMilliseconds(lastSaleTime);
                var lastUpdated = message["lastUpdated"].Value<long>();
                if (lastUpdated == -1)
                {
                    // there were no trades on this day
                    return;
                }
                var lastUpdatedDatetime = _unixEpoch.AddMilliseconds(lastUpdated);

                var tick = new Tick()
                {
                    Symbol = symbol,
                    Time = lastUpdatedDatetime.ConvertFromUtc(TimeZones.NewYork),
                    TickType = lastUpdatedDatetime == lastSaleDateTime ? TickType.Trade : TickType.Quote,
                    Exchange = "IEX",
                    BidSize = bidSize,
                    BidPrice = bidPrice,
                    AskSize = askSize,
                    AskPrice = askPrice,
                    Value = lastSalePrice,
                    Quantity = lastSaleSize
                };

                _aggregator.Update(tick);
            }
            catch (Exception err)
            {
                // this method should never fail
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

        /// <summary>
        /// Subscribe to symbols
        /// </summary>
        public void Subscribe(IEnumerable<Symbol> symbols)
        {
            try
            {
                var sb = new StringBuilder();
                foreach (var symbol in symbols)
                {
                    // IEX only supports equities
                    if (symbol.Value.Equals("firehose", StringComparison.InvariantCultureIgnoreCase))
                    {
                        _subscribedToAll = true;
                    }
                    if (_symbols.TryAdd(symbol.Value, symbol))
                    {
                        // added new symbol
                        sb.Append(symbol.Value);
                        sb.Append(",");
                    }
                }
                var symbolsList = sb.ToString().TrimEnd(',');
                if (!string.IsNullOrEmpty(symbolsList))
                {
                    SocketSafeAsyncEmit("subscribe", symbolsList);
                }
            }
            catch (Exception err)
            {
                Log.Error("IEXDataQueueHandler.Subscribe(): " + err.Message);
            }
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
        /// Unsubscribe from symbols
        /// </summary>
        public void Unsubscribe(IEnumerable<Symbol> symbols)
        {
            try
            {
                var sb = new StringBuilder();
                foreach (var symbol in symbols)
                {
                    // IEX only supports equities
                    if (symbol.SecurityType != SecurityType.Equity) continue;
                    Symbol tmp;
                    if (_symbols.TryRemove(symbol.Value, out tmp))
                    {
                        // removed existing
                        Trace.Assert(symbol.Value == tmp.Value);
                        sb.Append(symbol.Value);
                        sb.Append(",");
                    }
                }
                var symbolsList = sb.ToString().TrimEnd(',');
                if (!string.IsNullOrEmpty(symbolsList))
                {
                    SocketSafeAsyncEmit("unsubscribe", symbolsList);
                }
            }
            catch (Exception err)
            {
                Log.Error("IEXDataQueueHandler.Unsubscribe(): " + err.Message);
            }
        }

        /// <summary>
        /// This method is used to schedule _socket.Emit request until the connection state is OPEN
        /// </summary>
        /// <param name="symbol"></param>
        private void SocketSafeAsyncEmit(string command, string value)
        {
            Task.Run(async () =>
            {
                await _connected.Task;
                const int retriesLimit = 100;
                var retriesCount = 0;
                while (true)
                {
                    try
                    {
                        if (_manager.ReadyState == Manager.ReadyStateEnum.OPEN)
                        {
                            // there is an ACK functionality in socket.io, but IEX will be moving to standard WebSockets
                            // and this retry logic is just for rare cases of connection interrupts
                            _socket.Emit(command, value);
                            break;
                        }
                    }
                    catch (Exception err)
                    {
                        Log.Error("IEXDataQueueHandler.SocketSafeAsyncEmit(): " + err.Message);
                    }
                    await Task.Delay(100);
                    retriesCount++;
                    if (retriesCount >= retriesLimit)
                    {
                        Log.Error("IEXDataQueueHandler.SocketSafeAsyncEmit(): " +
                                  (new TimeoutException("Cannot subscribe to symbol :" + value)));
                        break;
                    }
                }
            }, _cts.Token)
            .ContinueWith((t) =>
            {
                Log.Error("IEXDataQueueHandler.SocketSafeAsyncEmit(): " + t.Exception.Message);
                return t;

            }, TaskContinuationOptions.OnlyOnFaulted);
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
            if (_socket != null)
            {
                _socket.Disconnect();
                _socket.Close();
            }
            Log.Trace("IEXDataQueueHandler.Dispose(): Disconnected from IEX live data");
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
                yield break;
            }

            foreach (var request in requests)
            {
                foreach (var slice in ProcessHistoryRequests(request))
                {
                    yield return slice;
                }
            }
        }

        /// <summary>
        /// Populate request data
        /// </summary>
        private IEnumerable<Slice> ProcessHistoryRequests(Data.HistoryRequest request)
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

            if (start <= DateTime.Today.AddYears(-5))
            {
                Log.Error("IEXDataQueueHandler.GetHistory(): History calls for IEX only support a maximum of 5 years history.");
                yield break;
            }

            Log.Trace("IEXDataQueueHandler.ProcessHistoryRequests(): Submitting request: " +
                Invariant($"{request.Symbol.SecurityType}-{ticker}: {request.Resolution} {start}->{end}")
            );

            var span = end.Date - start.Date;
            var suffixes = new List<string>();
            if (span.Days < 30 && request.Resolution == Resolution.Minute)
            {
                var begin = start;
                while (begin < end)
                {
                    suffixes.Add("date/" + begin.ToStringInvariant("yyyyMMdd"));
                    begin = begin.AddDays(1);
                }
            }
            else if (span.Days < 30)
            {
                suffixes.Add("1m");
            }
            else if (span.Days < 3 * 30)
            {
                suffixes.Add("3m");
            }
            else if (span.Days < 6 * 30)
            {
                suffixes.Add("6m");
            }
            else if (span.Days < 12 * 30)
            {
                suffixes.Add("1y");
            }
            else if (span.Days < 24 * 30)
            {
                suffixes.Add("2y");
            }
            else
            {
                suffixes.Add("5y");
            }

            // Download and parse data
            using (var client = new System.Net.WebClient())
            {
                foreach (var suffix in suffixes)
                {
                    var response = client.DownloadString("https://cloud.iexapis.com/v1/stock/" + ticker + "/chart/" + suffix + "?token=" + _apiKey);
                    var parsedResponse = JArray.Parse(response);

                    foreach (var item in parsedResponse.Children())
                    {
                        DateTime date;
                        if (item["minute"] != null)
                        {
                            date = DateTime.ParseExact(item["date"].Value<string>(), "yyyy-MM-dd", CultureInfo.InvariantCulture);
                            var mins = TimeSpan.ParseExact(item["minute"].Value<string>(), "hh\\:mm", CultureInfo.InvariantCulture);
                            date += mins;
                        }
                        else
                        {
                            date = Parse.DateTime(item["date"].Value<string>());
                        }

                        if (date.Date < start.Date || date.Date > end.Date)
                        {
                            continue;
                        }

                        Interlocked.Increment(ref _dataPointCount);

                        if (item["open"].Type == JTokenType.Null)
                        {
                            continue;
                        }
                        var open = item["open"].Value<decimal>();
                        var high = item["high"].Value<decimal>();
                        var low = item["low"].Value<decimal>();
                        var close = item["close"].Value<decimal>();
                        var volume = item["volume"].Value<int>();

                        var tradeBar = new TradeBar(date, request.Symbol, open, high, low, close, volume);

                        yield return new Slice(tradeBar.EndTime, new[] {tradeBar});
                    }
                }
            }
        }

        #endregion
    }
}

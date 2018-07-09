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
using QuantConnect.Lean.Engine.DataFeeds.Queues;
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

namespace QuantConnect.ToolBox.IEX
{
    /// <summary>
    /// IEX live data handler.
    /// Data provided for free by IEX. See more at https://iextrading.com/api-exhibit-a
    /// </summary>
    public class IEXDataQueueHandler : LiveDataQueue, IDisposable
    {
        // using SocketIoClientDotNet is a temp solution until IEX implements standard WebSockets protocol
        private Socket _socket;

        private ConcurrentDictionary<string, Symbol> _symbols = new ConcurrentDictionary<string, Symbol>(StringComparer.InvariantCultureIgnoreCase);
        private Manager _manager;
        private CancellationTokenSource _cts = new CancellationTokenSource();
        private static DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Unspecified);
        private TaskCompletionSource<bool> _connected = new TaskCompletionSource<bool>();
        private Task _lastEmitTask;
        private bool _subscribedToAll;

        private BlockingCollection<BaseData> _outputCollection = new BlockingCollection<BaseData>();

        public string Endpoint { get; internal set; }

        public bool IsConnected
        {
            get { return _manager.ReadyState == Manager.ReadyStateEnum.OPEN; }
        }

        public IEXDataQueueHandler()
        {
            Endpoint = "https://ws-api.iextrading.com/1.0/tops";
            Reconnect();
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
                var lastSaleDateTime = UnixEpoch.AddMilliseconds(lastSaleTime);
                var lastUpdated = message["lastUpdated"].Value<long>();
                if (lastUpdated == -1)
                {
                    // there were no trades on this day
                    return;
                }
                var lastUpdatedDatetime = UnixEpoch.AddMilliseconds(lastUpdated);

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
                _outputCollection.TryAdd(tick);
            }
            catch (Exception err)
            {
                // this method should never fail
                Log.Error("IEXDataQueueHandler.ProcessJsonObject(): " + err.Message);
            }
        }

        /// <summary>
        /// Desktop/Local doesn't support live data from this handler
        /// </summary>
        /// <returns>Tick</returns>
        public sealed override IEnumerable<BaseData> GetNextTicks()
        {
            return _outputCollection.GetConsumingEnumerable();
        }

        /// <summary>
        /// Subscribe to symbols
        /// </summary>
        public sealed override void Subscribe(LiveNodePacket job, IEnumerable<Symbol> symbols)
        {
            try
            {
                var sb = new StringBuilder();
                foreach (var symbol in symbols)
                {
                    // IEX only supports equities
                    if (symbol.SecurityType != SecurityType.Equity) continue;
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
                if (!String.IsNullOrEmpty(symbolsList))
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
        /// Unsubscribe from symbols
        /// </summary>
        public sealed override void Unsubscribe(LiveNodePacket job, IEnumerable<Symbol> symbols)
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
                if (!String.IsNullOrEmpty(symbolsList))
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
            _outputCollection.CompleteAdding();
            _cts.Cancel();
            _socket.Disconnect();
            _socket.Close();

            Log.Trace("IEXDataQueueHandler.Dispose(): Disconnected from IEX live data");
        }

        ~IEXDataQueueHandler()
        {
            Dispose(false);
        }
    }
}
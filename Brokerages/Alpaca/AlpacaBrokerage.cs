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
using System.Globalization;
using System.Linq;
using System.Threading;
using NodaTime;
using QuantConnect.Brokerages.Alpaca.Markets;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;
using QuantConnect.Logging;
using QuantConnect.Orders;
using QuantConnect.Securities;
using QuantConnect.Util;
using HistoryRequest = QuantConnect.Data.HistoryRequest;

namespace QuantConnect.Brokerages.Alpaca
{
    /// <summary>
    /// Alpaca Brokerage implementation
    /// </summary>
    [BrokerageFactory(typeof(AlpacaBrokerageFactory))]
    public partial class AlpacaBrokerage : Brokerage, IDataQueueHandler
    {
        private bool _isConnected;
        private Thread _connectionMonitorThread;
        private volatile bool _connectionLost;
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        // Rest API requests must be limited to a maximum of 200 messages/minute
        private readonly RateGate _messagingRateLimiter = new RateGate(200, TimeSpan.FromMinutes(1));

        private readonly RestClient _restClient;
        private readonly SockClient _sockClient;
        private readonly NatsClient _natsClient;

        /// <summary>
        /// This lock is used to sync 'PlaceOrder' and callback 'OnTransactionDataReceived'
        /// </summary>
        private readonly object _locker = new object();

        /// <summary>
        /// The UTC time of the last received heartbeat message
        /// </summary>
        private DateTime _lastHeartbeatUtcTime;

        /// <summary>
        /// A lock object used to synchronize access to LastHeartbeatUtcTime
        /// </summary>
        private readonly object _lockerConnectionMonitor = new object();

        /// <summary>
        /// The order provider
        /// </summary>
        private readonly IOrderProvider _orderProvider;

        /// <summary>
        /// The security provider
        /// </summary>
        private readonly ISecurityProvider _securityProvider;

        /// <summary>
        /// The market hours database
        /// </summary>
        private readonly MarketHoursDatabase _marketHours;

        private readonly Dictionary<Symbol, DateTimeZone> _symbolExchangeTimeZones = new Dictionary<Symbol, DateTimeZone>();

        /// <summary>
        /// Initializes a new instance of the <see cref="AlpacaBrokerage"/> class.
        /// </summary>
        /// <param name="orderProvider">The order provider.</param>
        /// <param name="securityProvider">The holdings provider.</param>
        /// <param name="accountKeyId">The Alpaca api key id</param>
        /// <param name="secretKey">The api secret key</param>
        /// <param name="tradingMode">The Alpaca trading mode. paper/live</param>
        public AlpacaBrokerage(IOrderProvider orderProvider, ISecurityProvider securityProvider, string accountKeyId, string secretKey, string tradingMode)
            : base("Alpaca Brokerage")
        {
            var baseUrl = "api.alpaca.markets";
            if (tradingMode.Equals("paper")) baseUrl = "paper-" + baseUrl;
            baseUrl = "https://" + baseUrl;

            _orderProvider = orderProvider;
            _securityProvider = securityProvider;

            _marketHours = MarketHoursDatabase.FromDataFolder();

            // api client for alpaca
            _restClient = new RestClient(accountKeyId, secretKey, baseUrl);

            // websocket client for alpaca
            _sockClient = new SockClient(accountKeyId, secretKey, baseUrl);
            _sockClient.OnTradeUpdate += OnTradeUpdate;
            _sockClient.OnError += OnSockClientError;
            _sockClient.ConnectAsync().SynchronouslyAwaitTask();

            // polygon client for alpaca
            _natsClient = new NatsClient(accountKeyId, baseUrl.Contains("staging"));
            _natsClient.QuoteReceived += OnQuoteReceived;
            _natsClient.TradeReceived += OnTradeReceived;
            _natsClient.OnError += OnNatsClientError;
            _natsClient.Open();
        }

        #region IBrokerage implementation

        /// <summary>
        /// Returns true if we're currently connected to the broker
        /// </summary>
        public override bool IsConnected => _isConnected && !_connectionLost;

        /// <summary>
        /// Connects the client to the broker's remote servers
        /// </summary>
        public override void Connect()
        {
            if (IsConnected) return;

            _isConnected = true;

            // create new thread to manage disconnections and reconnections
            _cancellationTokenSource = new CancellationTokenSource();
            _connectionMonitorThread = new Thread(() =>
                {
                    var nextReconnectionAttemptUtcTime = DateTime.UtcNow;
                    double nextReconnectionAttemptSeconds = 1;

                    lock (_lockerConnectionMonitor)
                    {
                        _lastHeartbeatUtcTime = DateTime.UtcNow;
                    }

                    try
                    {
                        while (!_cancellationTokenSource.IsCancellationRequested)
                        {
                            TimeSpan elapsed;
                            lock (_lockerConnectionMonitor)
                            {
                                elapsed = DateTime.UtcNow - _lastHeartbeatUtcTime;
                            }

                            if (!_connectionLost && elapsed > TimeSpan.FromSeconds(20))
                            {
                                _connectionLost = true;
                                nextReconnectionAttemptUtcTime = DateTime.UtcNow.AddSeconds(nextReconnectionAttemptSeconds);

                                OnMessage(BrokerageMessageEvent.Disconnected("Connection with Alpaca server lost. " +
                                                                             "This could be because of internet connectivity issues. "));
                            }
                            else if (_connectionLost)
                            {
                                try
                                {
                                    if (elapsed <= TimeSpan.FromSeconds(20))
                                    {
                                        _connectionLost = false;
                                        nextReconnectionAttemptSeconds = 1;

                                        OnMessage(BrokerageMessageEvent.Reconnected("Connection with Alpaca server restored."));
                                    }
                                    else
                                    {
                                        if (DateTime.UtcNow > nextReconnectionAttemptUtcTime)
                                        {
                                            try
                                            {
                                                // check if we have a connection
                                                GetInstrumentList();
                                            }
                                            catch (Exception)
                                            {
                                                // double the interval between attempts (capped to 1 minute)
                                                nextReconnectionAttemptSeconds = Math.Min(nextReconnectionAttemptSeconds * 2, 60);
                                                nextReconnectionAttemptUtcTime = DateTime.UtcNow.AddSeconds(nextReconnectionAttemptSeconds);
                                            }
                                        }
                                    }
                                }
                                catch (Exception exception)
                                {
                                    Log.Error(exception);
                                }
                            }

                            Thread.Sleep(1000);
                        }
                    }
                    catch (Exception exception)
                    {
                        Log.Error(exception);
                    }
                })
                { IsBackground = true };
            _connectionMonitorThread.Start();
            while (!_connectionMonitorThread.IsAlive)
            {
                Thread.Sleep(1);
            }
        }

        /// <summary>
        /// Disconnects the client from the broker's remote servers
        /// </summary>
        public override void Disconnect()
        {
            // request and wait for thread to stop
            _cancellationTokenSource.Cancel();
            _connectionMonitorThread?.Join();

            _sockClient.DisconnectAsync().SynchronouslyAwaitTask();
            _natsClient.Close();

            _isConnected = false;
        }

        /// <summary>
        /// Gets the current cash balance for each currency held in the brokerage account
        /// </summary>
        /// <returns>The current cash balance for each currency available for trading</returns>
        public override List<Cash> GetCashBalance()
        {
            try
            {
                CheckRateLimiting();
                var task = _restClient.GetAccountAsync();
                var balance = task.SynchronouslyAwaitTaskResult();

                return new List<Cash>
                {
                    new Cash("USD",
                        balance.TradableCash,
                        1m)
                };
            }
            catch (Exception e)
            {
                Log.Error(e);
                if (e.InnerException != null)
                    OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, 100, e.InnerException.Message));
                throw;
            }
        }

        /// <summary>
        /// Gets all holdings for the account
        /// </summary>
        /// <returns>The current holdings from the account</returns>
        public override List<Holding> GetAccountHoldings()
        {
            try
            {
                CheckRateLimiting();
                var task = _restClient.ListPositionsAsync();
                var holdings = task.SynchronouslyAwaitTaskResult();

                var qcHoldings = new List<Holding>();
                foreach (var holds in holdings)
                {
                    qcHoldings.Add(ConvertHolding(holds));
                }

                // Set MarketPrice in each Holding
                var alpacaSymbols = qcHoldings
                    .Select(x => x.Symbol.Value)
                    .ToList();

                if (alpacaSymbols.Count > 0)
                {
                    var quotes = GetRates(alpacaSymbols);
                    foreach (var holding in qcHoldings)
                    {
                        var alpacaSymbol = holding.Symbol;
                        Tick tick;
                        if (quotes.TryGetValue(alpacaSymbol.Value, out tick))
                        {
                            holding.MarketPrice = (tick.BidPrice + tick.AskPrice) / 2;
                        }
                    }
                }

                return qcHoldings;
            }
            catch (Exception e)
            {
                Log.Error(e);
                if (e.InnerException != null)
                    OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, 100, e.InnerException.Message));
                throw;
            }
        }

        /// <summary>
        /// Gets all open orders on the account.
        /// NOTE: The order objects returned do not have QC order IDs.
        /// </summary>
        /// <returns>The open orders returned from Alpaca</returns>
        public override List<Order> GetOpenOrders()
        {
            try
            {
                CheckRateLimiting();
                var task = _restClient.ListOrdersAsync();
                var orders = task.SynchronouslyAwaitTaskResult();

                var qcOrders = new List<Order>();
                foreach (var order in orders)
                {
                    qcOrders.Add(ConvertOrder(order));
                }
                return qcOrders;
            }
            catch (Exception e)
            {
                Log.Error(e);
                if (e.InnerException != null)
                    OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, 100, e.InnerException.Message));
                throw;
            }

        }

        /// <summary>
        /// Places a new order and assigns a new broker ID to the order
        /// </summary>
        /// <param name="order">The order to be placed</param>
        /// <returns>True if the request for a new order has been placed, false otherwise</returns>
        public override bool PlaceOrder(Order order)
        {
            const int orderFee = 0;
            order.PriceCurrency = "USD";

            lock (_locker)
            {
                try
                {
                    var apOrder = GenerateAndPlaceOrder(order);
                    order.BrokerId.Add(apOrder.OrderId.ToString());
                }
                catch (Exception e)
                {
                    Log.Error(e);
                    if (e.InnerException != null)
                        OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, 100, e.InnerException.Message));
                    return false;
                }

            }
            OnOrderEvent(new OrderEvent(order, DateTime.UtcNow, orderFee) { Status = Orders.OrderStatus.Submitted });

            return true;
        }

        /// <summary>
        /// Updates the order with the same id
        /// </summary>
        /// <param name="order">The new order information</param>
        /// <returns>True if the request was made for the order to be updated, false otherwise</returns>
        public override bool UpdateOrder(Order order)
        {
            return false;
        }

        /// <summary>
        /// Cancels the order with the specified ID
        /// </summary>
        /// <param name="order">The order to cancel</param>
        /// <returns>True if the request was made for the order to be canceled, false otherwise</returns>
        public override bool CancelOrder(Order order)
        {
            Log.Trace("AlpacaBrokerage.CancelOrder(): " + order);

            if (!order.BrokerId.Any())
            {
                Log.Trace("AlpacaBrokerage.CancelOrder(): Unable to cancel order without BrokerId.");
                return false;
            }

            foreach (var orderId in order.BrokerId)
            {
                CheckRateLimiting();
                var task = _restClient.DeleteOrderAsync(new Guid(orderId));
                task.SynchronouslyAwaitTaskResult();
            }

            return true;
        }

        /// <summary>
        /// Gets the history for the requested security
        /// </summary>
        /// <param name="request">The historical data request</param>
        /// <returns>An enumerable of bars covering the span specified in the request</returns>
        public override IEnumerable<BaseData> GetHistory(HistoryRequest request)
        {
            var exchangeTimeZone = _marketHours.GetExchangeHours(Market.USA, request.Symbol, request.Symbol.SecurityType).TimeZone;

            // set the starting date/time
            var startDateTime = request.StartTimeUtc;

            if (request.Resolution == Resolution.Tick)
            {
                var ticks = DownloadTicks(request.Symbol, startDateTime, request.EndTimeUtc, exchangeTimeZone).ToList();
                if (ticks.Count != 0)
                {
                    foreach (var tick in ticks)
                    {
                        yield return tick;
                    }
                }
            }
            else if (request.Resolution == Resolution.Second)
            {
                var quoteBars = DownloadQuoteBars(request.Symbol, startDateTime, request.EndTimeUtc, request.Resolution, exchangeTimeZone).ToList();
                if (quoteBars.Count != 0)
                {
                    foreach (var quoteBar in quoteBars)
                    {
                        yield return quoteBar;
                    }
                }
            }
            // Due to the slow processing time for QuoteBars in larger resolution, we change into TradeBar in these cases
            else
            {
                var tradeBars = DownloadTradeBars(request.Symbol, startDateTime, request.EndTimeUtc, request.Resolution, exchangeTimeZone).ToList();
                if (tradeBars.Count != 0)
                {
                    tradeBars.RemoveAt(0);
                    foreach (var tradeBar in tradeBars)
                    {
                        yield return tradeBar;
                    }
                }
            }
        }

        #endregion

        /// <summary>
        /// Returns a DateTime from an RFC3339 string (with microsecond resolution)
        /// </summary>
        /// <param name="time">The time string</param>
        public static DateTime GetDateTimeFromString(string time)
        {
            return DateTime.ParseExact(time, "yyyy-MM-dd'T'HH:mm:ss.ffffff'Z'", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Retrieves the current quotes for an instrument
        /// </summary>
        /// <param name="instrument">the instrument to check</param>
        /// <returns>Returns a Tick object with the current bid/ask prices for the instrument</returns>
        public Tick GetRates(string instrument)
        {
            return GetRates(new List<string> { instrument }).Values.First();
        }
    }
}

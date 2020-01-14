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
using QuantConnect.Brokerages.Alpaca.Markets;
using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Logging;
using QuantConnect.Orders;
using QuantConnect.Orders.Fees;
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
        private readonly bool _handlesMarketData;

        /// <summary>
        /// This lock is used to sync 'PlaceOrder' and callback 'OnTradeUpdate'
        /// </summary>
        private readonly object _locker = new object();

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
        /// <param name="handlesMarketData">true if market data subscriptions will be handled by Alpaca</param>
        public AlpacaBrokerage(IOrderProvider orderProvider, ISecurityProvider securityProvider, string accountKeyId, string secretKey, string tradingMode, bool handlesMarketData)
            : base("Alpaca Brokerage")
        {
            _handlesMarketData = handlesMarketData;

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

            // polygon client for alpaca
            _natsClient = new NatsClient(accountKeyId, baseUrl.Contains("staging"));
            _natsClient.QuoteReceived += OnQuoteReceived;
            _natsClient.TradeReceived += OnTradeReceived;
            _natsClient.OnError += OnNatsClientError;
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

            _sockClient.ConnectAsync().SynchronouslyAwaitTask();

            if (_handlesMarketData)
            {
                _natsClient.Open();
            }

            _isConnected = true;

            // create new thread to manage disconnections and reconnections
            var connectionMonitorStartedEvent = new AutoResetEvent(false);
            _cancellationTokenSource = new CancellationTokenSource();
            _connectionMonitorThread = new Thread(() =>
            {
                connectionMonitorStartedEvent.Set();

                var nextReconnectionAttemptSeconds = 1;

                try
                {
                    while (!_cancellationTokenSource.IsCancellationRequested)
                    {
                        var isAlive = true;
                        try
                        {
                            isAlive = _sockClient.IsAlive;
                        }
                        catch (Exception)
                        {
                            // ignored
                        }

                        if (isAlive && _connectionLost)
                        {
                            _connectionLost = false;
                            nextReconnectionAttemptSeconds = 1;

                            OnMessage(BrokerageMessageEvent.Reconnected("Connection with Alpaca server restored."));
                        }
                        else if (!isAlive)
                        {
                            if (_connectionLost)
                            {
                                try
                                {
                                    Thread.Sleep(TimeSpan.FromSeconds(nextReconnectionAttemptSeconds));

                                    _sockClient.ConnectAsync().SynchronouslyAwaitTask();
                                }
                                catch (Exception exception)
                                {
                                    // double the interval between attempts (capped to 1 minute)
                                    nextReconnectionAttemptSeconds = Math.Min(nextReconnectionAttemptSeconds * 2, 60);

                                    Log.Error(exception);
                                }
                            }
                            else
                            {
                                _connectionLost = true;

                                OnMessage(
                                    BrokerageMessageEvent.Disconnected(
                                        "Connection with Alpaca server lost. " +
                                        "This could be because of internet connectivity issues. "));
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

            connectionMonitorStartedEvent.WaitOne();
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

            if (_handlesMarketData)
            {
                _natsClient.Close();
            }

            _isConnected = false;
        }


        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public override void Dispose()
        {
            Log.Trace("AlpacaBrokerage.Dispose(): Disposing of Alpaca brokerage resources.");

            _cancellationTokenSource.Dispose();
            _sockClient?.Dispose();
            _natsClient?.Dispose();

            _messagingRateLimiter.Dispose();
        }

        /// <summary>
        /// Gets the current cash balance for each currency held in the brokerage account
        /// </summary>
        /// <returns>The current cash balance for each currency available for trading</returns>
        public override List<CashAmount> GetCashBalance()
        {
            CheckRateLimiting();

            var task = _restClient.GetAccountAsync();
            var balance = task.SynchronouslyAwaitTaskResult();

            return new List<CashAmount>
            {
                new CashAmount(balance.TradableCash,
                    Currencies.USD)
            };
        }

        /// <summary>
        /// Gets all holdings for the account
        /// </summary>
        /// <returns>The current holdings from the account</returns>
        public override List<Holding> GetAccountHoldings()
        {
            CheckRateLimiting();

            var task = _restClient.ListPositionsAsync();
            var holdings = task.SynchronouslyAwaitTaskResult();

            return holdings.Select(ConvertHolding).ToList();
        }

        /// <summary>
        /// Gets all open orders on the account.
        /// NOTE: The order objects returned do not have QC order IDs.
        /// </summary>
        /// <returns>The open orders returned from Alpaca</returns>
        public override List<Order> GetOpenOrders()
        {
            CheckRateLimiting();

            var task = _restClient.ListOrdersAsync();
            var orders = task.SynchronouslyAwaitTaskResult();

            return orders.Select(ConvertOrder).ToList();
        }

        /// <summary>
        /// Places a new order and assigns a new broker ID to the order
        /// </summary>
        /// <param name="order">The order to be placed</param>
        /// <returns>True if the request for a new order has been placed, false otherwise</returns>
        public override bool PlaceOrder(Order order)
        {
            var orderFee = OrderFee.Zero;
            order.PriceCurrency = Currencies.USD;

            try
            {
                lock (_locker)
                {
                    var apOrder = GenerateAndPlaceOrder(order);
                    order.BrokerId.Add(apOrder.OrderId.ToString());
                }
            }
            catch (Exception e)
            {
                var errorMessage = $"Error placing order: {e.Message}";

                OnOrderEvent(
                    new OrderEvent(order, DateTime.UtcNow, orderFee, "Alpaca Order Event")
                    {
                        Status = Orders.OrderStatus.Invalid,
                        Message = errorMessage
                    });
                OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, -1, errorMessage));

                return true;
            }

            OnOrderEvent(new OrderEvent(order, DateTime.UtcNow, orderFee)
                { Status = Orders.OrderStatus.Submitted });

            return true;
        }

        /// <summary>
        /// Updates the order with the same id
        /// </summary>
        /// <param name="order">The new order information</param>
        /// <returns>True if the request was made for the order to be updated, false otherwise</returns>
        public override bool UpdateOrder(Order order)
        {
            throw new NotSupportedException("AlpacaBrokerage.UpdateOrder(): Order update not supported. Please cancel and re-create.");
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
            var exchangeTimeZone = _marketHours.GetExchangeHours(request.Symbol.ID.Market, request.Symbol, request.Symbol.SecurityType).TimeZone;

            IEnumerable<BaseData> items;
            switch (request.Resolution)
            {
                case Resolution.Tick:
                    items = DownloadTradeTicks(request.Symbol, request.StartTimeUtc, request.EndTimeUtc, exchangeTimeZone);
                    break;

                case Resolution.Second:
                    var ticks = DownloadTradeTicks(request.Symbol, request.StartTimeUtc, request.EndTimeUtc, exchangeTimeZone);
                    items = AggregateTicks(request.Symbol, ticks, request.Resolution.ToTimeSpan());
                    break;

                default:
                    items = DownloadTradeBars(request.Symbol, request.StartTimeUtc, request.EndTimeUtc, request.Resolution, exchangeTimeZone);
                    break;
            }

            foreach (var item in items)
            {
                yield return item;
            }
        }

        #endregion
    }
}

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
using QuantConnect.Brokerages.Oanda.DataType;
using QuantConnect.Brokerages.Oanda.Framework;
using QuantConnect.Brokerages.Oanda.Session;
using QuantConnect.Interfaces;
using QuantConnect.Logging;
using QuantConnect.Orders;
using QuantConnect.Securities;
using Order = QuantConnect.Orders.Order;

namespace QuantConnect.Brokerages.Oanda
{
    /// <summary>
    /// Oanda Brokerage - implementation of IBrokerage interface
    /// </summary>
    public partial class OandaBrokerage : Brokerage, IDataQueueHandler, IHistoryProvider
    {
        private readonly IOrderProvider _orderProvider;
        private readonly ISecurityProvider _securityProvider;
        private readonly Environment _environment;
        private readonly string _accessToken;
        private readonly int _accountId;

        private EventsSession _eventsSession;
        private Dictionary<string, Instrument> _oandaInstruments; 
        private readonly OandaSymbolMapper _symbolMapper = new OandaSymbolMapper();

        private bool _isConnected;

        private DateTime _lastHeartbeatUtcTime;
        private Thread _connectionMonitorThread;
        private readonly object _lockerConnectionMonitor = new object();
        private volatile bool _connectionLost;
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        /// <summary>
        /// Initializes a new instance of the <see cref="OandaBrokerage"/> class.
        /// </summary>
        /// <param name="orderProvider">The order provider.</param>
        /// <param name="securityProvider">The holdings provider.</param>
        /// <param name="environment">The Oanda environment (Trade or Practice)</param>
        /// <param name="accessToken">The Oanda access token (can be the user's personal access token or the access token obtained with OAuth by QC on behalf of the user)</param>
        /// <param name="accountId">The account identifier.</param>
        public OandaBrokerage(IOrderProvider orderProvider, ISecurityProvider securityProvider, Environment environment, string accessToken, int accountId)
            : base("Oanda Brokerage")
        {
            _orderProvider = orderProvider;
            _securityProvider = securityProvider;

            if (environment != Environment.Trade && environment != Environment.Practice)
                throw new NotSupportedException("Oanda Environment not supported: " + environment);

            _environment = environment;
            _accessToken = accessToken;
            _accountId = accountId;
        }

        #region IBrokerage implementation

        /// <summary>
        /// Returns true if we're currently connected to the broker
        /// </summary>
        public override bool IsConnected
        {
            get { return _isConnected && !_connectionLost; }
        }

        /// <summary>
        /// Connects the client to the broker's remote servers
        /// </summary>
        public override void Connect()
        {
            if (IsConnected) return;

            // Load the list of instruments
            _oandaInstruments = GetInstruments().ToDictionary(x => x.instrument);

            // Register to the event session to receive events.
            _eventsSession = new EventsSession(this, _accountId);
            _eventsSession.DataReceived += OnEventReceived;
            _eventsSession.StartSession();

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

                            OnMessage(BrokerageMessageEvent.Disconnected("Connection with Oanda server lost. " +
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

                                    OnMessage(BrokerageMessageEvent.Reconnected("Connection with Oanda server restored."));
                                }
                                else
                                {
                                    if (DateTime.UtcNow > nextReconnectionAttemptUtcTime)
                                    {
                                        try
                                        {
                                            // check if we have a connection
                                            GetInstruments();

                                            // restore events session
                                            if (_eventsSession != null)
                                            {
                                                _eventsSession.DataReceived -= OnEventReceived;
                                                _eventsSession.StopSession();
                                            }
                                            _eventsSession = new EventsSession(this, _accountId);
                                            _eventsSession.DataReceived += OnEventReceived;
                                            _eventsSession.StartSession();

                                            // restore rates session
                                            List<Symbol> symbolsToSubscribe;
                                            lock (_lockerSubscriptions)
                                            {
                                                symbolsToSubscribe = _subscribedSymbols.ToList();
                                            }
                                            SubscribeSymbols(symbolsToSubscribe);
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
            });
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
            if (_eventsSession != null)
            {
                _eventsSession.DataReceived -= OnEventReceived;
                _eventsSession.StopSession();
            }

            if (_ratesSession != null)
            {
                _ratesSession.DataReceived -= OnDataReceived;
                _ratesSession.StopSession();
            }

            // request and wait for thread to stop
            _cancellationTokenSource.Cancel();
            _connectionMonitorThread.Join();

            _isConnected = false;
        }

        /// <summary>
        /// Gets all open orders on the account. 
        /// NOTE: The order objects returned do not have QC order IDs.
        /// </summary>
        /// <returns>The open orders returned from Oanda</returns>
        public override List<Order> GetOpenOrders()
        {
            var oandaOrders = GetOrderList();

            var orderList = oandaOrders.Select(ConvertOrder).ToList();
            return orderList;
        }

        /// <summary>
        /// Gets all holdings for the account
        /// </summary>
        /// <returns>The current holdings from the account</returns>
        public override List<Holding> GetAccountHoldings()
        {
            var holdings = GetPositions(_accountId).Select(ConvertHolding).Where(x => x.Quantity != 0).ToList();

            // Set MarketPrice in each Holding
            var oandaSymbols = holdings
                .Select(x => _symbolMapper.GetBrokerageSymbol(x.Symbol))
                .ToList();

            if (oandaSymbols.Count > 0)
            {
                var quotes = GetRates(oandaSymbols).ToDictionary(x => x.instrument);
                foreach (var holding in holdings)
                {
                    var oandaSymbol = _symbolMapper.GetBrokerageSymbol(holding.Symbol);
                    Price quote;
                    if (quotes.TryGetValue(oandaSymbol, out quote))
                    {
                        holding.MarketPrice = Convert.ToDecimal((quote.bid + quote.ask) / 2);
                    }
                }
            }

            return holdings;
        }

        /// <summary>
        /// Gets the current cash balance for each currency held in the brokerage account
        /// </summary>
        /// <returns>The current cash balance for each currency available for trading</returns>
        public override List<Cash> GetCashBalance()
        {
            var getAccountRequestString = EndpointResolver.ResolveEndpoint(_environment, Server.Account) + "accounts/" + _accountId;
            var accountResponse = MakeRequest<Account>(getAccountRequestString);

            return new List<Cash>
            {
                new Cash(accountResponse.accountCurrency, accountResponse.balance.ToDecimal(),
                    GetUsdConversion(accountResponse.accountCurrency))
            };
        }

        /// <summary>
        /// Places a new order and assigns a new broker ID to the order
        /// </summary>
        /// <param name="order">The order to be placed</param>
        /// <returns>True if the request for a new order has been placed, false otherwise</returns>
        public override bool PlaceOrder(Order order)
        {
            var requestParams = new Dictionary<string, string>
            {
                { "instrument", _symbolMapper.GetBrokerageSymbol(order.Symbol) },
                { "units", Convert.ToInt32(order.AbsoluteQuantity).ToString() }
            };

            PopulateOrderRequestParameters(order, requestParams);

            var postOrderResponse = PostOrderAsync(requestParams);
            if (postOrderResponse == null) 
                return false;

            // if market order, find fill quantity and price
            var marketOrderFillPrice = 0m;
            if (order.Type == OrderType.Market)
            {
                marketOrderFillPrice = Convert.ToDecimal(postOrderResponse.price);
            }

            var marketOrderFillQuantity = 0;
            if (postOrderResponse.tradeOpened != null && postOrderResponse.tradeOpened.id > 0)
            {
                if (order.Type == OrderType.Market)
                {
                    marketOrderFillQuantity = postOrderResponse.tradeOpened.units;
                }
                else
                {
                    order.BrokerId.Add(postOrderResponse.tradeOpened.id.ToString());
                }
            }

            if (postOrderResponse.tradeReduced != null && postOrderResponse.tradeReduced.id > 0)
            {
                if (order.Type == OrderType.Market)
                {
                    marketOrderFillQuantity = postOrderResponse.tradeReduced.units;
                }
                else
                {
                    order.BrokerId.Add(postOrderResponse.tradeReduced.id.ToString());
                }
            }

            if (postOrderResponse.orderOpened != null && postOrderResponse.orderOpened.id > 0)
            {
                if (order.Type != OrderType.Market)
                {
                    order.BrokerId.Add(postOrderResponse.orderOpened.id.ToString());
                }
            }

            if (postOrderResponse.tradesClosed != null && postOrderResponse.tradesClosed.Count > 0)
            {
                marketOrderFillQuantity += postOrderResponse.tradesClosed
                    .Where(trade => order.Type == OrderType.Market)
                    .Sum(trade => trade.units);
            }

            // send Submitted order event
            const int orderFee = 0;
            order.PriceCurrency = _securityProvider.GetSecurity(order.Symbol).SymbolProperties.QuoteCurrency;
            OnOrderEvent(new OrderEvent(order, DateTime.UtcNow, orderFee) { Status = OrderStatus.Submitted });

            if (order.Type == OrderType.Market)
            {
                // if market order, also send Filled order event
                OnOrderEvent(new OrderEvent(order, DateTime.UtcNow, orderFee)
                {
                    Status = OrderStatus.Filled,
                    FillPrice = marketOrderFillPrice,
                    FillQuantity = marketOrderFillQuantity * Math.Sign(order.Quantity)
                });
            }

            return true;
        }


        /// <summary>
        /// Updates the order with the same id
        /// </summary>
        /// <param name="order">The new order information</param>
        /// <returns>True if the request was made for the order to be updated, false otherwise</returns>
        public override bool UpdateOrder(Order order)
        {
            Log.Trace("OandaBrokerage.UpdateOrder(): " + order);
            
            if (!order.BrokerId.Any())
            {
                // we need the brokerage order id in order to perform an update
                Log.Trace("OandaBrokerage.UpdateOrder(): Unable to update order without BrokerId.");
                return false;
            }
            
            var requestParams = new Dictionary<string, string>
            {
                { "instrument", _symbolMapper.GetBrokerageSymbol(order.Symbol) },
                { "units", Convert.ToInt32(order.AbsoluteQuantity).ToString() },
            };

            // we need the brokerage order id in order to perform an update
            PopulateOrderRequestParameters(order, requestParams);

            UpdateOrder(long.Parse(order.BrokerId.First()), requestParams);

            return true;
        }

        /// <summary>
        /// Cancels the order with the specified ID
        /// </summary>
        /// <param name="order">The order to cancel</param>
        /// <returns>True if the request was made for the order to be canceled, false otherwise</returns>
        public override bool CancelOrder(Order order)
        {
            Log.Trace("OandaBrokerage.CancelOrder(): " + order);
            
            if (!order.BrokerId.Any())
            {
                Log.Trace("OandaBrokerage.CancelOrder(): Unable to cancel order without BrokerId.");
                return false;
            }

            foreach (var orderId in order.BrokerId)
            {
                CancelOrder(long.Parse(orderId));
                OnOrderEvent(new OrderEvent(order, DateTime.UtcNow, 0, "Oanda Cancel Order Event") { Status = OrderStatus.Canceled });
            }

            return true;
        }

        #endregion

    }
}

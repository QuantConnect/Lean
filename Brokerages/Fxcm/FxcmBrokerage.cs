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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using com.fxcm.external.api.transport;
using com.fxcm.external.api.transport.listeners;
using com.fxcm.external.api.util;
using com.fxcm.fix;
using com.fxcm.fix.pretrade;
using com.fxcm.fix.trade;
using QuantConnect.Interfaces;
using QuantConnect.Logging;
using QuantConnect.Orders;
using QuantConnect.Securities;

namespace QuantConnect.Brokerages.Fxcm
{
    /// <summary>
    /// FXCM brokerage - implementation of IBrokerage interface
    /// </summary>
    public partial class FxcmBrokerage : Brokerage, IDataQueueHandler, IHistoryProvider, IGenericMessageListener, IStatusMessageListener
    {
        private readonly IOrderProvider _orderProvider;
        private readonly ISecurityProvider _securityProvider;
        private readonly string _server;
        private readonly string _terminal;
        private readonly string _userName;
        private readonly string _password;
        private readonly string _accountId;

        private Thread _orderEventThread;
        private Thread _connectionMonitorThread;

        private readonly object _lockerConnectionMonitor = new object();
        private DateTime _lastReadyMessageTime;
        private volatile bool _connectionLost;
        private volatile bool _connectionError;

        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private readonly ConcurrentQueue<OrderEvent> _orderEventQueue = new ConcurrentQueue<OrderEvent>();
        private readonly FxcmSymbolMapper _symbolMapper = new FxcmSymbolMapper();

        /// <summary>
        /// Creates a new instance of the <see cref="FxcmBrokerage"/> class
        /// </summary>
        /// <param name="orderProvider">The order provider</param>
        /// <param name="securityProvider">The holdings provider</param>
        /// <param name="server">The url of the server</param>
        /// <param name="terminal">The terminal name</param>
        /// <param name="userName">The user name (login id)</param>
        /// <param name="password">The user password</param>
        /// <param name="accountId">The account id</param>
        public FxcmBrokerage(IOrderProvider orderProvider, ISecurityProvider securityProvider, string server, string terminal, string userName, string password, string accountId)
            : base("FXCM Brokerage")
        {
            _orderProvider = orderProvider;
            _securityProvider = securityProvider;
            _server = server;
            _terminal = terminal;
            _userName = userName;
            _password = password;
            _accountId = accountId;

            HistoryResponseTimeout = 5000;
            MaximumHistoryRetryAttempts = 1;
        }

        #region IBrokerage implementation

        /// <summary>
        /// Returns true if we're currently connected to the broker
        /// </summary>
        public override bool IsConnected
        {
            get
            {
                return _gateway != null && _gateway.isConnected() && !_connectionLost;
            }
        }

        /// <summary>
        /// Connects the client to the broker's remote servers
        /// </summary>
        public override void Connect()
        {
            if (IsConnected) return;

            Log.Trace("FxcmBrokerage.Connect()");

            _cancellationTokenSource = new CancellationTokenSource();

            // create new thread to fire order events in queue
            _orderEventThread = new Thread(() =>
            {
                while (!_cancellationTokenSource.IsCancellationRequested)
                {
                    OrderEvent orderEvent;
                    if (!_orderEventQueue.TryDequeue(out orderEvent))
                    {
                        Thread.Sleep(1);
                        continue;
                    }

                    OnOrderEvent(orderEvent);
                }
            });
            _orderEventThread.Start();
            while (!_orderEventThread.IsAlive)
            {
                Thread.Sleep(1);
            }

            // create the gateway
            _gateway = GatewayFactory.createGateway();

            // register the message listeners with the gateway
            _gateway.registerGenericMessageListener(this);
            _gateway.registerStatusMessageListener(this);

            // create local login properties
            var loginProperties = new FXCMLoginProperties(_userName, _password, _terminal, _server);

            // log in
            try
            {
                _gateway.login(loginProperties);
            }
            catch (Exception err)
            {
                var message =
                    err.Message.Contains("ORA-20101") ? "Incorrect login credentials" :
                    err.Message.Contains("ORA-20003") ? "API connections are not available on Mini accounts. If you have a standard account contact api@fxcm.com to enable API access" :
                    err.Message;

                _cancellationTokenSource.Cancel();

                throw new BrokerageException(message, err.InnerException);
            }

            // create new thread to manage disconnections and reconnections
            _connectionMonitorThread = new Thread(() =>
            {
                _lastReadyMessageTime = DateTime.UtcNow;

                try
                {
                    while (!_cancellationTokenSource.IsCancellationRequested)
                    {
                        TimeSpan elapsed;
                        lock (_lockerConnectionMonitor)
                        {
                            elapsed = DateTime.UtcNow - _lastReadyMessageTime;
                        }

                        if (!_connectionLost && elapsed > TimeSpan.FromSeconds(5))
                        {
                            _connectionLost = true;

                            OnMessage(BrokerageMessageEvent.Disconnected("Connection with FXCM server lost. " +
                                                                         "This could be because of internet connectivity issues. "));
                        }
                        else if (_connectionLost && elapsed <= TimeSpan.FromSeconds(5) && IsWithinTradingHours())
                        {
                            Log.Trace("FxcmBrokerage.Connect(): Attempting reconnection...");

                            try
                            {
                                _gateway.relogin();

                                _connectionLost = false;

                                OnMessage(BrokerageMessageEvent.Reconnected("Connection with FXCM server restored."));
                            }
                            catch (Exception exception)
                            {
                                Log.Error(exception);
                            }
                        }
                        else if (_connectionError && IsWithinTradingHours())
                        {
                            Log.Trace("FxcmBrokerage.Connect(): Attempting reconnection...");

                            try
                            {
                                // log out
                                _gateway.logout();

                                // remove the message listeners
                                _gateway.removeGenericMessageListener(this);
                                _gateway.removeStatusMessageListener(this);

                                // register the message listeners with the gateway
                                _gateway.registerGenericMessageListener(this);
                                _gateway.registerStatusMessageListener(this);

                                // log in
                                _gateway.login(loginProperties);

                                // load instruments, accounts, orders, positions
                                LoadInstruments();
                                if (!EnableOnlyHistoryRequests)
                                {
                                    LoadAccounts();
                                    LoadOpenOrders();
                                    LoadOpenPositions();
                                }

                                _connectionError = false;
                                _connectionLost = false;

                                OnMessage(BrokerageMessageEvent.Reconnected("Connection with FXCM server restored."));
                            }
                            catch (Exception exception)
                            {
                                Log.Error(exception);
                            }
                        }

                        Thread.Sleep(5000);
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

            // load instruments, accounts, orders, positions
            LoadInstruments();
            if (!EnableOnlyHistoryRequests)
            {
                LoadAccounts();
                LoadOpenOrders();
                LoadOpenPositions();
            }
        }

        /// <summary>
        /// Returns true if we are within FXCM trading hours
        /// </summary>
        /// <returns></returns>
        private static bool IsWithinTradingHours()
        {
            var time = DateTime.UtcNow.ConvertFromUtc(TimeZones.EasternStandard);

            // FXCM Trading Hours: http://help.fxcm.com/us/Trading-Basics/New-to-Forex/38757093/What-are-the-Trading-Hours.htm

            return !(time.DayOfWeek == DayOfWeek.Friday && time.TimeOfDay > new TimeSpan(16, 55, 0) ||
                     time.DayOfWeek == DayOfWeek.Saturday ||
                     time.DayOfWeek == DayOfWeek.Sunday && time.TimeOfDay < new TimeSpan(17, 0, 0) ||
                     time.Month == 12 && time.Day == 25 ||
                     time.Month == 1 && time.Day == 1);
        }

        /// <summary>
        /// Disconnects the client from the broker's remote servers
        /// </summary>
        public override void Disconnect()
        {
            Log.Trace("FxcmBrokerage.Disconnect()");

            if (_gateway != null)
            {
                // log out
                if (_gateway.isConnected())
                    _gateway.logout();

                // remove the message listeners
                _gateway.removeGenericMessageListener(this);
                _gateway.removeStatusMessageListener(this);
            }

            // request and wait for thread to stop
            if (_cancellationTokenSource != null) _cancellationTokenSource.Cancel();
            if (_orderEventThread != null) _orderEventThread.Join();
            if (_connectionMonitorThread != null) _connectionMonitorThread.Join();
        }

        /// <summary>
        /// Gets all open orders on the account. 
        /// NOTE: The order objects returned do not have QC order IDs.
        /// </summary>
        /// <returns>The open orders returned from FXCM</returns>
        public override List<Order> GetOpenOrders()
        {
            Log.Trace(string.Format("FxcmBrokerage.GetOpenOrders(): Located {0} orders", _openOrders.Count));
            var orders = _openOrders.Values.ToList()
                .Where(x => OrderIsOpen(x.getFXCMOrdStatus().getCode()))
                .Select(ConvertOrder)
                .ToList();
            return orders;
        }

        /// <summary>
        /// Gets all holdings for the account
        /// </summary>
        /// <returns>The current holdings from the account</returns>
        public override List<Holding> GetAccountHoldings()
        {
            Log.Trace("FxcmBrokerage.GetAccountHoldings()");

            var holdings = _openPositions.Values.Select(ConvertHolding).Where(x => x.Quantity != 0).ToList();

            // Set MarketPrice in each Holding
            var fxcmSymbols = holdings
                .Select(x => _symbolMapper.GetBrokerageSymbol(x.Symbol))
                .ToList();

            if (fxcmSymbols.Count > 0)
            {
                var quotes = GetQuotes(fxcmSymbols).ToDictionary(x => x.getInstrument().getSymbol());
                foreach (var holding in holdings)
                {
                    MarketDataSnapshot quote;
                    if (quotes.TryGetValue(_symbolMapper.GetBrokerageSymbol(holding.Symbol), out quote))
                    {
                        holding.MarketPrice = Convert.ToDecimal((quote.getBidClose() + quote.getAskClose()) / 2);
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
            Log.Trace("FxcmBrokerage.GetCashBalance()");
            var cashBook = new List<Cash>();

            //Adds the account currency USD to the cashbook.
            cashBook.Add(new Cash(_fxcmAccountCurrency,
                        Convert.ToDecimal(_accounts[_accountId].getCashOutstanding()),
                        GetUsdConversion(_fxcmAccountCurrency)));

            foreach (var trade in _openPositions.Values)
            {
                //settlement price for the trade
                var settlementPrice = Convert.ToDecimal(trade.getSettlPrice());
                //direction of trade
                var direction = trade.getPositionQty().getLongQty() > 0 ? 1 : -1;
                //quantity of the asset
                var quantity = Convert.ToDecimal(trade.getPositionQty().getQty());
                //quantity of base currency
                var baseQuantity = direction * quantity;
                //quantity of quote currency
                var quoteQuantity = -direction * quantity * settlementPrice;
                //base currency
                var baseCurrency = trade.getCurrency();
                //quote currency
                var quoteCurrency = FxcmSymbolMapper.ConvertFxcmSymbolToLeanSymbol(trade.getInstrument().getSymbol());
                quoteCurrency = quoteCurrency.Substring(quoteCurrency.Length - 3);

                var baseCurrencyObject = (from cash in cashBook where cash.Symbol == baseCurrency select cash).FirstOrDefault();
                //update the value of the base currency
                if (baseCurrencyObject != null)
                {
                    baseCurrencyObject.AddAmount(baseQuantity);
                }
                else
                {
                    //add the base currency if not present
                    cashBook.Add(new Cash(baseCurrency, baseQuantity, GetUsdConversion(baseCurrency)));
                }

                var quoteCurrencyObject = (from cash in cashBook where cash.Symbol == quoteCurrency select cash).FirstOrDefault();
                //update the value of the quote currency
                if (quoteCurrencyObject != null)
                {
                    quoteCurrencyObject.AddAmount(quoteQuantity);
                }
                else
                {
                    //add the quote currency if not present
                    cashBook.Add(new Cash(quoteCurrency, quoteQuantity, GetUsdConversion(quoteCurrency)));
                }
            }
            return cashBook;
        }

        /// <summary>
        /// Places a new order and assigns a new broker ID to the order
        /// </summary>
        /// <param name="order">The order to be placed</param>
        /// <returns>True if the request for a new order has been placed, false otherwise</returns>
        public override bool PlaceOrder(Order order)
        {
            Log.Trace("FxcmBrokerage.PlaceOrder(): {0}", order);

            if (!IsConnected)
                throw new InvalidOperationException("FxcmBrokerage.PlaceOrder(): Unable to place order while not connected.");

            if (order.Direction != OrderDirection.Buy && order.Direction != OrderDirection.Sell)
                throw new ArgumentException("FxcmBrokerage.PlaceOrder(): Invalid Order Direction");

            var fxcmSymbol = _symbolMapper.GetBrokerageSymbol(order.Symbol);
            var orderSide = order.Direction == OrderDirection.Buy ? SideFactory.BUY : SideFactory.SELL;
            var quantity = (double)order.AbsoluteQuantity;

            OrderSingle orderRequest;
            switch (order.Type)
            {
                case OrderType.Market:
                    orderRequest = MessageGenerator.generateMarketOrder(_accountId, quantity, orderSide, fxcmSymbol, "");
                    break;

                case OrderType.Limit:
                    var limitPrice = (double)((LimitOrder)order).LimitPrice;
                    orderRequest = MessageGenerator.generateOpenOrder(limitPrice, _accountId, quantity, orderSide, fxcmSymbol, "");
                    orderRequest.setOrdType(OrdTypeFactory.LIMIT);
                    orderRequest.setTimeInForce(TimeInForceFactory.GOOD_TILL_CANCEL);
                    break;

                case OrderType.StopMarket:
                    var stopPrice = (double)((StopMarketOrder)order).StopPrice;
                    orderRequest = MessageGenerator.generateOpenOrder(stopPrice, _accountId, quantity, orderSide, fxcmSymbol, "");
                    orderRequest.setOrdType(OrdTypeFactory.STOP);
                    orderRequest.setTimeInForce(TimeInForceFactory.GOOD_TILL_CANCEL);
                    break;

                default:
                    throw new NotSupportedException("FxcmBrokerage.PlaceOrder(): Order type " + order.Type + " is not supported.");
            }

            _isOrderSubmitRejected = false;
            AutoResetEvent autoResetEvent;
            lock (_locker)
            {
                _currentRequest = _gateway.sendMessage(orderRequest);
                _mapRequestsToOrders[_currentRequest] = order;
                autoResetEvent = new AutoResetEvent(false);
                _mapRequestsToAutoResetEvents[_currentRequest] = autoResetEvent;
            }
            if (!autoResetEvent.WaitOne(ResponseTimeout))
                throw new TimeoutException(string.Format("FxcmBrokerage.PlaceOrder(): Operation took longer than {0} seconds.", (decimal)ResponseTimeout / 1000));

            return !_isOrderSubmitRejected;
        }

        /// <summary>
        /// Updates the order with the same id
        /// </summary>
        /// <param name="order">The new order information</param>
        /// <returns>True if the request was made for the order to be updated, false otherwise</returns>
        public override bool UpdateOrder(Order order)
        {
            Log.Trace("FxcmBrokerage.UpdateOrder(): {0}", order);

            if (!IsConnected)
                throw new InvalidOperationException("FxcmBrokerage.UpdateOrder(): Unable to update order while not connected.");

            if (!order.BrokerId.Any())
            {
                // we need the brokerage order id in order to perform an update
                Log.Trace("FxcmBrokerage.UpdateOrder(): Unable to update order without BrokerId.");
                return false;
            }

            var fxcmOrderId = order.BrokerId[0].ToString();

            ExecutionReport fxcmOrder;
            if (!_openOrders.TryGetValue(fxcmOrderId, out fxcmOrder))
                throw new ArgumentException("FxcmBrokerage.UpdateOrder(): FXCM order id not found: " + fxcmOrderId);

            double price;
            switch (order.Type)
            {
                case OrderType.Limit:
                    price = (double)((LimitOrder)order).LimitPrice;
                    break;

                case OrderType.StopMarket:
                    price = (double)((StopMarketOrder)order).StopPrice;
                    break;

                default:
                    throw new NotSupportedException("FxcmBrokerage.UpdateOrder(): Invalid order type.");
            }

            _isOrderUpdateOrCancelRejected = false;
            var orderReplaceRequest = MessageGenerator.generateOrderReplaceRequest("", fxcmOrder.getOrderID(), fxcmOrder.getSide(), fxcmOrder.getOrdType(), price, fxcmOrder.getAccount());
            orderReplaceRequest.setInstrument(fxcmOrder.getInstrument());
            orderReplaceRequest.setOrderQty((double)order.AbsoluteQuantity);

            AutoResetEvent autoResetEvent;
            lock (_locker)
            {
                _currentRequest = _gateway.sendMessage(orderReplaceRequest);
                autoResetEvent = new AutoResetEvent(false);
                _mapRequestsToAutoResetEvents[_currentRequest] = autoResetEvent;
            }
            if (!autoResetEvent.WaitOne(ResponseTimeout))
                throw new TimeoutException(string.Format("FxcmBrokerage.UpdateOrder(): Operation took longer than {0} seconds.", (decimal)ResponseTimeout / 1000));

            return !_isOrderUpdateOrCancelRejected;
        }

        /// <summary>
        /// Cancels the order with the specified ID
        /// </summary>
        /// <param name="order">The order to cancel</param>
        /// <returns>True if the request was made for the order to be canceled, false otherwise</returns>
        public override bool CancelOrder(Order order)
        {
            Log.Trace("FxcmBrokerage.CancelOrder(): {0}", order);

            if (!IsConnected)
                throw new InvalidOperationException("FxcmBrokerage.UpdateOrder(): Unable to cancel order while not connected.");

            if (!order.BrokerId.Any())
            {
                // we need the brokerage order id in order to perform a cancellation
                Log.Trace("FxcmBrokerage.CancelOrder(): Unable to cancel order without BrokerId.");
                return false;
            }

            var fxcmOrderId = order.BrokerId[0].ToString();

            ExecutionReport fxcmOrder;
            if (!_openOrders.TryGetValue(fxcmOrderId, out fxcmOrder))
                throw new ArgumentException("FxcmBrokerage.CancelOrder(): FXCM order id not found: " + fxcmOrderId);

            _isOrderUpdateOrCancelRejected = false;
            var orderCancelRequest = MessageGenerator.generateOrderCancelRequest("", fxcmOrder.getOrderID(), fxcmOrder.getSide(), fxcmOrder.getAccount());
            AutoResetEvent autoResetEvent;
            lock (_locker)
            {
                _currentRequest = _gateway.sendMessage(orderCancelRequest);
                autoResetEvent = new AutoResetEvent(false);
                _mapRequestsToAutoResetEvents[_currentRequest] = autoResetEvent;
            }
            if (!autoResetEvent.WaitOne(ResponseTimeout))
                throw new TimeoutException(string.Format("FxcmBrokerage.CancelOrder(): Operation took longer than {0} seconds.", (decimal)ResponseTimeout / 1000));

            return !_isOrderUpdateOrCancelRejected;
        }

        #endregion

    }
}

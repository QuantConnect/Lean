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
using com.fxcm.messaging.util;
using QuantConnect.Orders;
using QuantConnect.Securities;
using Log = QuantConnect.Logging.Log;

namespace QuantConnect.Brokerages.Fxcm
{
    /// <summary>
    /// The FXCM brokerage implementation
    /// </summary>
    public partial class FxcmBrokerage : Brokerage, IGenericMessageListener, IStatusMessageListener
    {
        private readonly IOrderProvider _orderProvider;
        private readonly IHoldingsProvider _holdingsProvider;
        private readonly string _server;
        private readonly string _terminal;
        private readonly string _userName;
        private readonly string _password;

        private bool _isConnected;

        private Thread _orderEventThread;
        private volatile bool _orderEventThreadStopped;
        private readonly ConcurrentQueue<OrderEvent> _orderEventQueue = new ConcurrentQueue<OrderEvent>();

        /// <summary>
        /// Creates a new instance of the <see cref="FxcmBrokerage"/> class
        /// </summary>
        /// <param name="orderProvider">The order provider</param>
        /// <param name="holdingsProvider">The holdings provider</param>
        /// <param name="server">The url of the server</param>
        /// <param name="terminal">The terminal name</param>
        /// <param name="userName">The user name (login id)</param>
        /// <param name="password">The user password</param>
        public FxcmBrokerage(IOrderProvider orderProvider, IHoldingsProvider holdingsProvider, string server, string terminal, string userName, string password)
            : base("FXCM Brokerage")
        {
            _orderProvider = orderProvider;
            _holdingsProvider = holdingsProvider;
            _server = server;
            _terminal = terminal;
            _userName = userName;
            _password = password;
        }

        #region IBrokerage implementation

        /// <summary>
        /// Returns true if we're currently connected to the broker
        /// </summary>
        public override bool IsConnected
        {
            get { return _isConnected; }
        }

        /// <summary>
        /// Connects the client to the broker's remote servers
        /// </summary>
        public override void Connect()
        {
            if (IsConnected) return;

            Log.TraceFormat("FxcmBrokerage.Connect()");

            // create new thread to fire order events in queue
            _orderEventThread = new Thread(() =>
            {
                while (!_orderEventThreadStopped)
                {
                    OrderEvent orderEvent;
                    if (_orderEventQueue.IsEmpty || !_orderEventQueue.TryDequeue(out orderEvent))
                    {
                        Thread.Sleep(1);
                        continue;
                    }

                    OnOrderEvent(orderEvent);
                }
            });
            _orderEventThread.Start();
            while (!_orderEventThread.IsAlive) {}

            // create the gateway
            _gateway = GatewayFactory.createGateway();

            // register the message listeners with the gateway
            _gateway.registerGenericMessageListener(this);
            _gateway.registerStatusMessageListener(this);

            // create local login properties
            var loginProperties = new FXCMLoginProperties(_userName, _password, _terminal, _server);

            // disable the streaming rates (default automatic subscriptions)
            loginProperties.addProperty(IConnectionManager.__Fields.MSG_FLAGS, IFixDefs.__Fields.CHANNEL_MARKET_DATA.ToString());

            // log in
            _gateway.login(loginProperties);
            _isConnected = true;

            // load instruments, accounts, orders, positions
            LoadInstruments();
            LoadAccounts();
            LoadOpenOrders();
            LoadOpenPositions();
        }

        /// <summary>
        /// Disconnects the client from the broker's remote servers
        /// </summary>
        public override void Disconnect()
        {
            if (!IsConnected) return;

            Log.TraceFormat("FxcmBrokerage.Disconnect()");

            // log out
            _gateway.logout();

            // remove the message listeners
            _gateway.removeGenericMessageListener(this);
            _gateway.removeStatusMessageListener(this);

            // request and wait for thread to stop
            _orderEventThreadStopped = true;
            _orderEventThread.Join();
        }

        /// <summary>
        /// Gets all open orders on the account. 
        /// NOTE: The order objects returned do not have QC order IDs.
        /// </summary>
        /// <returns>The open orders returned from FXCM</returns>
        public override List<Order> GetOpenOrders()
        {
            Log.TraceFormat("FxcmBrokerage.GetOpenOrders()");

            return _openOrders.Values.ToList().Where(x => OrderIsOpen(x.getFXCMOrdStatus().getCode())).Select(ConvertOrder).ToList();
        }

        /// <summary>
        /// Gets all holdings for the account
        /// </summary>
        /// <returns>The current holdings from the account</returns>
        public override List<Holding> GetAccountHoldings()
        {
            Log.TraceFormat("FxcmBrokerage.GetAccountHoldings()");

            var holdings = _openPositions.Values.Select(ConvertHolding).ToList();

            // Set MarketPrice in each Holding
            var symbols = holdings.Select(x => ConvertSymbolToFxcmSymbol(x.Symbol)).ToList();
            if (symbols.Count > 0)
            {
                var quotes = GetQuotes(symbols).ToDictionary(x => x.getInstrument().getSymbol());
                foreach (var holding in holdings)
                {
                    MarketDataSnapshot quote;
                    if (quotes.TryGetValue(ConvertSymbolToFxcmSymbol(holding.Symbol), out quote))
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
            Log.TraceFormat("FxcmBrokerage.GetCashBalance()");

            return _accounts.Values.Select(account => 
                new Cash(_fxcmAccountCurrency, Convert.ToDecimal(account.getCashOutstanding()), GetUsdConversion(_fxcmAccountCurrency))).ToList();
        }

        /// <summary>
        /// Places a new order and assigns a new broker ID to the order
        /// </summary>
        /// <param name="order">The order to be placed</param>
        /// <returns>True if the request for a new order has been placed, false otherwise</returns>
        public override bool PlaceOrder(Order order)
        {
            Log.TraceFormat("FxcmBrokerage.PlaceOrder(): {0}", order);

            if (order.Direction != OrderDirection.Buy && order.Direction != OrderDirection.Sell)
                throw new ArgumentException("FxcmBrokerage.PlaceOrder(): Invalid Order Direction");

            var symbol = ConvertSymbolToFxcmSymbol(order.Symbol);
            var orderSide = order.Direction == OrderDirection.Buy ? SideFactory.BUY : SideFactory.SELL;
            var quantity = (double)order.AbsoluteQuantity;
            var accountId = _accounts.Keys.First();

            OrderSingle orderRequest;
            switch (order.Type)
            {
                case OrderType.Market:
                    orderRequest = MessageGenerator.generateMarketOrder(accountId, quantity, orderSide, symbol, "");
                    break;

                case OrderType.Limit:
                    var limitPrice = (double)((LimitOrder)order).LimitPrice;
                    orderRequest = MessageGenerator.generateOpenOrder(limitPrice, accountId, quantity, orderSide, symbol, "");
                    orderRequest.setOrdType(OrdTypeFactory.LIMIT);
                    orderRequest.setTimeInForce(TimeInForceFactory.GOOD_TILL_CANCEL);
                    break;

                case OrderType.StopMarket:
                    var stopPrice = (double)((StopMarketOrder)order).StopPrice;
                    orderRequest = MessageGenerator.generateOpenOrder(stopPrice, accountId, quantity, orderSide, symbol, "");
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
            Log.TraceFormat("FxcmBrokerage.UpdateOrder(): {0}", order);

            if (!order.BrokerId.Any())
            {
                // we need the brokerage order id in order to perform an update
                Log.TraceFormat("FxcmBrokerage.UpdateOrder(): Unable to update order without BrokerId.");
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
            Log.TraceFormat("FxcmBrokerage.CancelOrder(): {0}", order);

            if (!order.BrokerId.Any())
            {
                // we need the brokerage order id in order to perform a cancellation
                Log.TraceFormat("FxcmBrokerage.CancelOrder(): Unable to cancel order without BrokerId.");
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

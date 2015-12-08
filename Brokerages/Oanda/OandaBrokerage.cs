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
using QuantConnect.Brokerages.Oanda.DataType;
using QuantConnect.Brokerages.Oanda.Framework;
using QuantConnect.Brokerages.Oanda.Session;
using QuantConnect.Logging;
using QuantConnect.Orders;
using QuantConnect.Securities;
using Order = QuantConnect.Orders.Order;

namespace QuantConnect.Brokerages.Oanda
{
    /// <summary>
    /// Oanda Brokerage - implementation of IBrokerage interface
    /// </summary>
    public partial class OandaBrokerage : Brokerage
    {
        private readonly IOrderProvider _orderProvider;
        private readonly IHoldingsProvider _holdingsProvider;
        private readonly Environment _environment;
        private readonly string _accessToken;
        private readonly int _accountId;

        private readonly OandaSymbolMapper _symbolMapper = new OandaSymbolMapper();

        private bool _isConnected = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="OandaBrokerage"/> class.
        /// </summary>
        /// <param name="orderProvider">The order provider.</param>
        /// <param name="holdingsProvider">The holdings provider.</param>
        /// <param name="environment">The Oanda environment (Trade or Practice)</param>
        /// <param name="accessToken">The Oanda access token (can be the user's personal access token or the access token obtained with OAuth by QC on behalf of the user)</param>
        /// <param name="accountId">The account identifier.</param>
        public OandaBrokerage(IOrderProvider orderProvider, IHoldingsProvider holdingsProvider, Environment environment, string accessToken, int accountId)
            : base("Oanda Brokerage")
        {
            _orderProvider = orderProvider;
            _holdingsProvider = holdingsProvider;

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
            get { return _isConnected; }
        }

        /// <summary>
        /// Connects the client to the broker's remote servers
        /// </summary>
        public override void Connect()
        {
            if (IsConnected) return;

            // Register to the event session to receive events.
            var session = new EventsSession(this, _accountId);
            session.DataReceived += OnEventReceived;
            session.StartSession();

            _isConnected = true;
        }

        /// <summary>
        /// Disconnects the client from the broker's remote servers
        /// </summary>
        public override void Disconnect()
        {
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
            var holdings = GetPositions(_accountId).Select(ConvertHolding).ToList();
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

            Log.Trace(order.ToString());


            var priorOrderPositions = GetTradeList(requestParams);

            var postOrderResponse = PostOrderAsync(requestParams);

            if (postOrderResponse != null)
            {
                if (postOrderResponse.tradeOpened != null)
                {
                    order.BrokerId.Add(postOrderResponse.tradeOpened.id);
                }
                
                if (postOrderResponse.tradeReduced != null)
                {
                    order.BrokerId.Add(postOrderResponse.tradeReduced.id);
                }

                if (postOrderResponse.orderOpened != null)
                {
                    order.BrokerId.Add(postOrderResponse.orderOpened.id);
                }

                const int orderFee = 0;
                OnOrderEvent(new OrderEvent(order, DateTime.UtcNow, orderFee) { Status = OrderStatus.Submitted });
            } 
            else
            {
                return false;
            }

            // we need to determine if there was an existing order and wheter we closed it with market orders.

            if (order.Type == OrderType.Market && order.Direction == OrderDirection.Buy)
            {
                //assume that we are opening a new buy market order
                if (postOrderResponse.tradeOpened != null && postOrderResponse.tradeOpened.id > 0)
                {
                    var tradeOpenedId = postOrderResponse.tradeOpened.id;
                    requestParams = new Dictionary<string, string>();
                    var tradeListResponse = GetTradeList(requestParams);
                    if (tradeListResponse.trades.Any(trade => trade.id == tradeOpenedId))
                    {
                        order.BrokerId.Add(tradeOpenedId);
                        const int orderFee = 0;
                        OnOrderEvent(new OrderEvent(order, DateTime.UtcNow, orderFee) { Status = OrderStatus.Filled });
                    }
                }

                if (postOrderResponse.tradesClosed != null)
                {
                    var tradePositionClosedIds = postOrderResponse.tradesClosed.Select(tradesClosed => tradesClosed.id).ToList();
                    var priorOrderPositionIds = priorOrderPositions.trades.Select(previousTrade => previousTrade.id).ToList();
                    var verifyClosedOrder = tradePositionClosedIds.Intersect(priorOrderPositionIds).Count() == tradePositionClosedIds.Count();
                    if (verifyClosedOrder)
                    {
                        const int orderFee = 0;
                        OnOrderEvent(new OrderEvent(order, DateTime.UtcNow, orderFee) { Status = OrderStatus.Filled });
                    }
                }
            }

            if (order.Type == OrderType.Market && order.Direction == OrderDirection.Sell)
            {                
                //assume that we are opening a new buy market order
                if (postOrderResponse.tradeOpened != null && postOrderResponse.tradeOpened.id > 0)
                {
                    var tradeOpenedId = postOrderResponse.tradeOpened.id;
                    requestParams = new Dictionary<string, string>();
                    var tradeListResponse = GetTradeList(requestParams);
                    if (tradeListResponse.trades.Any(trade => trade.id == tradeOpenedId))
                    {
                        order.BrokerId.Add(tradeOpenedId);
                        const int orderFee = 0;
                        OnOrderEvent(new OrderEvent(order, DateTime.UtcNow, orderFee) { Status = OrderStatus.Filled });
                    }
                }

                if (postOrderResponse.tradesClosed != null)
                {
                    var tradePositionClosedIds = postOrderResponse.tradesClosed.Select(tradesClosed => tradesClosed.id).ToList();
                    var priorOrderPositionIds = priorOrderPositions.trades.Select(previousTrade => previousTrade.id).ToList();
                    var verifyClosedOrder = tradePositionClosedIds.Intersect(priorOrderPositionIds).Count() == tradePositionClosedIds.Count();
                    if (verifyClosedOrder)
                    {
                        const int orderFee = 0;
                        OnOrderEvent(new OrderEvent(order, DateTime.UtcNow, orderFee) { Status = OrderStatus.Filled });
                    }
                }
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

            UpdateOrder(order.BrokerId.First(), requestParams);

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
                CancelOrder(orderId);
                OnOrderEvent(new OrderEvent(order, DateTime.UtcNow, 0, "Oanda Cancel Order Event") { Status = OrderStatus.Canceled });
            }

            return true;
        }

        #endregion

    }
}

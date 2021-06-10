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
using Newtonsoft.Json;
using Python.Runtime;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;
using QuantConnect.Logging;
using QuantConnect.Orders;
using QuantConnect.Orders.Fees;
using QuantConnect.Packets;
using QuantConnect.Python;
using QuantConnect.Securities;
using Order = QuantConnect.Brokerages.Ccxt.Messages.Order;

namespace QuantConnect.Brokerages.Ccxt
{
    /// <summary>
    /// CCXT brokerage implementation
    /// </summary>
    [BrokerageFactory(typeof(CcxtBrokerageFactory))]
    public class CcxtBrokerage : Brokerage, IDataQueueHandler
    {
        private readonly IOrderProvider _orderProvider;
        private readonly string _exchangeName;
        private readonly string _apiKey;
        private readonly string _secret;
        private readonly string _password;
        private readonly IDataAggregator _aggregator;

        private readonly EventBasedDataQueueHandlerSubscriptionManager _subscriptionManager;
        private readonly CcxtSymbolMapper _symbolMapper;

        private dynamic _pyBridge;
        private bool _isConnected;

        /// <summary>
        /// Constructor for brokerage
        /// </summary>
        /// <param name="orderProvider">An instance of IOrderProvider used to fetch Order objects by brokerage ID</param>
        /// <param name="exchangeName">The name of the exchange</param>
        /// <param name="apiKey">The exchange api key</param>
        /// <param name="secret">The exchange secret</param>
        /// <param name="password">The exchange password</param>
        /// <param name="aggregator">the aggregator for consolidating ticks</param>
        public CcxtBrokerage(IOrderProvider orderProvider, string exchangeName, string apiKey, string secret, string password, IDataAggregator aggregator)
            : base($"CCXT Brokerage [{exchangeName}]")
        {
            _orderProvider = orderProvider;
            _exchangeName = exchangeName;
            _apiKey = apiKey;
            _secret = secret;
            _password = password;
            _aggregator = aggregator;

            _symbolMapper = new CcxtSymbolMapper(exchangeName);

            PythonInitializer.Initialize();
            PythonInitializer.AddPythonPaths(new[] { "./Ccxt" });

            _subscriptionManager = new EventBasedDataQueueHandlerSubscriptionManager();
            _subscriptionManager.SubscribeImpl += (s, _) => Subscribe(s);
            _subscriptionManager.UnsubscribeImpl += (s, _) => Unsubscribe(s);
        }

        #region IDataQueueHandler

        /// <summary>
        /// Checks if the websocket connection is connected or in the process of connecting
        /// </summary>
        public override bool IsConnected => _isConnected;

        /// <summary>
        /// Sets the job we're subscribing for
        /// </summary>
        /// <param name="job">Job we're subscribing for</param>
        public void SetJob(LiveNodePacket job)
        {
        }

        /// <summary>
        /// Subscribe to the specified configuration
        /// </summary>
        /// <param name="dataConfig">defines the parameters to subscribe to a data feed</param>
        /// <param name="newDataAvailableHandler">handler to be fired on new data available</param>
        /// <returns>The new enumerator for this subscription request</returns>
        public IEnumerator<BaseData> Subscribe(SubscriptionDataConfig dataConfig, EventHandler newDataAvailableHandler)
        {
            if (!CanSubscribe(dataConfig.Symbol))
            {
                return Enumerable.Empty<BaseData>().GetEnumerator();
            }

            var enumerator = _aggregator.Add(dataConfig, newDataAvailableHandler);
            _subscriptionManager.Subscribe(dataConfig);

            return enumerator;
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
        /// Subscribes to the requested symbols
        /// </summary>
        /// <param name="symbols">The list of symbols to subscribe</param>
        private bool Subscribe(IEnumerable<Symbol> symbols)
        {
            foreach (var symbol in symbols)
            {
                using (Py.GIL())
                {
                    var brokerageSymbol = _symbolMapper.GetBrokerageSymbol(symbol);

                    _pyBridge.Subscribe(brokerageSymbol);

                    Log.Trace($"Symbol subscribed: {brokerageSymbol}");
                }
            }

            return true;
        }

        /// <summary>
        /// Ends current subscriptions
        /// </summary>
        private bool Unsubscribe(IEnumerable<Symbol> symbols)
        {
            foreach (var symbol in symbols)
            {
                using (Py.GIL())
                {
                    var brokerageSymbol = _symbolMapper.GetBrokerageSymbol(symbol);

                    _pyBridge.Unsubscribe(brokerageSymbol);

                    Log.Trace($"Symbol unsubscribed: {brokerageSymbol}");
                }
            }

            return true;
        }

        #endregion

        #region IBrokerage

        /// <summary>
        /// Returns the brokerage account's base currency
        /// </summary>
        public override string AccountBaseCurrency => null;

        /// <summary>
        /// Connects the client to the broker's remote servers
        /// </summary>
        public override void Connect()
        {
            using (Py.GIL())
            {
                var module = Py.Import("CcxtPythonBridge");
                _pyBridge = module.GetAttr("CcxtPythonBridge").Invoke();
                Log.Trace("CcxtPythonBridge module loaded.");

                var versionInformation = _pyBridge.GetVersionInformation();
                Log.Trace($"Version information: Ccxt v{versionInformation["ccxt"]}, CcxtPro v{versionInformation["ccxtPro"]}");

                var config = new Dictionary<string, object>
                {
                    { "enableRateLimit", true },
                    { "apiKey", _apiKey },
                    { "secret", _secret },
                    { "password", _password }
                };

                _pyBridge.Initialize(
                    _exchangeName,
                    JsonConvert.SerializeObject(config),
                    new Action<string>(OnOrderEvent),
                    new Action<dynamic>(OnTrade),
                    new Action<string, dynamic>(OnQuote));
                Log.Trace("CcxtPythonBridge module initialized.");
            }

            _isConnected = true;
        }

        /// <summary>
        /// Disconnects the client from the broker's remote servers
        /// </summary>
        public override void Disconnect()
        {
            using (Py.GIL())
            {
                _pyBridge.Terminate();
                Log.Trace("CcxtPythonBridge module terminated.");
            }

            _isConnected = false;
        }

        /// <summary>
        /// Gets all open orders on the account
        /// </summary>
        /// <returns>The open orders returned from the brokerage</returns>
        public override List<Orders.Order> GetOpenOrders()
        {
            var list = new List<Orders.Order>();

            using (Py.GIL())
            {
                var pyOrders = _pyBridge.GetOpenOrders();

                foreach (var pyOrder in pyOrders)
                {
                    pyOrder.DelItem("info");

                    var ccxtOrder = ConvertToObject<Order>(pyOrder);

                    list.Add(ConvertOrder(ccxtOrder));
                }
            }

            return list;
        }

        /// <summary>
        /// Gets all holdings for the account
        /// </summary>
        /// <returns>The current holdings from the account</returns>
        public override List<Holding> GetAccountHoldings()
        {
            return new();
        }

        /// <summary>
        /// Gets the current cash balance for each currency held in the brokerage account
        /// </summary>
        /// <returns>The current cash balance for each currency available for trading</returns>
        public override List<CashAmount> GetCashBalance()
        {
            var list = new List<CashAmount>();

            using (Py.GIL())
            {
                var balances = _pyBridge.GetBalances().GetItem("total");

                var dictionary = ConvertToDictionary<string, decimal>(balances);

                foreach (var kvp in dictionary)
                {
                    var balance = kvp.Value;
                    if (balance > 0)
                    {
                        list.Add(new CashAmount(balance, kvp.Key));
                    }
                }
            }

            return list;
        }

        /// <summary>
        /// Places a new order and assigns a new broker ID to the order
        /// </summary>
        /// <param name="order">The order to be placed</param>
        /// <returns>True if the request for a new order has been placed, false otherwise</returns>
        public override bool PlaceOrder(Orders.Order order)
        {
            var brokerageSymbol = _symbolMapper.GetBrokerageSymbol(order.Symbol);
            var orderSide = order.Direction.ToLower();
            var amount = order.AbsoluteQuantity;

            using (Py.GIL())
            {
                switch (order.Type)
                {
                    case OrderType.Market:
                        {
                            var pyOrder = _pyBridge.PlaceMarketOrder(brokerageSymbol, orderSide, amount);

                            pyOrder.DelItem("info");

                            var ccxtOrder = ConvertToObject<Order>(pyOrder);

                            Log.Trace($"Order submitted: {ccxtOrder.Symbol} - Side: {ccxtOrder.Side} - Price: {ccxtOrder.Price} - Amount: {ccxtOrder.Amount} - Id: {ccxtOrder.Id}");

                            order.BrokerId.Add(ccxtOrder.Id.ToString());
                        }
                        break;

                    case OrderType.Limit:
                        {
                            var limitPrice = ((LimitOrder) order).LimitPrice;
                            var pyOrder = _pyBridge.PlaceLimitOrder(brokerageSymbol, orderSide, amount, limitPrice);

                            pyOrder.DelItem("info");

                            var ccxtOrder = ConvertToObject<Order>(pyOrder);

                            Log.Trace($"Order submitted: {ccxtOrder.Symbol} - Side: {ccxtOrder.Side} - Price: {ccxtOrder.Price} - Amount: {ccxtOrder.Amount} - Id: {ccxtOrder.Id}");

                            order.BrokerId.Add(ccxtOrder.Id.ToString());
                        }
                        break;

                    case OrderType.StopMarket:
                        {
                            var stopPrice = ((StopMarketOrder)order).StopPrice;
                            var pyOrder = _pyBridge.PlaceStopMarketOrder(brokerageSymbol, orderSide, amount, stopPrice);

                            pyOrder.DelItem("info");

                            var ccxtOrder = ConvertToObject<Order>(pyOrder);

                            Log.Trace($"Order submitted: {ccxtOrder.Symbol} - Side: {ccxtOrder.Side} - Price: {ccxtOrder.Price} - Amount: {ccxtOrder.Amount} - Id: {ccxtOrder.Id}");

                            order.BrokerId.Add(ccxtOrder.Id.ToString());
                        }
                        break;

                    case OrderType.StopLimit:
                        {
                            var limitPrice = ((StopLimitOrder)order).LimitPrice;
                            var stopPrice = ((StopLimitOrder)order).StopPrice;
                            var pyOrder = _pyBridge.PlaceStopLimitOrder(brokerageSymbol, orderSide, amount, stopPrice, limitPrice);

                            pyOrder.DelItem("info");

                            var ccxtOrder = ConvertToObject<Order>(pyOrder);

                            Log.Trace($"Order submitted: {ccxtOrder.Symbol} - Side: {ccxtOrder.Side} - Price: {ccxtOrder.Price} - Amount: {ccxtOrder.Amount} - Id: {ccxtOrder.Id}");

                            order.BrokerId.Add(ccxtOrder.Id.ToString());
                        }
                        break;

                    default:
                        throw new NotSupportedException($"Order type not supported: {order.Type}");
                }
            }

            return true;
        }

        /// <summary>
        /// Updates the order with the same id
        /// </summary>
        /// <param name="order">The new order information</param>
        /// <returns>True if the request was made for the order to be updated, false otherwise</returns>
        public override bool UpdateOrder(Orders.Order order)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Cancels the order with the specified ID
        /// </summary>
        /// <param name="order">The order to cancel</param>
        /// <returns>True if the request was made for the order to be canceled, false otherwise</returns>
        public override bool CancelOrder(Orders.Order order)
        {
            if (!order.BrokerId.Any())
            {
                Log.Error("CcxtBrokerage.CancelOrder(): Unable to cancel order without BrokerId.");
                return false;
            }

            foreach (var orderId in order.BrokerId)
            {
                var symbol = _symbolMapper.GetBrokerageSymbol(order.Symbol);

                using (Py.GIL())
                {
                    _pyBridge.CancelOrder(orderId, symbol);
                }

                OnOrderEvent(new OrderEvent(order, DateTime.UtcNow, OrderFee.Zero, "CCXT Order Event")
                {
                    Status = OrderStatus.Canceled
                });
            }

            return true;
        }

        #endregion

        private void OnOrderEvent(string message)
        {
            Log.Trace($"OnOrderEvent(): Message: {message}");

            try
            {
                var orders = JsonConvert.DeserializeObject<List<Order>>(message);

                if (orders != null)
                {
                    foreach (var order in orders)
                    {
                        Log.Trace($"OnOrderEvent(): Id: {order.Id} - Symbol: {order.Symbol} - Side: {order.Side} - Status: {order.Status} - Amount: {order.Amount} - Filled: {order.Filled} - Remaining: {order.Remaining}");

                        var qcOrder = _orderProvider.GetOrderByBrokerageId(order.Id);
                        if (qcOrder == null)
                        {
                            Log.Error($"OnOrderEvent(): Broker order id not found: {order.Id}");
                            continue;
                        }

                        var orderStatus = ConvertOrderStatus(order);

                        if (orderStatus != OrderStatus.Filled && orderStatus != OrderStatus.PartiallyFilled)
                        {
                            OnOrderEvent(new OrderEvent(qcOrder, DateTime.UtcNow, OrderFee.Zero, "CCXT Order Event")
                            {
                                Status = orderStatus
                            });
                        }

                        if (order.Trades != null)
                        {
                            var ticket = _orderProvider.GetOrderTicket(qcOrder.Id);
                            if (ticket == null)
                            {
                                Log.Error($"OnOrderEvent(): Order ticket not found - OrderId: {order.Id}");
                                continue;
                            }

                            var orderQuantity = Math.Abs(ticket.Quantity);
                            var totalQuantityFilled = Math.Abs(ticket.QuantityFilled);

                            foreach (var trade in order.Trades)
                            {
                                Log.Trace($"OnOrderEvent(): TradeId: {trade.Id} - Symbol: {trade.Symbol} - Side: {trade.Side} - Price: {trade.Price} - Amount: {trade.Amount}");

                                totalQuantityFilled += trade.Amount;

                                orderStatus = totalQuantityFilled < orderQuantity
                                    ? OrderStatus.PartiallyFilled
                                    : OrderStatus.Filled;

                                var orderFee = OrderFee.Zero;
                                if (trade.Fee?.Cost != null)
                                {
                                    orderFee = new OrderFee(new CashAmount(trade.Fee.Cost.Value, trade.Fee.Currency));
                                }

                                OnOrderEvent(new OrderEvent(qcOrder, DateTime.UtcNow, orderFee, "CCXT Order Fill Event")
                                {
                                    Status = orderStatus
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }

        private void OnTrade(dynamic data)
        {
            try
            {
                using (Py.GIL())
                {
                    foreach (var pyTrade in data)
                    {
                        var symbol = _symbolMapper.GetLeanSymbol(pyTrade["symbol"].ToString());

                        var tick = new Tick
                        {
                            Symbol = symbol,
                            Time = DateTime.UtcNow,
                            TickType = TickType.Trade,
                            Value = pyTrade["price"],
                            Quantity = pyTrade["amount"]
                        };

                        _aggregator.Update(tick);
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }

        private void OnQuote(string ticker, dynamic data)
        {
            try
            {
                Tick tick;
                var symbol = _symbolMapper.GetLeanSymbol(ticker);

                using (Py.GIL())
                {
                    var bestBid = data["bids"][0];
                    var bestAsk = data["asks"][0];

                    tick = new Tick
                    {
                        Symbol = symbol,
                        Time = DateTime.UtcNow,
                        TickType = TickType.Quote,
                        BidPrice = bestBid[0],
                        BidSize = bestBid[1],
                        AskPrice = bestAsk[0],
                        AskSize = bestAsk[1]
                    };
                    tick.SetValue();
                }

                _aggregator.Update(tick);
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }

        /// <summary>
        /// Checks if this brokerage supports the specified symbol
        /// </summary>
        /// <param name="symbol">The symbol</param>
        /// <returns>returns true if brokerage supports the specified symbol; otherwise false</returns>
        private static bool CanSubscribe(Symbol symbol)
        {
            return !symbol.Value.Contains("UNIVERSE") &&
                symbol.SecurityType == SecurityType.Crypto;
        }

        private Orders.Order ConvertOrder(Order ccxtOrder)
        {
            Orders.Order order;
            switch (ccxtOrder.Type)
            {
                case "market":
                    order = new MarketOrder();
                    break;

                case "limit":
                    order = new LimitOrder { LimitPrice = ccxtOrder.Price.GetValueOrDefault() };
                    break;

                default:
                    throw new NotSupportedException($"Unsupported order type: {ccxtOrder.Type}");
            }

            order.Quantity = ccxtOrder.Amount * (ccxtOrder.Side == "sell" ? -1 : 1);
            order.BrokerId = new List<string> { ccxtOrder.Id };
            order.Symbol = _symbolMapper.GetLeanSymbol(ccxtOrder.Symbol);
            order.Time = ccxtOrder.Datetime ?? DateTime.UtcNow;
            order.Status = ConvertOrderStatus(ccxtOrder);
            order.Properties.TimeInForce = ConvertTimeInForce(ccxtOrder.TimeInForce);

            return order;
        }

        private static OrderStatus ConvertOrderStatus(Order ccxtOrder)
        {
            switch (ccxtOrder.Status)
            {
                case "open":
                    return ccxtOrder.Filled > 0 ? OrderStatus.PartiallyFilled : OrderStatus.Submitted;

                case "closed":
                    return OrderStatus.Filled;

                case "canceled":
                case "expired":
                    return OrderStatus.Canceled;

                default:
                    throw new NotSupportedException($"Unsupported order status: {ccxtOrder.Status}");
            }
        }

        private static TimeInForce ConvertTimeInForce(string ccxtTimeInForce)
        {
            switch (ccxtTimeInForce)
            {
                case "GTC":
                case null:
                    return TimeInForce.GoodTilCanceled;

                default:
                    throw new NotSupportedException($"Unsupported time in force: {ccxtTimeInForce}");
            }
        }

        private static Dictionary<T1, T2> ConvertToDictionary<T1, T2>(dynamic pyObject)
        {
            var json = ToJson(pyObject);

            return (Dictionary<T1, T2>)JsonConvert.DeserializeObject<Dictionary<T1, T2>>(json);
        }

        private static T ConvertToObject<T>(dynamic pyObject)
        {
            var json = ToJson(pyObject);

            return (T)JsonConvert.DeserializeObject<T>(json);
        }

        private static string ToJson(dynamic pyObject)
        {
            return pyObject.Repr()
                .Replace(": None", ": null")
                .Replace(": True", ": true")
                .Replace(": False", ": false")
                .Replace("'", "\"");
        }
    }
}

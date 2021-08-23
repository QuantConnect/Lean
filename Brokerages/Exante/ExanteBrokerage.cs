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

using QuantConnect.Orders;
using QuantConnect.Securities;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using CryptoExchange.Net.Objects;
using Exante.Net;
using Exante.Net.Enums;
using Exante.Net.Objects;
using Newtonsoft.Json.Linq;
using QuantConnect.Data;
using QuantConnect.Orders.Fees;
using QuantConnect.Orders.TimeInForces;
using Log = QuantConnect.Logging.Log;


namespace QuantConnect.Brokerages.Exante
{
    /// <summary>
    /// The Exante brokerage
    /// </summary>
    public partial class ExanteBrokerage : Brokerage
    {
        private bool _isConnected;
        private readonly ExanteClientWrapper _client;
        private readonly string _accountId;
        private readonly IDataAggregator _aggregator;
        private readonly ExanteSymbolMapper _symbolMapper;
        private readonly ConcurrentDictionary<Guid, Order> _orderMap = new();
        private readonly EventBasedDataQueueHandlerSubscriptionManager _subscriptionManager;
        private readonly BrokerageConcurrentMessageHandler<ExanteOrder> _messageHandler;

        /// <summary>
        /// Returns the brokerage account's base currency
        /// </summary>
        public override string AccountBaseCurrency => Currencies.USD;

        /// <summary>
        /// Provides the mapping between Lean symbols and Exante symbols.
        /// </summary>
        public ExanteSymbolMapper SymbolMapper => _symbolMapper;

        /// <summary>
        /// Instance of the wrapper class for a Exante REST API client
        /// </summary>
        public ExanteClientWrapper Client => _client;

        private static readonly HashSet<string> SupportedCryptoCurrencies = new HashSet<string>()
        {
            "ETC", "MKR", "BNB", "NEO", "IOTA", "QTUM", "XMR", "EOS", "ETH", "XRP", "DCR",
            "XLM", "ZRX", "BTC", "XAI", "ZEC", "BAT", "BCH", "VEO", "DEFIX", "OMG", "LTC", "DASH"
        };

        /// <summary>
        /// Creates a new ExanteBrokerage
        /// </summary>
        /// <param name="client">Exante client options to create REST API client instance</param>
        /// <param name="accountId">Exante account id</param>
        /// <param name="aggregator">consolidate ticks</param>
        public ExanteBrokerage(
            ExanteClientOptions client,
            string accountId,
            IDataAggregator aggregator
            )
            : base("Exante Brokerage")
        {
            _client = new ExanteClientWrapper(client);
            _symbolMapper = new ExanteSymbolMapper(ComposeTickerToExchangeDictionary());
            _accountId = accountId;
            _aggregator = aggregator;
            _messageHandler = new BrokerageConcurrentMessageHandler<ExanteOrder>(OnUserMessage);
            _subscriptionManager = new EventBasedDataQueueHandlerSubscriptionManager();
            _subscriptionManager.SubscribeImpl += Subscribe;
            _subscriptionManager.UnsubscribeImpl += Unsubscribe;

            _client.StreamClient.GetOrdersStreamAsync(exanteOrder =>
            {
                _messageHandler.HandleNewMessage(exanteOrder);
            });
        }

        /// <summary>
        /// Returns true if we're currently connected to the broker
        /// </summary>
        public override bool IsConnected => _isConnected;

        private void OnUserMessage(ExanteOrder exanteOrder)
        {
            Order order;
            if (_orderMap.TryGetValue(exanteOrder.OrderId, out order))
            {
                OnOrderEvent(new OrderEvent(order, DateTime.UtcNow, OrderFee.Zero) // TODO: What's the fee?
                {
                    Status = ConvertOrderStatus(exanteOrder.OrderState.Status),
                });
            }
        }

        private Dictionary<string, string> ComposeTickerToExchangeDictionary()
        {
            var tickerToExchange = new Dictionary<string, string>();

            void AddMarketSymbols(string market, Func<string, List<string>> tickersByMarket)
            {
                market = market.LazyToUpper();
                var symbols = tickersByMarket(market);
                foreach (var sym in symbols)
                {
                    if (tickerToExchange.ContainsKey(sym))
                    {
                        if (market != tickerToExchange[sym])
                        {
                            Log.Error($"Symbol {sym} occurs on two exchanges: {tickerToExchange[sym]} {market}");
                        }
                    }
                    else
                    {
                        tickerToExchange.Add(sym, market);
                    }
                }
            }

            foreach (var market in new[]
            {
                "NASDAQ", "ARCA", "AMEX",
                "USD", "USCORP", "EUR", "GBP", "ASN", "CAD", "AUD", "ARG", "CAD",
                Market.CBOE, Market.CME, "OTCMKTS", Market.NYMEX, Market.CBOT, Market.COMEX, Market.ICE,
            })
            {
                AddMarketSymbols(market,
                    m => _client.GetSymbolsByExchange(m).Data.Select(x => x.Ticker).ToList());
            }

            AddMarketSymbols("USD", m => SupportedCryptoCurrencies.ToList());

            return tickerToExchange;
        }

        /// <summary>
        /// Gets all open orders on the account.
        /// NOTE: The order objects returned do not have QC order IDs.
        /// </summary>
        /// <returns>The open orders returned from IB</returns>
        public override List<Order> GetOpenOrders()
        {
            var orders = _client.GetActiveOrders().Data;
            var list = new List<Order>();
            foreach (var item in orders)
            {
                Order order;
                switch (item.OrderParameters.Type)
                {
                    case ExanteOrderType.Market:
                        order = new MarketOrder();
                        break;
                    case ExanteOrderType.Limit:
                        if (item.OrderParameters.LimitPrice == null)
                        {
                            throw new ArgumentNullException(nameof(item.OrderParameters.LimitPrice));
                        }

                        order = new LimitOrder { LimitPrice = item.OrderParameters.LimitPrice.Value };
                        break;
                    case ExanteOrderType.Stop:
                        if (item.OrderParameters.StopPrice == null)
                        {
                            throw new ArgumentNullException(nameof(item.OrderParameters.StopPrice));
                        }

                        order = new StopMarketOrder { StopPrice = item.OrderParameters.StopPrice.Value };
                        break;
                    case ExanteOrderType.StopLimit:
                        if (item.OrderParameters.LimitPrice == null)
                        {
                            throw new ArgumentNullException(nameof(item.OrderParameters.LimitPrice));
                        }

                        if (item.OrderParameters.StopPrice == null)
                        {
                            throw new ArgumentNullException(nameof(item.OrderParameters.StopPrice));
                        }

                        order = new StopLimitOrder
                        {
                            StopPrice = item.OrderParameters.StopPrice.Value,
                            LimitPrice = item.OrderParameters.LimitPrice.Value
                        };
                        break;

                    default:
                        OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Error, -1,
                            $"ExanteBrokerage.GetOpenOrders: Unsupported order type returned from brokerage: {item.OrderParameters.Type}"));
                        continue;
                }

                var symbol = _client.GetSymbol(item.OrderParameters.SymbolId).Data;

                switch (item.OrderParameters.Side)
                {
                    case ExanteOrderSide.Buy:
                        order.Quantity = Math.Abs(item.OrderParameters.Quantity);
                        break;
                    case ExanteOrderSide.Sell:
                        order.Quantity = -Math.Abs(item.OrderParameters.Quantity);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                order.BrokerId = new List<string> { item.OrderId.ToString() };
                order.Symbol = ConvertSymbol(symbol);
                order.Time = item.Date;
                order.Status = ConvertOrderStatus(item.OrderState.Status);
                // order.Price = ; // TODO: what's the price?
                list.Add(order);
            }

            return list;
        }

        /// <summary>
        /// Gets all holdings for the account
        /// </summary>
        /// <returns>The current holdings from the account</returns>
        public override List<Holding> GetAccountHoldings()
        {
            var accountSummary = _client.GetAccountSummary(_accountId, AccountBaseCurrency);
            var positions = accountSummary.Positions
                .Where(position => position.Quantity != 0)
                .Select(ConvertHolding)
                .ToList();
            return positions;
        }

        /// <summary>
        /// Gets the current cash balance for each currency held in the brokerage account
        /// </summary>
        /// <returns>The current cash balance for each currency available for trading</returns>
        public override List<CashAmount> GetCashBalance()
        {
            var accountSummary = _client.GetAccountSummary(_accountId, AccountBaseCurrency);
            var cashAmounts =
                from currencyData in accountSummary.Currencies
                select new CashAmount(currencyData.Value, currencyData.Currency);
            return cashAmounts.ToList();
        }

        /// <summary>
        /// Places a new order and assigns a new broker ID to the order
        /// </summary>
        /// <param name="order">The order to be placed</param>
        /// <returns>True if the request for a new order has been placed, false otherwise</returns>
        public override bool PlaceOrder(Order order)
        {
            if (order == null)
            {
                throw new ArgumentNullException(nameof(order));
            }

            var orderSide = ConvertOrderDirection(order.Direction);

            DateTime? goodTilDateTimeInForceExpiration = null;
            ExanteOrderDuration orderDuration;
            switch (order.TimeInForce)
            {
                case GoodTilCanceledTimeInForce _:
                    orderDuration = ExanteOrderDuration.GoodTillCancel;
                    break;
                case DayTimeInForce _:
                    orderDuration = ExanteOrderDuration.Day;
                    break;
                case GoodTilDateTimeInForce goodTilDateTimeInForce:
                    orderDuration = ExanteOrderDuration.GoodTillTime;
                    goodTilDateTimeInForceExpiration = goodTilDateTimeInForce.Expiry;
                    break;
                default:
                    throw new NotSupportedException(
                        $"ExanteBrokerage.ConvertOrderDuration: Unsupported order duration: {order.TimeInForce}");
            }

            var quantity = Math.Abs(order.Quantity);

            var orderPlacementSuccess = false;

            _messageHandler.WithLockedStream(() =>
            {
                WebCallResult<IEnumerable<ExanteOrder>> orderPlacement;
                switch (order.Type)
                {
                    case OrderType.Market:
                        orderPlacement = _client.PlaceOrder(
                            _accountId,
                            _symbolMapper.GetBrokerageSymbol(order.Symbol),
                            ExanteOrderType.Market,
                            orderSide,
                            quantity,
                            orderDuration,
                            gttExpiration: goodTilDateTimeInForceExpiration
                        );
                        break;

                    case OrderType.Limit:
                        var limitOrder = (LimitOrder)order;
                        orderPlacement = _client.PlaceOrder(
                            _accountId,
                            _symbolMapper.GetBrokerageSymbol(order.Symbol),
                            ExanteOrderType.Limit,
                            orderSide,
                            quantity,
                            orderDuration,
                            limitPrice: limitOrder.LimitPrice,
                            gttExpiration: goodTilDateTimeInForceExpiration
                        );
                        break;

                    case OrderType.StopMarket:
                        var stopMarketOrder = (StopMarketOrder)order;
                        orderPlacement = _client.PlaceOrder(
                            _accountId,
                            _symbolMapper.GetBrokerageSymbol(order.Symbol),
                            ExanteOrderType.Stop,
                            orderSide,
                            quantity,
                            orderDuration,
                            stopPrice: stopMarketOrder.StopPrice,
                            gttExpiration: goodTilDateTimeInForceExpiration
                        );
                        break;

                    case OrderType.StopLimit:
                        var stopLimitOrder = (StopLimitOrder)order;
                        orderPlacement = _client.PlaceOrder(
                            _accountId,
                            _symbolMapper.GetBrokerageSymbol(order.Symbol),
                            ExanteOrderType.Stop,
                            orderSide,
                            quantity,
                            orderDuration,
                            limitPrice: stopLimitOrder.LimitPrice,
                            stopPrice: stopLimitOrder.StopPrice,
                            gttExpiration: goodTilDateTimeInForceExpiration
                        );
                        break;

                    default:
                        throw new NotSupportedException(
                            $"ExanteBrokerage.ConvertOrderType: Unsupported order type: {order.Type}");
                }

                foreach (var o in orderPlacement.Data)
                {
                    _orderMap[o.OrderId] = order;
                }

                if (!orderPlacement.Success)
                {
                    var errorsJson =
                        JArray.Parse(orderPlacement.Error?.Message ?? throw new InvalidOperationException());
                    var errorMsg = string.Join(",", errorsJson.Select(x => x["message"]));
                    OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Error, -1, errorMsg));
                }

                orderPlacementSuccess = orderPlacement.Success;
            });

            return orderPlacementSuccess;
        }

        /// <summary>
        /// Updates the order with the same id
        /// </summary>
        /// <param name="order">The new order information</param>
        /// <returns>True if the request was made for the order to be updated, false otherwise</returns>
        public override bool UpdateOrder(Order order)
        {
            if (order == null)
            {
                throw new ArgumentNullException(nameof(order));
            }

            var updateResult = true;
            foreach (var bi in order.BrokerId.Skip(1))
            {
                var d = _client.ModifyOrder(Guid.Parse(bi), ExanteOrderAction.Cancel);
                updateResult = updateResult && d.Success;
            }

            _messageHandler.WithLockedStream(() =>
            {
                WebCallResult<ExanteOrder> exanteOrder;
                switch (order.Type)
                {
                    case OrderType.Market:
                        exanteOrder = _client.ModifyOrder(
                            Guid.Parse(order.BrokerId.First()),
                            ExanteOrderAction.Replace,
                            order.Quantity);
                        break;

                    case OrderType.Limit:
                        var limitOrder = (LimitOrder)order;
                        exanteOrder = _client.ModifyOrder(
                            Guid.Parse(order.BrokerId.First()),
                            ExanteOrderAction.Replace,
                            order.Quantity,
                            limitPrice: limitOrder.LimitPrice);
                        break;

                    case OrderType.StopMarket:
                        var stopMarketOrder = (StopMarketOrder)order;
                        exanteOrder = _client.ModifyOrder(
                            Guid.Parse(order.BrokerId.First()),
                            ExanteOrderAction.Replace,
                            order.Quantity,
                            stopPrice: stopMarketOrder.StopPrice);
                        break;

                    case OrderType.StopLimit:
                        var stopLimitOrder = (StopLimitOrder)order;
                        exanteOrder = _client.ModifyOrder(
                            Guid.Parse(order.BrokerId.First()),
                            ExanteOrderAction.Replace,
                            order.Quantity,
                            limitPrice: stopLimitOrder.LimitPrice,
                            stopPrice: stopLimitOrder.StopPrice);
                        break;

                    default:
                        throw new NotSupportedException(
                            $"ExanteBrokerage.UpdateOrder: Unsupported order type: {order.Type}");
                }

                _orderMap[exanteOrder.Data.OrderId] = order;

                updateResult = updateResult && exanteOrder.Success;
            });
            return updateResult;
        }

        /// <summary>
        /// Cancels the order with the specified ID
        /// </summary>
        /// <param name="order">The order to cancel</param>
        /// <returns>True if the request was made for the order to be canceled, false otherwise</returns>
        public override bool CancelOrder(Order order)
        {
            if (order == null)
            {
                throw new ArgumentNullException(nameof(order));
            }

            var cancelResult = true;
            _messageHandler.WithLockedStream(() =>
            {
                foreach (var bi in order.BrokerId)
                {
                    var biGuid = Guid.Parse(bi);
                    var exanteOrder = _client.ModifyOrder(biGuid, ExanteOrderAction.Cancel);
                    _orderMap.TryRemove(biGuid, out _);
                    cancelResult = cancelResult && exanteOrder.Success;
                }
            });

            return cancelResult;
        }

        /// <summary>
        /// Connects the client to the broker's remote servers
        /// </summary>
        public override void Connect()
        {
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
        /// Dispose of the brokerage instance
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _client?.Dispose();
                _aggregator?.Dispose();
            }
        }

        public sealed override void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}

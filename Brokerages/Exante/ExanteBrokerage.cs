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
using QuantConnect.Data;
using QuantConnect.Orders.Fees;
using QuantConnect.Orders.TimeInForces;

namespace QuantConnect.Brokerages.Exante
{
    public partial class ExanteBrokerage : Brokerage
    {
        private bool _isConnected;
        private readonly ExanteClientWrapper _client;
        private readonly string _accountId;
        private readonly IDataAggregator Aggregator;
        private readonly ExanteSymbolMapper _symbolMapper = new ExanteSymbolMapper();
        private readonly ConcurrentDictionary<Guid, Order> _orderMap = new ConcurrentDictionary<Guid, Order>();
        private readonly EventBasedDataQueueHandlerSubscriptionManager _subscriptionManager;
        public override string AccountBaseCurrency => Currencies.USD;

        private static readonly HashSet<string> SupportedCryptoCurrencies = new HashSet<string>()
        {
            "ETC", "MKR", "BNB", "NEO", "IOTA", "QTUM", "XMR", "EOS", "ETH", "XRP", "DCR",
            "XLM", "ZRX", "BTC", "XAI", "ZEC", "BAT", "BCH", "VEO", "DEFIX", "OMG", "LTC", "DASH"
        };

        public ExanteBrokerage(
            ExanteClientOptions client,
            string accountId,
            IDataAggregator aggregator
            )
            : base("Exante Brokerage")
        {
            _client = new ExanteClientWrapper(client);
            _accountId = accountId;
            Aggregator = aggregator;
            _subscriptionManager = new EventBasedDataQueueHandlerSubscriptionManager();
            _subscriptionManager.SubscribeImpl += Subscribe;
            _subscriptionManager.UnsubscribeImpl += Unsubscribe;

            _client.StreamClient.GetOrdersStreamAsync(exanteOrder =>
            {
                Order order;
                if (_orderMap.TryGetValue(exanteOrder.OrderId, out order))
                {
                    OnOrderEvent(new OrderEvent(order, DateTime.UtcNow, OrderFee.Zero) // TODO: What's the fee?
                    {
                        Status = ConvertOrderStatus(exanteOrder.OrderState.Status),
                    });
                }
            });
        }

        public override bool IsConnected => _isConnected;

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

                        order = new LimitOrder {LimitPrice = item.OrderParameters.LimitPrice.Value};
                        break;
                    case ExanteOrderType.Stop:
                        if (item.OrderParameters.StopPrice == null)
                        {
                            throw new ArgumentNullException(nameof(item.OrderParameters.StopPrice));
                        }

                        order = new StopMarketOrder {StopPrice = item.OrderParameters.StopPrice.Value};
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

                order.Quantity = item.OrderParameters.Quantity;
                order.BrokerId = new List<string> {item.OrderId.ToString()};
                order.Symbol = ConvertSymbol(symbol);
                order.Time = item.Date;
                order.Status = ConvertOrderStatus(item.OrderState.Status);
                // order.Price = ; // TODO: what's the price?
                list.Add(order);
            }

            return list;
        }

        public override List<Holding> GetAccountHoldings()
        {
            var accountSummary = _client.GetAccountSummary(_accountId, AccountBaseCurrency);
            var positions = accountSummary.Positions
                .Where(position => position.Quantity != 0)
                .Select(ConvertHolding)
                .ToList();
            return positions;
        }

        public override List<CashAmount> GetCashBalance()
        {
            var accountSummary = _client.GetAccountSummary(_accountId, AccountBaseCurrency);
            var cashAmounts =
                from currencyData in accountSummary.Currencies
                select new CashAmount(currencyData.Value, currencyData.Currency);
            return cashAmounts.ToList();
        }

        public override bool PlaceOrder(Order order)
        {
            var orderSide = ConvertOrderDirection(order.Direction);

            DateTime? gttExpiration = null;
            ExanteOrderDuration orderDuration;
            switch (order.TimeInForce)
            {
                case GoodTilCanceledTimeInForce _:
                    orderDuration = ExanteOrderDuration.GoodTillCancel;
                    break;
                case DayTimeInForce _:
                    orderDuration = ExanteOrderDuration.Day;
                    break;
                case GoodTilDateTimeInForce gtdtif:
                    orderDuration = ExanteOrderDuration.GoodTillTime;
                    gttExpiration = gtdtif.Expiry;
                    break;
                default:
                    throw new NotSupportedException(
                        $"ExanteBrokerage.ConvertOrderDuration: Unsupported order duration: {order.TimeInForce}");
            }

            WebCallResult<IEnumerable<ExanteOrder>> orderPlacement;
            switch (order.Type)
            {
                case OrderType.Market:
                    orderPlacement = _client.PlaceOrder(
                        _accountId,
                        _symbolMapper.GetBrokerageSymbol(order.Symbol),
                        ExanteOrderType.Market,
                        orderSide,
                        order.Quantity,
                        orderDuration,
                        gttExpiration: gttExpiration
                    );
                    break;

                case OrderType.Limit:
                    var limitOrder = (LimitOrder) order;
                    orderPlacement = _client.PlaceOrder(
                        _accountId,
                        _symbolMapper.GetBrokerageSymbol(order.Symbol),
                        ExanteOrderType.Limit,
                        orderSide,
                        order.Quantity,
                        orderDuration,
                        limitPrice: limitOrder.LimitPrice,
                        gttExpiration: gttExpiration
                    );
                    break;

                case OrderType.StopMarket:
                    var stopMarketOrder = (StopMarketOrder) order;
                    orderPlacement = _client.PlaceOrder(
                        _accountId,
                        _symbolMapper.GetBrokerageSymbol(order.Symbol),
                        ExanteOrderType.Stop,
                        orderSide,
                        order.Quantity,
                        orderDuration,
                        stopPrice: stopMarketOrder.StopPrice,
                        gttExpiration: gttExpiration
                    );
                    break;

                case OrderType.StopLimit:
                    var stopLimitOrder = (StopLimitOrder) order;
                    orderPlacement = _client.PlaceOrder(
                        _accountId,
                        _symbolMapper.GetBrokerageSymbol(order.Symbol),
                        ExanteOrderType.Stop,
                        orderSide,
                        order.Quantity,
                        orderDuration,
                        limitPrice: stopLimitOrder.LimitPrice,
                        stopPrice: stopLimitOrder.StopPrice,
                        gttExpiration: gttExpiration
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

            return orderPlacement.Success;
        }

        public override bool UpdateOrder(Order order)
        {
            var updateResult = true;
            foreach (var bi in order.BrokerId.Skip(1))
            {
                var d = _client.ModifyOrder(Guid.Parse(bi), ExanteOrderAction.Cancel);
                updateResult = updateResult && d.Success;
            }

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
                    var limitOrder = (LimitOrder) order;
                    exanteOrder = _client.ModifyOrder(
                        Guid.Parse(order.BrokerId.First()),
                        ExanteOrderAction.Replace,
                        order.Quantity,
                        limitPrice: limitOrder.LimitPrice);
                    break;

                case OrderType.StopMarket:
                    var stopMarketOrder = (StopMarketOrder) order;
                    exanteOrder = _client.ModifyOrder(
                        Guid.Parse(order.BrokerId.First()),
                        ExanteOrderAction.Replace,
                        order.Quantity,
                        stopPrice: stopMarketOrder.StopPrice);
                    break;

                case OrderType.StopLimit:
                    var stopLimitOrder = (StopLimitOrder) order;
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
            return updateResult;
        }

        public override bool CancelOrder(Order order)
        {
            var cancelResult = true;
            foreach (var bi in order.BrokerId)
            {
                var biGuid = Guid.Parse(bi);
                var exanteOrder = _client.ModifyOrder(biGuid, ExanteOrderAction.Cancel);
                _orderMap.TryRemove(biGuid, out _);
                cancelResult = cancelResult && exanteOrder.Success;
            }

            return cancelResult;
        }

        public override void Connect()
        {
            _isConnected = true;
        }

        public override void Disconnect()
        {
            _isConnected = false;
        }
    }
}

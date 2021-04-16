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

using Newtonsoft.Json;
using QuantConnect.Orders;
using QuantConnect.Securities;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CryptoExchange.Net.Objects;
using Exante.Net;
using Exante.Net.Enums;
using Exante.Net.Objects;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;
using QuantConnect.Logging;
using QuantConnect.Orders.Fees;
using QuantConnect.Orders.TimeInForces;
using QuantConnect.Packets;
using QuantConnect.Util;

namespace QuantConnect.Brokerages.Exante
{
    public partial class ExanteBrokerage : Brokerage, IDataQueueHandler
    {
        private bool _isConnected;
        private readonly ExanteClientWrapper _client;
        private string _accountId;
        private readonly ExanteSymbolMapper _symbolMapper = new ExanteSymbolMapper();
        private const string ReportCurrency = "USD";

        public ExanteBrokerage(
            ExanteClient client,
            string accountId
            )
            : base("Exante Brokerage")
        {
            _client = new ExanteClientWrapper(client);
            _accountId = accountId;
        }

        public IEnumerator<BaseData> Subscribe(SubscriptionDataConfig dataConfig, EventHandler newDataAvailableHandler)
        {
            throw new NotImplementedException();
        }

        public void Unsubscribe(SubscriptionDataConfig dataConfig)
        {
            throw new NotImplementedException();
        }

        public void SetJob(LiveNodePacket job)
        {
            throw new NotImplementedException();
        }

        public override bool IsConnected => _isConnected;

        public override List<Order> GetOpenOrders()
        {
            var orders = _client.GetActiveOrders();
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

                var symbol = _client.GetSymbol(item.OrderParameters.SymbolId);

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
            var accountSummary = _client.GetAccountSummary(_accountId, ReportCurrency);
            var positions = accountSummary.Positions
                .Where(position => position.Quantity != 0)
                .Select(ConvertHolding)
                .ToList();
            return positions;
        }

        public override List<CashAmount> GetCashBalance()
        {
            var accountSummary = _client.GetAccountSummary(_accountId, ReportCurrency);
            var cashAmounts =
                from currencyData in accountSummary.Currencies
                select new CashAmount(currencyData.Value, currencyData.Currency);
            return cashAmounts.ToList();
        }

        public override bool PlaceOrder(Order order)
        {
            var orderSide = default(ExanteOrderSide);
            switch (order.Direction)
            {
                case OrderDirection.Buy:
                    orderSide = ExanteOrderSide.Buy;
                    break;
                case OrderDirection.Sell:
                    orderSide = ExanteOrderSide.Sell;
                    break;
                case OrderDirection.Hold:
                    throw new NotSupportedException(
                        $"ExanteBrokerage.ConvertOrderDirection: Unsupported order direction: {order.Direction}");
            }

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

            IEnumerable<ExanteOrder> orderPlacement;
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

                default:
                    throw new NotSupportedException(
                        $"ExanteBrokerage.ConvertOrderType: Unsupported order type: {order.Type}");
            }

            var isPlaced = orderPlacement.Any(item => item.OrderState.Status == ExanteOrderStatus.Cancelled);
            return isPlaced;
        }

        public override bool UpdateOrder(Order order)
        {
            throw new NotImplementedException();
        }

        public override bool CancelOrder(Order order)
        {
            throw new NotImplementedException();
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

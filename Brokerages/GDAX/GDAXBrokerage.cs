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
using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Logging;
using QuantConnect.Orders;
using QuantConnect.Securities;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using WebSocket4Net;

namespace QuantConnect.Brokerages.GDAX
{
    public partial class GDAXBrokerage : BaseWebsocketsBrokerage, IDataQueueHandler
    {

        #region IBrokerage
        /// <summary>
        /// Checks if the websocket connection is connected or in the process of connecting
        /// </summary>
        public override bool IsConnected
        {
            get { return WebSocket.IsOpen; }
        }

        /// <summary>
        /// Creates a new order
        /// </summary>
        /// <param name="order"></param>
        /// <returns></returns>
        public override bool PlaceOrder(Orders.Order order)
        {
            var req = new RestRequest("/orders", Method.POST);

            dynamic payload = new ExpandoObject();

            payload.size = Math.Abs(order.Quantity);
            payload.side = order.Direction.ToString().ToLower();
            payload.type = ConvertOrderType(order.Type);
            payload.price = order is LimitOrder ? ((LimitOrder)order).LimitPrice : order is StopMarketOrder ? ((StopMarketOrder)order).StopPrice : 0;
            payload.product_id = ConvertSymbol(order.Symbol);

            if (_algorithm.BrokerageModel.AccountType == AccountType.Margin)
            {
                payload.overdraft_enabled = true;
            }

            req.AddJsonBody(payload);

            GetAuthenticationToken(req);
            var response = RestClient.Execute(req);

            if (response.StatusCode == System.Net.HttpStatusCode.OK && response.Content != null)
            {
                dynamic raw = JsonConvert.DeserializeObject<dynamic>(response.Content);

                if (raw == null || raw.id == null)
                {
                    OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Error, (int)response.StatusCode, "GDAXBrokerage.PlaceOrder: Error parsing response from place order: " + response.Content));
                    return false;
                }

                string brokerId = raw.id;
                if (CachedOrderIDs.ContainsKey(order.Id))
                {
                    CachedOrderIDs[order.Id].BrokerId.Add(brokerId);
                }
                else
                {
                    order.BrokerId.Add(brokerId);
                    CachedOrderIDs.TryAdd(order.Id, order);
                }

                if (order.Type != OrderType.Market)
                {
                    FillSplit.TryAdd(order.Id, new GDAXFill(order));
                }

                OnOrderEvent(new OrderEvent(order, DateTime.UtcNow, 0, "GDAX Order Event") { Status = OrderStatus.Submitted });

                if (order.Type == OrderType.Market)
                {
                    OnOrderEvent(new OrderEvent(order, DateTime.UtcNow, (decimal)raw.fill_fees, "GDAX Order Event") { Status = OrderStatus.Filled });
                    Orders.Order outOrder = null;
                    CachedOrderIDs.TryRemove(order.Id, out outOrder);
                }

                OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Information, -1, "GDAXBrokerage.PlaceOrder: Order completed successfully orderid:" + order.Id.ToString()));
                return true;

            }

            OnOrderEvent(new OrderEvent(order, DateTime.UtcNow, 0, "GDAX Order Event") { Status = OrderStatus.Invalid });

            string message = $"GDAXBrokerage.PlaceOrder: Order failed Order Id: {order.Id} timestamp: {order.Time} quantity: {order.Quantity.ToString()} content: {response.Content}";
            OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Error, -1, message));
            return false;

        }

        /// <summary>
        /// This operation is not supported
        /// </summary>
        /// <param name="order"></param>
        /// <returns></returns>
        public override bool UpdateOrder(Orders.Order order)
        {
            throw new NotSupportedException("GDAXBrokerage.UpdateOrder: Order update not supported. Please cancel and re-create.");
        }

        /// <summary>
        /// Cancels an order
        /// </summary>
        /// <param name="order"></param>
        /// <returns></returns>
        public override bool CancelOrder(Orders.Order order)
        {
            var success = new List<bool>();

            foreach (var id in order.BrokerId)
            {
                var req = new RestRequest("/orders/" + id, Method.DELETE);
                GetAuthenticationToken(req);
                var response = RestClient.Execute(req);

                success.Add(response.StatusCode == System.Net.HttpStatusCode.OK);
            }

            return success.All(a => a);
        }

        /// <summary>
        /// Closes the websockets connection
        /// </summary>
        public override void Disconnect()
        {
            WebSocket.Close();
        }

        /// <summary>
        /// Gets all orders not yet closed
        /// </summary>
        /// <returns></returns>
        public override List<Orders.Order> GetOpenOrders()
        {
            var list = new List<Order>();

            try
            {
                var req = new RestRequest("/orders?status=open&status=pending", Method.GET);
                GetAuthenticationToken(req);
                var response = RestClient.Execute(req);

                if (response != null)
                {
                    var orders = JsonConvert.DeserializeObject<Messages.Order[]>(response.Content);
                    foreach (var item in orders)
                    {
                        Order order = null;
                        if (item.Type == "market")
                        {
                            order = new MarketOrder { Price = item.Price };
                        }
                        else if (item.Type == "limit")
                        {
                            order = new LimitOrder { LimitPrice = item.Price };
                        }
                        else if (item.Type == "stop")
                        {
                            order = new StopMarketOrder { StopPrice = item.Price };
                        }
                        else
                        {
                            OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Error, (int)response.StatusCode, 
                                "GDAXBrokerage.GetOpenOrders: Unsupported order type returned from brokerage: " + item.Type));
                            continue;
                        }

                        order.Quantity = item.Side == "sell" ? -item.Size : item.Size;
                        order.BrokerId = new List<string> { item.Id.ToString() };
                        order.Symbol = ConvertProductId(item.ProductId);
                        order.Time = DateTime.UtcNow;
                        order.Status = ConvertOrderStatus(item);
                        order.Price = item.Price;
                        list.Add(order);
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }

            foreach (Order item in list)
            {
                if (item.Status.IsOpen())
                {
                    var cached = this.CachedOrderIDs.Where(c => c.Value.BrokerId.Contains(item.BrokerId.First()));
                    if (cached.Any())
                    {
                        this.CachedOrderIDs[cached.First().Key] = item;
                    }
                }
            }

            return list;

        }

        /// <summary>
        /// Gets all open positions
        /// </summary>
        /// <returns></returns>
        public override List<Holding> GetAccountHoldings()
        {
            var list = new List<Holding>();

            var req = new RestRequest("/orders?status=active", Method.GET);
            GetAuthenticationToken(req);
            var response = RestClient.Execute(req);
            if (response != null)
            {
                foreach (var item in JsonConvert.DeserializeObject<Messages.Order[]>(response.Content))
                {

                    decimal conversionRate;
                    if (!item.ProductId.EndsWith("USD", StringComparison.InvariantCultureIgnoreCase))
                    {

                        var baseSymbol = (item.ProductId.Substring(0, 3) + "USD").ToLower();
                        var tick = this.GetTick(Symbol.Create(baseSymbol, SecurityType.Crypto, Market.GDAX));
                        conversionRate = tick.Price;
                    }
                    else
                    {
                        var tick = this.GetTick(ConvertProductId(item.ProductId));
                        conversionRate = tick.Price;
                    }

                    list.Add(new Holding
                    {
                        Symbol = ConvertProductId(item.ProductId),
                        Quantity = item.Side == "sell" ? -item.FilledSize : item.FilledSize,
                        Type = SecurityType.Crypto,
                        CurrencySymbol = item.ProductId.Substring(0, 3).ToUpper(),
                        ConversionRate = conversionRate,
                        MarketPrice = item.Price,
                        //todo: check this
                        AveragePrice = item.FilledSize > 0 ? item.ExecutedValue / item.FilledSize : 0
                    });
                }

            }
            return list;
        }

        /// <summary>
        /// Gets the total account cash balance
        /// </summary>
        /// <returns></returns>
        public override List<Cash> GetCashBalance()
        {
            var list = new List<Securities.Cash>();

            var req = new RestRequest("/accounts", Method.GET);
            GetAuthenticationToken(req);
            var response = RestClient.Execute(req);

            foreach (var item in JsonConvert.DeserializeObject<Messages.Account[]>(response.Content))
            {
                if (item.Balance > 0)
                {
                    if (item.Currency == "USD")
                    {
                        list.Add(new Securities.Cash(item.Currency, item.Balance, 1));
                    }
                    else if (new[] {"GBP", "EUR" }.Contains(item.Currency))
                    {
                        var rate = GetConversionRate(item.Currency);
                        list.Add(new Securities.Cash(item.Currency.ToUpper(), item.Balance, rate));
                    }
                    else
                    {
                        var tick = GetTick(Symbol.Create(item.Currency + "USD", SecurityType.Crypto, Market.GDAX));

                        list.Add(new Securities.Cash(item.Currency.ToUpper(), item.Balance, tick.Price));
                    }
                }
            }
            return list;
        }

        /// <summary>
        /// Get queued tick data
        /// </summary>
        /// <returns></returns>
        public IEnumerable<Data.BaseData> GetNextTicks()
        {
            lock (Ticks)
            {
                var copy = Ticks.ToArray();
                Ticks.Clear();
                return copy;
            }
        }
        #endregion

        /// <summary>
        /// Retreives the fee for a given order
        /// </summary>
        /// <param name="order"></param>
        /// <returns></returns>
        public decimal GetFee(Orders.Order order)
        {
            var totalFee = 0m;

            foreach (var item in order.BrokerId)
            {
                var req = new RestRequest("/orders/" + item, Method.GET);
                GetAuthenticationToken(req);
                var response = RestClient.Execute(req);
                var fill = JsonConvert.DeserializeObject<dynamic>(response.Content);

                totalFee += (decimal)fill.fill_fees;
            }

            return totalFee;
        }

    }
}

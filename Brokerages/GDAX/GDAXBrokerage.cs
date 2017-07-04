using Newtonsoft.Json;
using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Logging;
using QuantConnect.Orders;
using QuantConnect.Securities;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using WebSocketSharp;

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
            get { return WebSocket.ReadyState == WebSocketState.Connecting || WebSocket.ReadyState == WebSocketState.Open; }
        }

        /// <summary>
        /// Creates a new order
        /// </summary>
        /// <param name="order"></param>
        /// <returns></returns>
        public override bool PlaceOrder(Orders.Order order)
        {
            var req = new RestRequest("/orders", Method.POST);
            req.AddJsonBody(new
            {
                size = order.Quantity,
                side = order.Direction.ToString().ToLower(),
                type = ConvertOrderType(order.Type),
                price = order is LimitOrder ? ((LimitOrder)order).LimitPrice : 0,
                product_id = ConvertSymbol(order.Symbol),
                overdraft_enabled = true
            });
            var response = RestClient.Execute(req);

            if (response.StatusCode == System.Net.HttpStatusCode.OK && response.Content != null)
            {
                dynamic raw = JsonConvert.DeserializeObject<dynamic>(response.Content);

                if (raw != null && raw.order_id != 0)
                {
                    if (CachedOrderIDs.ContainsKey(order.Id))
                    {
                        CachedOrderIDs[order.Id].BrokerId.Add(raw.order_id);
                    }
                    else
                    {
                        CachedOrderIDs.TryAdd(order.Id, order);
                    }
                }
                OnOrderEvent(new OrderEvent(order, DateTime.UtcNow, 0, "Bitfinex Order Event") { Status = OrderStatus.Submitted });
                Log.Trace("BitfinexBrokerage.PlaceOrder(): Order completed successfully orderid:" + order.Id.ToString());
                return true;
            }

            OnOrderEvent(new OrderEvent(order, DateTime.UtcNow, 0, "Bitfinex Order Event") { Status = OrderStatus.Invalid });
            Log.Trace("BitfinexBrokerage.PlaceOrder(): Order failed Order Id: " + order.Id + " timestamp:" + order.Time + " quantity: " + order.Quantity.ToString());
            return false;

        }

        /// <summary>
        /// This operation is not supported
        /// </summary>
        /// <param name="order"></param>
        /// <returns></returns>
        public override bool UpdateOrder(Orders.Order order)
        {
            throw new NotSupportedException("Order update not supported. Please cancel and re-create.");
        }

        /// <summary>
        /// Cancels an order
        /// </summary>
        /// <param name="order"></param>
        /// <returns></returns>
        public override bool CancelOrder(Orders.Order order)
        {
            List<bool> success = new List<bool>();

            foreach (var id in order.BrokerId)
            {
                var req = new RestRequest("/orders/" + id, Method.DELETE);
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
                            Log.Error("GDAXBrokerage.GetOpenBitfinexOrders(): Unsupported order type returned from brokerage" + item.Type);
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
            var response = RestClient.Execute(req);
            if (response != null)
            {
                foreach (var item in JsonConvert.DeserializeObject<Messages.Order[]>(response.Content))
                {
                    decimal conversionRate;
                    if (!item.ProductId.EndsWith("USD", StringComparison.InvariantCultureIgnoreCase))
                    {

                        var baseSymbol = (item.ProductId.Substring(0, 3) + "USD").ToLower();
                        var tick = this.GetTick(Symbol.Create(baseSymbol, SecurityType.Forex, Market.GDAX));
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
                        Type = SecurityType.Forex,
                        CurrencySymbol = item.ProductId.Substring(0, 3).ToUpper(),
                        ConversionRate = conversionRate,
                        MarketPrice = item.Price,
                        //todo: check this
                        AveragePrice = item.ExecutedValue / item.FilledSize
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

            var req = new RestRequest("/accounts/" + _accountId, Method.GET);
            var response = RestClient.Execute(req);

            foreach (var item in JsonConvert.DeserializeObject<Messages.Account[]>(response.Content))
            {
                if (item.Balance > 0)
                {
                    if (item.Currency == "USD")
                    {
                        list.Add(new Securities.Cash(item.Currency, item.Balance, 1));
                    }
                    else
                    {
                        var tick = GetTick(Symbol.Create(item.Currency + "USD", SecurityType.Forex, Market.GDAX));

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

    }
}

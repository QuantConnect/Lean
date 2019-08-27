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
using Newtonsoft.Json.Linq;
using QuantConnect.Orders;
using System;
using System.Globalization;

namespace QuantConnect.Brokerages.Binance.Messages
{
#pragma warning disable 1591

    public class AccountInformation
    {
        public Balance[] Balances { get; set; }

        public class Balance
        {
            public string Asset { get; set; }
            public decimal Free { get; set; }
            public decimal Locked { get; set; }
            public decimal Amount => Free + Locked;
        }
    }

    public class PriceTicker
    {
        public string Symbol { get; set; }
        public decimal Price { get; set; }
    }

    public class Order
    {
        [JsonProperty("orderId")]
        public string Id { get; set; }
        public string Symbol { get; set; }
        public decimal Price { get; set; }
        public decimal StopPrice { get; set; }
        [JsonProperty("origQty")]
        public decimal OriginalAmount { get; set; }
        [JsonProperty("executedQty")]
        public decimal ExecutedAmount { get; set; }
        public string Status { get; set; }
        public string Type { get; set; }
        public string Side { get; set; }

        public decimal Quantity => string.Equals(Side, "buy", StringComparison.OrdinalIgnoreCase) ? OriginalAmount : -OriginalAmount;
    }

    public class OpenOrder : Order
    {
        public long Time { get; set; }
    }

    public class NewOrder : Order
    {
        [JsonProperty("transactTime")]
        public long TransactionTime { get; set; }
    }

    public enum EventType
    {
        None,
        OrderBook,
        Trade,
        Execution
    }

    public class BaseMessage
    {
        public virtual EventType @Event { get; } = EventType.None;

        [JsonProperty("e")]
        public string EventName { get; set; }

        [JsonProperty("E")]
        public long Time { get; set; }

        [JsonProperty("s")]
        public string Symbol { get; set; }

        public static BaseMessage Parse(string data)
        {
            var wrapped = JObject.Parse(data);
            var eventType = wrapped["data"]["e"].ToObject<string>();
            switch (eventType)
            {
                case "executionReport":
                    return wrapped.GetValue("data").ToObject<Messages.Execution>();
                case "depthUpdate":
                    return wrapped.GetValue("data").ToObject<Messages.OrderBookUpdateMessage>();
                case "trade":
                    return wrapped.GetValue("data").ToObject<Messages.Trade>();
                default:
                    return null;
            }
        }

        public T ToObject<T>() where T : BaseMessage
        {
            try
            {
                return (T)Convert.ChangeType(this, typeof(T), CultureInfo.InvariantCulture);
            }
            catch
            {
                return default(T);
            }
        }
    }

    public class OrderBookSnapshotMessage
    {
        public long LastUpdateId { get; set; }

        public object[][] Bids { get; set; }

        public object[][] Asks { get; set; }
    }

    public class OrderBookUpdateMessage : BaseMessage
    {
        public override EventType @Event => EventType.OrderBook;

        [JsonProperty("U")]
        public long FirstUpdate { get; set; }

        [JsonProperty("u")]
        public long FinalUpdate { get; set; }

        [JsonProperty("b")]
        public object[][] Bids { get; set; }

        [JsonProperty("a")]
        public object[][] Asks { get; set; }
    }

    public class Trade : BaseMessage
    {
        public override EventType @Event => EventType.Trade;

        [JsonProperty("T")]
        public new long Time { get; set; }

        [JsonProperty("p")]
        public decimal Price { get; private set; }

        [JsonProperty("q")]
        public decimal Quantity { get; private set; }
    }

    public class Execution : BaseMessage
    {
        public override EventType @Event => EventType.Execution;

        [JsonProperty("i")]
        public string OrderId { get; set; }

        [JsonProperty("t")]
        public string TradeId { get; set; }

        [JsonProperty("I")]
        public string Ignore { get; set; }

        [JsonProperty("x")]
        public string ExecutionType { get; private set; }

        [JsonProperty("X")]
        public string OrderStatus { get; private set; }

        [JsonProperty("T")]
        public long TransactionTime { get; set; }

        [JsonProperty("L")]
        public decimal LastExecutedPrice { get; set; }

        [JsonProperty("l")]
        public decimal LastExecutedQuantity { get; set; }

        [JsonProperty("S")]
        public string Side { get; set; }

        [JsonProperty("n")]
        public decimal Fee { get; set; }

        [JsonProperty("N")]
        public string FeeCurrency { get; set; }

        public OrderDirection Direction => Side.Equals("BUY", StringComparison.OrdinalIgnoreCase) ? OrderDirection.Buy : OrderDirection.Sell;
    }

    public class Kline
    {
        public long OpenTime { get; }
        public decimal Open { get; }
        public decimal Close { get; }
        public decimal High { get; }
        public decimal Low { get; }
        public decimal Volume { get; }

        public Kline() { }

        public Kline(long msts, decimal close)
        {
            OpenTime = msts;
            Open = Close = High = Low = close;
            Volume = 0;
        }

        public Kline(object[] entries)
        {
            OpenTime = Convert.ToInt64(entries[0], CultureInfo.InvariantCulture);
            Open = ((string)entries[1]).ToDecimal();
            Close = ((string)entries[4]).ToDecimal();
            High = ((string)entries[2]).ToDecimal();
            Low = ((string)entries[3]).ToDecimal();
            Volume = ((string)entries[5]).ToDecimal();
        }
    }

#pragma warning restore 1591
}

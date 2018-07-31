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
using System;
using System.Collections.Generic;

namespace QuantConnect.Brokerages.Bitfinex.Messages
{

    //several simple objects to facilitate json conversion
#pragma warning disable 1591

    public class BaseMessage
    {
        [JsonProperty("event")]
        public string Event { get; set; }
    }

    public class Order
    {
        public string Id { get; set; }
        public decimal Price { get; set; }
        public string Symbol { get; set; }
        public string Type { get; set; }
        public string Side { get; set; }
        public double Timestamp { get; set; }
        [JsonProperty("remaining_amount")]
        public decimal Quantity { get; set; }

        public bool IsExchange => Type.StartsWith("exchange", StringComparison.OrdinalIgnoreCase);
    }

    public class Position
    {
        public int Id { get; set; }
        public string Symbol { get; set; }
        [JsonProperty("base")]
        public decimal AveragePrice { get; set; }
        public decimal Amount { get; set; }
        public double Timestamp { get; set; }
        public decimal Swap { get; set; }
        public decimal PL { get; set; }
    }

    public class Wallet
    {
        public string Type { get; set; }
        public string Currency { get; set; }
        public decimal Amount { get; set; }
        public decimal Available { get; set; }
    }

    public class Tick
    {
        public decimal Mid { get; set; }
        public decimal Bid { get; set; }
        public decimal Ask { get; set; }
        [JsonProperty("last_price")]
        public decimal LastPrice { get; set; }
        public decimal Low { get; set; }
        public decimal High { get; set; }
        public decimal Volume { get; set; }
        public double Timestamp { get; set; }
    }

#pragma warning restore 1591

}

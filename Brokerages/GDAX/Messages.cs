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
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect.Brokerages.GDAX.Messages
{

#pragma warning disable 1591

    public class BaseMessage
    {
        public string Type { get; set; }
        public long Sequence { get; set; }
        public DateTime Time { get; set; }
        [JsonProperty("product_id")]
        public string ProductId { get; set; }
    }

    public class Done : BaseMessage
    {
        public decimal Price { get; set; }
        [JsonProperty("order_id")]
        public string OrderId { get; set; }
        public string Reason { get; set; }
        public string Side { get; set; }
        public decimal RemainingSize { get; set; }
    }

    public class Matched : BaseMessage
    {
        [JsonProperty("trade_id")]
        public int TradeId { get; set; }
        [JsonProperty("maker_order_id")]
        public string MakerOrderId { get; set; }
        [JsonProperty("taker_order_id")]
        public string TakerOrderId { get; set; }
        public decimal Size { get; set; }
        public decimal Price { get; set; }
        public string Side { get; set; }
        [JsonProperty("taker_user_id")]
        public string TakerUserId { get; set; }
        [JsonProperty("user_id")]
        public string UserId { get; set; }
        [JsonProperty("taker_profile_id")]
        public string TakerProfileId { get; set; }
        [JsonProperty("profile_id")]
        public string ProfileId { get; set; }
    }

    public class Heartbeat : BaseMessage
    {
        [JsonProperty("last_trade_id")]
        public int LastTradeId { get; set; }
    }

    public class Error : BaseMessage
    {
        public string Message { get; set; }
    }

    public class Subscribe
    {
        public string Type { get; set; }
        [JsonProperty("product_ids")]
        public IList<string> ProductIds { get; set; }
        public string Signature { get; set; }
        public string Key { get; set; }
        public string Passphrase { get; set; }
        public string Timestamp { get; set; }
    }

    public class Open : BaseMessage
    {
        [JsonProperty("order_id")]
        public string OrderId { get; set; }
        public decimal Price { get; set; }
        [JsonProperty("remaining_size")]
        public decimal RemainingSize { get; set; }
        public string Side { get; set; }
    }

    public class Change : Open
    {
        [JsonProperty("new_funds")]
        public decimal NewFunds { get; set; }
        [JsonProperty("old_funds")]
        public decimal OldFunds { get; set; }
    }

    public class Order
    {
        public string Id { get; set; }
        public decimal Price { get; set; }
        public decimal Size { get; set; }
        [JsonProperty("product_id")]
        public string ProductId { get; set; }
        public string Side { get; set; }
        public string Stp { get; set; }
        public string Type { get; set; }
        [JsonProperty("fill_fees")]
        public decimal FillFees { get; set; }
        [JsonProperty("filled_size")]
        public decimal FilledSize { get; set; }
        [JsonProperty("executed_value")]
        public decimal ExecutedValue { get; set; }
        public string Status { get; set; }
        public bool Settled { get; set; }
    }

    public class Account
    {
        public string Id { get; set; }
        public string Currency { get; set; }
        public decimal Balance { get; set; }
        public decimal Hold { get; set; }
        public decimal Available { get; set; }
        [JsonProperty("profile_id")]
        public string ProfileId { get; set; }
    }

    public class Tick
    {
        [JsonProperty("product_id")]
        public string ProductId { get; set; }
        [JsonProperty("trade_id")]
        public string TradeId { get; set; }
        public decimal Price { get; set; }
        public decimal Size { get; set; }
        public decimal Bid { get; set; }
        public decimal Ask { get; set; }
        public decimal Volume { get; set; }
        public DateTime Time { get; set; }
    }

}

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

namespace QuantConnect.Brokerages.Bitfinex.Messages
{
    //several simple objects to facilitate json conversion
#pragma warning disable 1591

    public class BaseMessage
    {
        [JsonProperty("event")]
        public string Event { get; set; }
    }

    public class ErrorMessage: BaseMessage
    {
        [JsonProperty("msg")]
        public string Message { get; set; }

        public int Code { get; set; }

        /// <summary>
        /// 10301 : Already subscribed
        /// </summary>
        public string Level => Code == 10301 ? "Warning" : "Error";
    }

    public class ChannelSubscription : BaseMessage
    {
        public string Channel { get; set; }

        [JsonProperty("chanId")]
        public int ChannelId { get; set; }

        [JsonProperty("symbol")]
        public string Symbol { get; set; }
    }

    public class ChannelUnsubscribing : BaseMessage
    {
        public string Status { get; set; }

        [JsonProperty("chanId")]
        public int ChannelId { get; set; }
    }

    public class AuthResponseMessage: BaseMessage
    {
        public string Status { get; set; }
    }

    public class Candle
    {
        public long Timestamp { get; set; }
        public decimal Open { get; set; }
        public decimal Close { get; set; }
        public decimal High { get; set; }
        public decimal Low { get; set; }
        public decimal Volume { get; set; }

        public Candle() { }

        public Candle(long msts, decimal close)
        {
            Timestamp = msts;
            Open = Close = High = Low = close;
            Volume = 0;
        }

        public Candle(object[] entries)
        {
            Timestamp = entries[0].ConvertInvariant<long>();
            Open = entries[1].ConvertInvariant<decimal>();
            Close = entries[2].ConvertInvariant<decimal>();
            High = entries[3].ConvertInvariant<decimal>();
            Low = entries[4].ConvertInvariant<decimal>();
            Volume = entries[5].ConvertInvariant<decimal>();
        }
    }

#pragma warning restore 1591

}

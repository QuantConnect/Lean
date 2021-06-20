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

namespace QuantConnect.ToolBox.Polygon.History
{
    public class EquityTradeTickResponse
    {
        [JsonProperty("c")]
        public int[] Conditions { get; set; }

        [JsonProperty("i")]
        public string Id { get; set; }

        [JsonProperty("q")]
        public long SequenceNumber { get; set; }

        [JsonProperty("s")]
        public decimal Size { get; set; }

        [JsonProperty("p")]
        public decimal Price { get; set; }

        [JsonProperty("x")]
        public int Exchange { get; set; }

        [JsonProperty("t")]
        public long SipTimestamp { get; set; }

        [JsonProperty("y")]
        public long ExchangeTimestamp { get; set; }

        [JsonProperty("f")]
        public long TrfTimestamp { get; set; }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = SequenceNumber.GetHashCode();
                hashCode = (hashCode * 397) ^ ExchangeTimestamp.GetHashCode();
                hashCode = (hashCode * 397) ^ SipTimestamp.GetHashCode();
                return hashCode;
            }
        }
    }
}

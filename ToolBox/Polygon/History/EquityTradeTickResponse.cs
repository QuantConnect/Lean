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
        [JsonProperty("I")]
        public int? originalTradeID { get; set; } = null;
        [JsonProperty("x")]
        public int? Exchange { get; set; } = null;
        [JsonProperty("p")]
        public decimal Price { get; set; }
        [JsonProperty("i")]
        public string tradeID { get; set; } = "";
        [JsonProperty("e")]
        public int? tradeCorrectionIndicator { get; set; } = null;
        [JsonProperty("r")]
        public int? tradeIDReportingFacility { get; set; } = null;
        [JsonProperty("t")]
        public long nanosecondSIPTimeStamp { get; set; }
        [JsonProperty("y")]
        public long? nanosecondParticipantExchangeTimeStamp { get; set; } = null;
        [JsonProperty("f")]
        public long? nanosecondTradeReportingFacilityTimeStamp { get; set; } = null;
        [JsonProperty("q")]
        public long? sequenceNumber { get; set; } = null;
        [JsonProperty("c")]
        public int[] Conditions { get; set; }
        [JsonProperty("s")]
        public decimal Size { get; set; }
        [JsonProperty("z")]
        public int? tape { get; set; } = null;








    }
}

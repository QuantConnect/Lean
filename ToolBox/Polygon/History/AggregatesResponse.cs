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

using System.Collections.Generic;
using Newtonsoft.Json;

namespace QuantConnect.ToolBox.Polygon.History
{
    public class AggregatesResponse
    {
        [JsonProperty("ticker")]
        public string Ticker { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("queryCount")]
        public int QueryCount { get; set; }

        [JsonProperty("resultsCount")]
        public int ResultsCount { get; set; }

        [JsonProperty("adjusted")]
        public bool Adjusted { get; set; }

        [JsonProperty("results")]
        public List<AggregateRowResponse> Results { get; set; }

        [JsonProperty("request_id")]
        public string RequestId { get; set; }
    }
}

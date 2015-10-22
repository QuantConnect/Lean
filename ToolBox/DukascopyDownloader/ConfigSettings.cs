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

using System;
using Newtonsoft.Json;

namespace QuantConnect.ToolBox.DukascopyDownloader
{
    /// <summary>
    /// Represents the configuration settings for the application
    /// </summary>
    public class ConfigSettings
    {
        [JsonProperty("output-folder")]
        public string OutputFolder { get; set; }

        [JsonProperty("instrument-list")]
        public string[] InstrumentList { get; set; }

        [JsonProperty("start-date")]
        public DateTime StartDate { get; set; }

        [JsonProperty("end-date")]
        public DateTime EndDate { get; set; }

        [JsonProperty("output-format")]
        public string OutputFormat { get; set; }

        [JsonProperty("enable-trace")]
        public bool EnableTrace { get; set; }
    }
}
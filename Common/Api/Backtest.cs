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
using System.Collections.Generic;
using Newtonsoft.Json;
using QuantConnect.Packets;

namespace QuantConnect.Api
{
    /// <summary>
    /// Backtest response packet from the QuantConnect.com API.
    /// </summary>
    public class Backtest : RestResponse
    {
        /// <summary>
        /// Name of the backtest
        /// </summary>
        [JsonProperty(PropertyName = "name")]
        public string Name;

        /// <summary>
        /// Note on the backtest attached by the user
        /// </summary>
        [JsonProperty(PropertyName = "note")]
        public string Note;

        /// <summary>
        /// Assigned backtest Id
        /// </summary>
        [JsonProperty(PropertyName = "backtestId")]
        public string BacktestId;

        /// <summary>
        /// Boolean true when the backtest is completed.
        /// </summary>
        [JsonProperty(PropertyName = "completed")]
        public bool Completed;

        /// <summary>
        /// Progress of the backtest in percent 0-1.
        /// </summary>
        [JsonProperty(PropertyName = "progress")]
        public decimal Progress;

        /// <summary>
        /// Result packet for the backtest
        /// </summary>
        [JsonProperty(PropertyName = "result")]
        public BacktestResult Result;

        /// <summary>
        /// Backtest error message
        /// </summary>
        [JsonProperty(PropertyName = "error")]
        public string Error;

        /// <summary>
        /// Backtest error stacktrace
        /// </summary>
        [JsonProperty(PropertyName = "stacktrace")]
        public string StackTrace;

        /// <summary>
        /// Backtest creation date and time
        /// </summary>
        [JsonProperty(PropertyName = "created")]
        public DateTime Created;
    }

    /// <summary>
    /// Collection container for a list of backtests for a project
    /// </summary>
    public class BacktestList : RestResponse
    {
        /// <summary>
        /// Collection of summarized backtest objects
        /// </summary>
        [JsonProperty(PropertyName = "backtests")]
        public List<Backtest> Backtests; 
    }
}
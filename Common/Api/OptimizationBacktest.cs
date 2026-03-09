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
using System.Collections.Generic;
using QuantConnect.Optimizer.Parameters;

namespace QuantConnect.Api
{
    /// <summary>
    /// OptimizationBacktest object from the QuantConnect.com API.
    /// </summary>
    [JsonConverter(typeof(OptimizationBacktestJsonConverter))]
    public class OptimizationBacktest
    {
        /// <summary>
        /// Progress of the backtest as a percentage from 0-1 based on the days lapsed from start-finish.
        /// </summary>
        public decimal Progress { get; set; }

        /// <summary>
        /// The backtest name
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The backtest host name
        /// </summary>
        public string HostName { get; set; }

        /// <summary>
        /// The backtest id
        /// </summary>
        public string BacktestId { get; }

        /// <summary>
        /// Represent a combination as key value of parameters, i.e. order doesn't matter
        /// </summary>
        public ParameterSet ParameterSet { get; }

        /// <summary>
        /// The backtest statistics results
        /// </summary>
        public IDictionary<string, string> Statistics { get; set; }

        /// <summary>
        /// The backtest equity chart series
        /// </summary>
        public CandlestickSeries Equity { get; set; }

        /// <summary>
        /// The exit code of this backtest
        /// </summary>
        public int ExitCode { get; set; }

        /// <summary>
        /// Backtest maximum end date
        /// </summary>
        public DateTime? OutOfSampleMaxEndDate { get; set; }

        /// <summary>
        /// The backtest out of sample day count
        /// </summary>
        public int OutOfSampleDays { get; set; }

        /// <summary>
        /// The backtest start date
        /// </summary>
        public DateTime StartDate { get; set; }

        /// <summary>
        /// The backtest end date
        /// </summary>
        public DateTime EndDate { get; set; }

        /// <summary>
        /// Creates a new instance
        /// </summary>
        /// <param name="parameterSet">The parameter set</param>
        /// <param name="backtestId">The backtest id if any</param>
        /// <param name="name">The backtest name</param>
        public OptimizationBacktest(ParameterSet parameterSet, string backtestId, string name)
        {
            ParameterSet = parameterSet;
            BacktestId = backtestId;
            Name = name;
        }
    }
}

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
using QuantConnect.Util;
using QuantConnect.Packets;
using QuantConnect.Interfaces;
using QuantConnect.Brokerages;
using System.Collections.Generic;

namespace QuantConnect
{
    /// <summary>
    /// This class includes algorithm configuration settings and parameters.
    /// This is used to include configuration parameters in the result packet to be used for report generation.
    /// </summary>
    public class AlgorithmConfiguration
    {
        /// <summary>
        /// The algorithm's name
        /// </summary>
        public string Name;

        /// <summary>
        /// List of tags associated with the algorithm
        /// </summary>
        public ISet<string> Tags;

        /// <summary>
        /// The algorithm's account currency
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string AccountCurrency;

        /// <summary>
        /// The algorithm's brokerage model
        /// </summary>
        /// <remarks> Required to set the correct brokerage model on report generation.</remarks>
        public BrokerageName Brokerage;

        /// <summary>
        /// The algorithm's account type
        /// </summary>
        /// <remarks> Required to set the correct brokerage model on report generation.</remarks>
        public AccountType AccountType;

        /// <summary>
        /// The parameters used by the algorithm
        /// </summary>
        public IReadOnlyDictionary<string, string> Parameters;

        /// <summary>
        /// Backtest maximum end date
        /// </summary>
        public DateTime? OutOfSampleMaxEndDate;

        /// <summary>
        /// The backtest out of sample day count
        /// </summary>
        public int OutOfSampleDays;

        /// <summary>
        /// The backtest start date
        /// </summary>
        [JsonConverter(typeof(DateTimeJsonConverter), DateFormat.UI)]
        public DateTime StartDate;

        /// <summary>
        /// The backtest end date
        /// </summary>
        [JsonConverter(typeof(DateTimeJsonConverter), DateFormat.UI)]
        public DateTime EndDate;

        /// <summary>
        /// Number of trading days per year for Algorithm's portfolio statistics.
        /// </summary>
        public int TradingDaysPerYear;

        /// <summary>
        /// Initializes a new instance of the <see cref="AlgorithmConfiguration"/> class
        /// </summary>
        public AlgorithmConfiguration(string name, ISet<string> tags, string accountCurrency, BrokerageName brokerageName,
            AccountType accountType, IReadOnlyDictionary<string, string> parameters, DateTime startDate, DateTime endDate,
            DateTime? outOfSampleMaxEndDate, int outOfSampleDays = 0, int tradingDaysPerYear = 0)
        {
            Name = name;
            Tags = tags;
            OutOfSampleMaxEndDate = outOfSampleMaxEndDate;
            TradingDaysPerYear = tradingDaysPerYear;
            OutOfSampleDays = outOfSampleDays;
            AccountCurrency = accountCurrency;
            Brokerage = brokerageName;
            AccountType = accountType;
            Parameters = parameters;
            StartDate = startDate;
            EndDate = endDate;
        }

        /// <summary>
        /// Initializes a new empty instance of the <see cref="AlgorithmConfiguration"/> class
        /// </summary>
        public AlgorithmConfiguration()
        {
            // use default value for backwards compatibility
            TradingDaysPerYear = 252;
        }

        /// <summary>
        /// Provides a convenience method for creating a <see cref="AlgorithmConfiguration"/> for a given algorithm.
        /// </summary>
        /// <param name="algorithm">Algorithm for which the configuration object is being created</param>
        /// <param name="backtestNodePacket">The associated backtest node packet if any</param>
        /// <returns>A new AlgorithmConfiguration object for the specified algorithm</returns>
        public static AlgorithmConfiguration Create(IAlgorithm algorithm, BacktestNodePacket backtestNodePacket)
        {
            return new AlgorithmConfiguration(
                algorithm.Name,
                algorithm.Tags,
                algorithm.AccountCurrency,
                BrokerageModel.GetBrokerageName(algorithm.BrokerageModel),
                algorithm.BrokerageModel.AccountType,
                algorithm.GetParameters(),
                algorithm.StartDate,
                algorithm.EndDate,
                backtestNodePacket?.OutOfSampleMaxEndDate,
                backtestNodePacket?.OutOfSampleDays ?? 0,
                // use value = 252 like default for backwards compatibility
                algorithm?.Settings?.TradingDaysPerYear ?? 252);
        }
    }
}

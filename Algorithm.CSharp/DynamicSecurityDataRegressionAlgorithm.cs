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
 *
*/

using System;
using System.Collections.Generic;
using System.IO;
using QuantConnect.Data;
using QuantConnect.Data.Custom.IconicTypes;
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;
using QuantConnect.Securities;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Provides an example algorithm showcasing the <see cref="Security.Data"/> features
    /// </summary>
    public class DynamicSecurityDataRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Security Equity;
        private const string Ticker = "GOOGL";

        public override void Initialize()
        {
            SetStartDate(2015, 10, 22);
            SetEndDate(2015, 10, 30);

            Equity = AddEquity(Ticker, Resolution.Daily);
            var customLinkedEquity = AddData<LinkedData>(Ticker, Resolution.Daily).Symbol;

            // Adding linked data manually to cache for example purposes, since
            // LinkedData is a type used for testing and doesn't point to any real data.
            Equity.Cache.AddDataList(new List<LinkedData>
            {
                new LinkedData
                {
                    Count = 100,

                    Symbol = customLinkedEquity,
                    EndTime = StartDate,
                },
                new LinkedData
                {
                    Count = 50,
                    
                    Symbol = customLinkedEquity,
                    EndTime = StartDate
                }
            }, typeof(LinkedData), false);
        }

        public override void OnData(Slice slice)
        {
            // The Security object's Data property provides convenient access
            // to the various types of data related to that security. You can
            // access not only the security's price data, but also any custom
            // data that is mapped to the security, such as our SEC reports.

            // 1. Get the most recent data point of a particular type:
            // 1.a Using the C# generic method, Get<T>:
            LinkedData customLinkedData = Equity.Data.Get<LinkedData>();
            Log($"{Time:o}: LinkedData: {customLinkedData}");

            // 2. Get the list of data points of a particular type for the most recent time step:
            // 2.a Using the C# generic method, GetAll<T>:
            List<LinkedData> customLinkedDataList = Equity.Data.GetAll<LinkedData>();
            Log($"{Time:o}: List: LinkedData: {customLinkedDataList.Count}");

            if (!Portfolio.Invested)
            {
                Buy(Equity.Symbol, 10);
            }
        }
        
        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public List<Language> Languages { get; } = new() { Language.CSharp, Language.Python };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 65;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// Final status of the algorithm
        /// </summary>
        public AlgorithmStatus AlgorithmStatus => AlgorithmStatus.Completed;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "1"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "-4.847%"},
            {"Drawdown", "0.300%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "99882.1"},
            {"Net Profit", "-0.118%"},
            {"Sharpe Ratio", "-2.151"},
            {"Sortino Ratio", "-1.743"},
            {"Probabilistic Sharpe Ratio", "30.061%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0.008"},
            {"Beta", "-0.104"},
            {"Annual Standard Deviation", "0.02"},
            {"Annual Variance", "0"},
            {"Information Ratio", "-5.063"},
            {"Tracking Error", "0.108"},
            {"Treynor Ratio", "0.423"},
            {"Total Fees", "$1.00"},
            {"Estimated Strategy Capacity", "$1600000000.00"},
            {"Lowest Capacity Asset", "GOOG T1AZ164W5VTX"},
            {"Portfolio Turnover", "0.83%"},
            {"OrderListHash", "ffab48ec7d6bf58aae9377c4bdf3be02"}
        };
    }
}

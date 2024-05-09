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
using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Securities;
using System.Collections.Generic;
using QuantConnect.Data.UniverseSelection;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm reproducing GH issue #5921. Asserting a security can be warmup correctly on initialize
    /// </summary>
    public class SecuritySeederRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2013, 10, 08);
            SetEndDate(2013, 10, 10);
            SetSecurityInitializer(new BrokerageModelSecurityInitializer(BrokerageModel,
                new FuncSecuritySeeder(GetLastKnownPrices)));
            AddEquity("SPY", Resolution.Minute);
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="data">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice data)
        {
            if (!Portfolio.Invested)
            {
                SetHoldings("SPY", 1);
            }
        }

        public override void OnSecuritiesChanged(SecurityChanges changes)
        {
            foreach (var addedSecurity in changes.AddedSecurities)
            {
                if (!addedSecurity.HasData
                    || addedSecurity.AskPrice == 0
                    || addedSecurity.BidPrice == 0
                    || addedSecurity.BidSize == 0
                    || addedSecurity.AskSize == 0
                    || addedSecurity.Price == 0
                    || addedSecurity.Volume == 0
                    || addedSecurity.High == 0
                    || addedSecurity.Low == 0
                    || addedSecurity.Open == 0
                    || addedSecurity.Close == 0)
                {
                    throw new Exception($"Security {addedSecurity.Symbol} was not warmed up!");
                }
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public Language[] Languages { get; } = { Language.CSharp, Language.Python };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 2369;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 10;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "1"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "307.471%"},
            {"Drawdown", "1.700%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "101031.62"},
            {"Net Profit", "1.032%"},
            {"Sharpe Ratio", "66.263"},
            {"Sortino Ratio", "0"},
            {"Probabilistic Sharpe Ratio", "0%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "-0.116"},
            {"Beta", "0.996"},
            {"Annual Standard Deviation", "0.242"},
            {"Annual Variance", "0.058"},
            {"Information Ratio", "-198.985"},
            {"Tracking Error", "0.001"},
            {"Treynor Ratio", "16.083"},
            {"Total Fees", "$3.44"},
            {"Estimated Strategy Capacity", "$31000000.00"},
            {"Lowest Capacity Asset", "SPY R735QTJ8XC9X"},
            {"Portfolio Turnover", "33.62%"},
            {"OrderListHash", "00636a25aed88acd2171c6221c747716"}
        };
    }
}

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
using QuantConnect.Algorithm.Framework.Alphas;
using QuantConnect.Data;
using QuantConnect.Brokerages;
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// This algorithm showcases an <see cref="AccountType.Cash"/> emitting insights
    /// and manually trading.
    /// </summary>
    public class EmitInsightCryptoCashAccountType : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _symbol;

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2018, 4, 4); // Set Start Date
            SetEndDate(2018, 4, 4); // Set End Date
            SetAccountCurrency("EUR");
            SetCash("EUR", 10000);
            _symbol = AddCrypto("BTCEUR").Symbol;

            SetBrokerageModel(BrokerageName.GDAX, AccountType.Cash);
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="data">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice data)
        {
            if (!Portfolio.Invested)
            {
                EmitInsights(Insight.Price(_symbol, Resolution.Daily, 1, InsightDirection.Up));
                SetHoldings(_symbol, 0.5);
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public Language[] Languages { get; } = { Language.CSharp };

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "1"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "-100.000%"},
            {"Drawdown", "5.500%"},
            {"Expectancy", "0"},
            {"Net Profit", "-3.799%"},
            {"Sharpe Ratio", "-12.079"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "-9.247"},
            {"Beta", "2.97"},
            {"Annual Standard Deviation", "0.397"},
            {"Annual Variance", "0.157"},
            {"Information Ratio", "-23.909"},
            {"Tracking Error", "0.263"},
            {"Treynor Ratio", "-1.614"},
            {"Total Fees", "$14.91"},
            {"Total Insights Generated", "1"},
            {"Total Insights Closed", "1"},
            {"Total Insights Analysis Completed", "1"},
            {"Long Insight Count", "1"},
            {"Short Insight Count", "0"},
            {"Long/Short Ratio", "100%"},
            {"Estimated Monthly Alpha Value", "€-7.1039"},
            {"Total Accumulated Estimated Alpha Value", "€-0.2762628"},
            {"Mean Population Estimated Insight Value", "€-0.2762628"},
            {"Mean Population Direction", "0%"},
            {"Mean Population Magnitude", "0%"},
            {"Rolling Averaged Population Direction", "0%"},
            {"Rolling Averaged Population Magnitude", "0%"}
        };
    }
}

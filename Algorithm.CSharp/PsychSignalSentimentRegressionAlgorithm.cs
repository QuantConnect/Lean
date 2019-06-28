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
using QuantConnect.Data;
using QuantConnect.Data.Custom.PsychSignal;
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// This example algorithm shows how to import and use psychsignal sentiment data.
    /// </summary>
    /// <meta name="tag" content="strategy example" />
    /// <meta name="tag" content="using data" />
    /// <meta name="tag" content="custom data" />
    /// <meta name="tag" content="psychsignal" />
    /// <meta name="tag" content="sentiment" />
    public class PsychSignalSentimentRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private const string Ticker = "AAPL";
        private Symbol _symbol;

        /// <summary>
        /// Initialize the algorithm with our custom data
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2019, 6, 3);
            SetEndDate(2019, 6, 9);
            SetCash(100000);

            _symbol = AddEquity(Ticker).Symbol;
            AddData<PsychSignalSentimentData>(Ticker);
        }

        /// <summary>
        /// Loads each new data point into the algorithm. On sentiment data, we place orders depending on the sentiment
        /// </summary>
        /// <param name="slice">Slice object containing the sentiment data</param>
        public override void OnData(Slice slice)
        {
            foreach (var message in slice.Get<PsychSignalSentimentData>().Values)
            {
                if (!Portfolio.Invested && Transactions.GetOpenOrders().Count == 0 && slice.ContainsKey(_symbol) && 
                    message.BullIntensity > 1.5m && message.BullScoredMessages > 3.0m)
                {
                    SetHoldings(_symbol, 0.25);
                }
                else if (Portfolio.Invested && message.BearIntensity > 1.5m && message.BearScoredMessages > 3.0m)
                {
                    Liquidate(_symbol);
                }
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = false;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public Language[] Languages { get; } = { Language.CSharp, Language.Python };

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "9"},
            {"Average Win", "0.32%"},
            {"Average Loss", "-0.05%"},
            {"Compounding Annual Return", "59.645%"},
            {"Drawdown", "0.700%"},
            {"Expectancy", "4.327"},
            {"Net Profit", "0.901%"},
            {"Sharpe Ratio", "6.985"},
            {"Loss Rate", "25%"},
            {"Win Rate", "75%"},
            {"Profit-Loss Ratio", "6.10"},
            {"Alpha", "-0.901"},
            {"Beta", "98.136"},
            {"Annual Standard Deviation", "0.041"},
            {"Annual Variance", "0.002"},
            {"Information Ratio", "6.725"},
            {"Tracking Error", "0.04"},
            {"Treynor Ratio", "0.003"},
            {"Total Fees", "$9.00"}
        };
    }
}

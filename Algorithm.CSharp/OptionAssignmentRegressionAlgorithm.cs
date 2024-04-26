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

using System.Collections.Generic;
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Securities;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// This regression algorithm verifies automatic option contract assignment behavior.
    /// </summary>
    /// <meta name="tag" content="regression test" />
    /// <meta name="tag" content="options" />
    /// <meta name="tag" content="using data" />
    /// <meta name="tag" content="filter selection" />
    public class OptionAssignmentRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Security Stock;

        private Security CallOption;
        private Symbol CallOptionSymbol;

        private Security PutOption;
        private Symbol PutOptionSymbol;

        public override void Initialize()
        {
            SetStartDate(2015, 12, 23);
            SetEndDate(2015, 12, 28);
            SetCash(100000);
            Stock = AddEquity("GOOG", Resolution.Minute);

            var contracts = OptionChainProvider.GetOptionContractList(Stock.Symbol, UtcTime).ToList();

            PutOptionSymbol = contracts
                .Where(c => c.ID.OptionRight == OptionRight.Put)
                .OrderBy(c => c.ID.Date)
                .First(c => c.ID.StrikePrice == 800m);

            CallOptionSymbol = contracts
                .Where(c => c.ID.OptionRight == OptionRight.Call)
                .OrderBy(c => c.ID.Date)
                .First(c => c.ID.StrikePrice == 600m);

            PutOption = AddOptionContract(PutOptionSymbol);
            CallOption = AddOptionContract(CallOptionSymbol);
        }

        public override void OnData(Slice data)
        {
            if (!Portfolio.Invested && Stock.Price != 0 && PutOption.Price != 0 && CallOption.Price != 0)
            {
                // this gets executed on start and after each auto-assignment, finally ending with expiration assignment
                if (Time < PutOptionSymbol.ID.Date)
                {
                    MarketOrder(PutOptionSymbol, -1);
                }

                if (Time < CallOptionSymbol.ID.Date)
                {
                    MarketOrder(CallOptionSymbol, -1);
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
        public virtual Language[] Languages { get; } = { Language.CSharp };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 4025;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public virtual Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "32"},
            {"Average Win", "9.60%"},
            {"Average Loss", "-16.91%"},
            {"Compounding Annual Return", "-84.836%"},
            {"Drawdown", "2.900%"},
            {"Expectancy", "-0.608"},
            {"Start Equity", "100000"},
            {"End Equity", "97114"},
            {"Net Profit", "-2.886%"},
            {"Sharpe Ratio", "-7.473"},
            {"Sortino Ratio", "0"},
            {"Probabilistic Sharpe Ratio", "1.125%"},
            {"Loss Rate", "75%"},
            {"Win Rate", "25%"},
            {"Profit-Loss Ratio", "0.57"},
            {"Alpha", "-0.016"},
            {"Beta", "0.458"},
            {"Annual Standard Deviation", "0.014"},
            {"Annual Variance", "0"},
            {"Information Ratio", "5.991"},
            {"Tracking Error", "0.015"},
            {"Treynor Ratio", "-0.229"},
            {"Total Fees", "$16.00"},
            {"Estimated Strategy Capacity", "$710000.00"},
            {"Lowest Capacity Asset", "GOOCV 305RBQ20WHPNQ|GOOCV VP83T1ZUHROL"},
            {"Portfolio Turnover", "218.80%"},
            {"OrderListHash", "84dbd164000f9fb9096c14cbcd0b3e15"}
        };
    }
}

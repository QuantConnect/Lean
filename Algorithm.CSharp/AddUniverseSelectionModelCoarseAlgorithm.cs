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
using System.Linq;
using QuantConnect.Algorithm.Framework.Alphas;
using QuantConnect.Algorithm.Framework.Execution;
using QuantConnect.Algorithm.Framework.Portfolio;
using QuantConnect.Algorithm.Framework.Selection;
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Test algorithm using <see cref="QCAlgorithm.AddUniverseSelection(IUniverseSelectionModel)"/>
    /// </summary>
    public class AddUniverseSelectionModelCoarseAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            // Set requested data resolution
            UniverseSettings.Resolution = Resolution.Daily;

            // Order margin value has to have a minimum of 0.5% of Portfolio value, allows filtering out small trades and reduce fees.
            // Commented so regression algorithm is more sensitive
            //Settings.MinimumOrderMarginPortfolioPercentage = 0.005m;

            SetStartDate(2014, 03, 24);
            SetEndDate(2014, 04, 07);
            SetCash(100000);

            // set algorithm framework models
            SetAlpha(new ConstantAlphaModel(InsightType.Price, InsightDirection.Up, TimeSpan.FromMinutes(20), 0.025, null));
            SetPortfolioConstruction(new EqualWeightingPortfolioConstructionModel());
            SetExecution(new ImmediateExecutionModel());

            SetUniverseSelection(new CoarseFundamentalUniverseSelectionModel(
                enumerable => enumerable
                    .Select(fundamental => fundamental.Symbol)
                    .Where(symbol => symbol.Value == "AAPL")));
            AddUniverseSelection(new CoarseFundamentalUniverseSelectionModel(
                enumerable => enumerable
                    .Select(fundamental => fundamental.Symbol)
                    .Where(symbol => symbol.Value == "SPY")));
            AddUniverseSelection(new CoarseFundamentalUniverseSelectionModel(
                enumerable => enumerable
                    .Select(fundamental => fundamental.Symbol)
                    .Where(symbol => symbol.Value == "FB")));
        }

        public override void OnEndOfAlgorithm()
        {
            if (UniverseManager.Count != 3)
            {
                throw new Exception("Unexpected universe count");
            }
            if (UniverseManager.ActiveSecurities.Count != 3
                || UniverseManager.ActiveSecurities.Keys.All(symbol => symbol.Value != "SPY")
                || UniverseManager.ActiveSecurities.Keys.All(symbol => symbol.Value != "AAPL")
                || UniverseManager.ActiveSecurities.Keys.All(symbol => symbol.Value != "FB"))
            {
                throw new Exception("Unexpected active securities");
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
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 234018;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "23"},
            {"Average Win", "0.00%"},
            {"Average Loss", "-0.01%"},
            {"Compounding Annual Return", "-75.275%"},
            {"Drawdown", "5.800%"},
            {"Expectancy", "-0.609"},
            {"Start Equity", "100000"},
            {"End Equity", "94419.21"},
            {"Net Profit", "-5.581%"},
            {"Sharpe Ratio", "-3.288"},
            {"Sortino Ratio", "-3.828"},
            {"Probabilistic Sharpe Ratio", "5.546%"},
            {"Loss Rate", "73%"},
            {"Win Rate", "27%"},
            {"Profit-Loss Ratio", "0.43"},
            {"Alpha", "-0.495"},
            {"Beta", "1.484"},
            {"Annual Standard Deviation", "0.196"},
            {"Annual Variance", "0.039"},
            {"Information Ratio", "-3.843"},
            {"Tracking Error", "0.141"},
            {"Treynor Ratio", "-0.435"},
            {"Total Fees", "$31.25"},
            {"Estimated Strategy Capacity", "$550000000.00"},
            {"Lowest Capacity Asset", "AAPL R735QTJ8XC9X"},
            {"Portfolio Turnover", "7.33%"},
            {"OrderListHash", "2add92a1f922c6730d8c20ff65934a46"}
        };
    }
}

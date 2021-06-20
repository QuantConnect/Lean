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

using QuantConnect.Data.UniverseSelection;
using QuantConnect.Interfaces;
using QuantConnect.Orders.Fees;
using System;
using System.Collections.Generic;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// In this algorithm we demonstrate how to use the UniverseSettings
    /// to define the data normalization mode (raw)
    /// </summary>
    /// <meta name="tag" content="using data" />
    /// <meta name="tag" content="universes" />
    /// <meta name="tag" content="coarse universes" />
    /// <meta name="tag" content="regression test" />
    public class RawPricesUniverseRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        public override void Initialize()
        {
            // what resolution should the data *added* to the universe be?
            UniverseSettings.Resolution = Resolution.Daily;

            // Use raw prices
            UniverseSettings.DataNormalizationMode = DataNormalizationMode.Raw;

            SetStartDate(2014,3,24);
            SetEndDate(2014,4,7);
            SetCash(50000);

            // Set the security initializer with zero fees
            SetSecurityInitializer(x => x.SetFeeModel(new ConstantFeeModel(0)));

            AddUniverse("MyUniverse", Resolution.Daily, SelectionFunction);
        }

        public IEnumerable<string> SelectionFunction(DateTime dateTime)
        {
            return dateTime.Day % 2 == 0
                ? new[] { "SPY", "IWM", "QQQ" }
                : new[] { "AIG", "BAC", "IBM" };
        }

        // this event fires whenever we have changes to our universe
        public override void OnSecuritiesChanged(SecurityChanges changes)
        {
            foreach (var security in changes.RemovedSecurities)
            {
                if (security.Invested)
                {
                    Liquidate(security.Symbol);
                }
            }

            // we want 20% allocation in each security in our universe
            foreach (var security in changes.AddedSecurities)
            {
                SetHoldings(security.Symbol, 0.2m);
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
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "27"},
            {"Average Win", "0.21%"},
            {"Average Loss", "-0.21%"},
            {"Compounding Annual Return", "-7.917%"},
            {"Drawdown", "0.800%"},
            {"Expectancy", "-0.009"},
            {"Net Profit", "-0.338%"},
            {"Sharpe Ratio", "-1.428"},
            {"Probabilistic Sharpe Ratio", "27.774%"},
            {"Loss Rate", "50%"},
            {"Win Rate", "50%"},
            {"Profit-Loss Ratio", "0.98"},
            {"Alpha", "-0.063"},
            {"Beta", "0.002"},
            {"Annual Standard Deviation", "0.044"},
            {"Annual Variance", "0.002"},
            {"Information Ratio", "0.348"},
            {"Tracking Error", "0.11"},
            {"Treynor Ratio", "-37.899"},
            {"Total Fees", "$0.00"},
            {"Estimated Strategy Capacity", "$80000000.00"},
            {"Lowest Capacity Asset", "AIG R735QTJ8XC9X"},
            {"Fitness Score", "0.068"},
            {"Kelly Criterion Estimate", "0"},
            {"Kelly Criterion Probability Value", "0"},
            {"Sortino Ratio", "-1.314"},
            {"Return Over Maximum Drawdown", "-9.513"},
            {"Portfolio Turnover", "0.413"},
            {"Total Insights Generated", "0"},
            {"Total Insights Closed", "0"},
            {"Total Insights Analysis Completed", "0"},
            {"Long Insight Count", "0"},
            {"Short Insight Count", "0"},
            {"Long/Short Ratio", "100%"},
            {"Estimated Monthly Alpha Value", "$0"},
            {"Total Accumulated Estimated Alpha Value", "$0"},
            {"Mean Population Estimated Insight Value", "$0"},
            {"Mean Population Direction", "0%"},
            {"Mean Population Magnitude", "0%"},
            {"Rolling Averaged Population Direction", "0%"},
            {"Rolling Averaged Population Magnitude", "0%"},
            {"OrderListHash", "5d42c2ef288fa9b3f8bf5fa8adc47a6c"}
        };
    }
}

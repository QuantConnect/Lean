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
using System.Linq;
using QuantConnect.Algorithm.Framework.Risk;
using QuantConnect.Algorithm.Framework.Selection;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Show example of how to use the <see cref="MaximumSectorExposureRiskManagementModel"/> Risk Management Model
    /// </summary>
    public class MaximumSectorExposureRiskManagementModelFrameworkRegressionAlgorithm : BaseFrameworkRegressionAlgorithm
    {
        public override void Initialize()
        {
            base.Initialize();

            // Set requested data resolution
            UniverseSettings.Resolution = Resolution.Daily;

            SetStartDate(2014, 2, 1);  //Set Start Date
            SetEndDate(2014, 5, 1);    //Set End Date

            // set algorithm framework models
            var tickers = new string[] { "AAPL", "MSFT", "GOOG", "AIG", "BAC" };
            SetUniverseSelection(new FineFundamentalUniverseSelectionModel(
                coarse => coarse.Where(x => tickers.Contains(x.Symbol.Value)).Select(x => x.Symbol),
                fine => fine.Select(x => x.Symbol)
            ));

            // define risk management model such that maximum weight of a single sector be 10%
            // Number of of trades changed from 34 to 30 when using the MaximumSectorExposureRiskManagementModel
            SetRiskManagement(new MaximumSectorExposureRiskManagementModel(0.1m));
        }

        public override void OnEndOfAlgorithm()
        {
            // The MaximumSectorExposureRiskManagementModel does not expire insights
        }

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public override long DataPoints => 544;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public override Dictionary<string, string> ExpectedStatistics => new()
        {
            {"Total Trades", "30"},
            {"Average Win", "0.01%"},
            {"Average Loss", "-0.07%"},
            {"Compounding Annual Return", "-3.542%"},
            {"Drawdown", "1.300%"},
            {"Expectancy", "-0.909"},
            {"Net Profit", "-0.885%"},
            {"Sharpe Ratio", "-1.538"},
            {"Probabilistic Sharpe Ratio", "3.669%"},
            {"Loss Rate", "92%"},
            {"Win Rate", "8%"},
            {"Profit-Loss Ratio", "0.18"},
            {"Alpha", "-0.036"},
            {"Beta", "0.06"},
            {"Annual Standard Deviation", "0.016"},
            {"Annual Variance", "0"},
            {"Information Ratio", "-2.295"},
            {"Tracking Error", "0.093"},
            {"Treynor Ratio", "-0.411"},
            {"Total Fees", "$33.89"},
            {"Estimated Strategy Capacity", "$44000000.00" },
            {"Lowest Capacity Asset", "AIG R735QTJ8XC9X"},
            {"Portfolio Turnover", "0.81%"},
            {"OrderListHash", "a4abf04ae5be2437253d76a96c349335" }
        };
    }
}

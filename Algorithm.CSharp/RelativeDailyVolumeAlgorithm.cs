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

from AlgorithmImports import *

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Relative Daily Volume Algorithm that uses EnableAutomaticIndicatorWarmUp
    /// </summary>
    /// <meta name="tag" content="using data" />
    /// <meta name="tag" content="using quantconnect" />
    /// <meta name="tag" content="trading and orders" />
    public class RelativeDailyVolumeAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        private RelativeDailyVolume _rdv;
        private Symbol _symbol;
        public override void Initialize()
        {
            // Set requested data resolution
            UniverseSettings.Resolution = Resolution.Hour;

            SetStartDate(2013, 10, 07);  //Set Start Date
            SetEndDate(2013, 10, 20);    //Set End Date
            SetCash(100000);             //Set Strategy Cash

            EnableAutomaticIndicatorWarmUp = true;
            _symbol = AddEquity("SPY", Resolution.Hour).Symbol;
            _rdv = RDV(_symbol, 2, Resolution.Hour);
        }

        public override void OnData(Slice slice)
        {
            if (_rdv.Current.Value > 1 & !Portfolio[_symbol].Invested)
            {
                SetHoldings(_symbol, 1);
            }
            else if (_rdv.Current.Value <= 1 & Portfolio[_symbol].Invested)
            {
                Liquidate(_symbol);
            }
        }

        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            if (orderEvent.Status.IsFill())
            {
                Debug($"Purchased Stock: {orderEvent.Symbol}");
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
            {"Total Trades", "9"},
            {"Average Win", "0.50%"},
            {"Average Loss", "-0.12%"},
            {"Compounding Annual Return", "53.616%"},
            {"Drawdown", "1.500%"},
            {"Expectancy", "2.831"},
            {"Net Profit", "1.382%"},
            {"Sharpe Ratio", "3.36"},
            {"Probabilistic Sharpe Ratio", "62.834%"},
            {"Loss Rate", "25%"},
            {"Win Rate", "75%"},
            {"Profit-Loss Ratio", "4.11"},
            {"Alpha", "-1.301"},
            {"Beta", "0.828"},
            {"Annual Standard Deviation", "0.143"},
            {"Annual Variance", "0.021"},
            {"Information Ratio", "-27.416"},
            {"Tracking Error", "0.061"},
            {"Treynor Ratio", "0.582"},
            {"Total Fees", "$28.97"},
            {"Fitness Score", "0.758"},
            {"Kelly Criterion Estimate", "0"},
            {"Kelly Criterion Probability Value", "0"},
            {"Sortino Ratio", "7.52"},
            {"Return Over Maximum Drawdown", "52.891"},
            {"Portfolio Turnover", "0.774"},
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
            {"OrderListHash", "544670850"}
        };
    }
}

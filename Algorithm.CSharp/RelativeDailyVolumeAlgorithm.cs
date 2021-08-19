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
using QuantConnect.Orders;
using QuantConnect.Interfaces;
using QuantConnect.Data;
using QuantConnect.Indicators;

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
            {"Compounding Annual Return", "53.252%"},
            {"Drawdown", "1.500%"},
            {"Expectancy", "2.820"},
            {"Net Profit", "1.374%"},
            {"Sharpe Ratio", "3.363"},
            {"Probabilistic Sharpe Ratio", "62.865%"},
            {"Loss Rate", "25%"},
            {"Win Rate", "75%"},
            {"Profit-Loss Ratio", "4.09"},
            {"Alpha", "-1.263"},
            {"Beta", "0.808"},
            {"Annual Standard Deviation", "0.142"},
            {"Annual Variance", "0.02"},
            {"Information Ratio", "-26.38"},
            {"Tracking Error", "0.064"},
            {"Treynor Ratio", "0.592"},
            {"Total Fees", "$30.61"},
            {"Estimated Strategy Capacity", "$98000000.00"},
            {"Lowest Capacity Asset", "SPY R735QTJ8XC9X"},
            {"Fitness Score", "0.758"},
            {"Kelly Criterion Estimate", "0"},
            {"Kelly Criterion Probability Value", "0"},
            {"Sortino Ratio", "7.512"},
            {"Return Over Maximum Drawdown", "52.819"},
            {"Portfolio Turnover", "0.773"},
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
            {"OrderListHash", "912d37ecb278ffa5ccaa636ec1dccdcd"}
        };
    }
}

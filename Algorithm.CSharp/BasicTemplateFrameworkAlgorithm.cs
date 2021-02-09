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
using QuantConnect.Algorithm.Framework.Alphas;
using QuantConnect.Algorithm.Framework.Execution;
using QuantConnect.Algorithm.Framework.Portfolio;
using QuantConnect.Algorithm.Framework.Risk;
using QuantConnect.Algorithm.Framework.Selection;
using QuantConnect.Orders;
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Basic template framework algorithm uses framework components to define the algorithm.
    /// </summary>
    /// <meta name="tag" content="using data" />
    /// <meta name="tag" content="using quantconnect" />
    /// <meta name="tag" content="trading and orders" />
    public class BasicTemplateFrameworkAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            // Set requested data resolution
            UniverseSettings.Resolution = Resolution.Minute;

            SetStartDate(2013, 10, 07);  //Set Start Date
            SetEndDate(2013, 10, 11);    //Set End Date
            SetCash(100000);             //Set Strategy Cash

            // Find more symbols here: http://quantconnect.com/data
            // Forex, CFD, Equities Resolutions: Tick, Second, Minute, Hour, Daily.
            // Futures Resolution: Tick, Second, Minute
            // Options Resolution: Minute Only.

            // set algorithm framework models
            SetUniverseSelection(new ManualUniverseSelectionModel(QuantConnect.Symbol.Create("SPY", SecurityType.Equity, Market.USA)));
            SetAlpha(new ConstantAlphaModel(InsightType.Price, InsightDirection.Up, TimeSpan.FromMinutes(20), 0.025, null));

            // We can define who often the EWPCM will rebalance if no new insight is submitted using:
            // Resolution Enum:
            SetPortfolioConstruction(new EqualWeightingPortfolioConstructionModel(Resolution.Daily));
            // TimeSpan
            // SetPortfolioConstruction(new EqualWeightingPortfolioConstructionModel(TimeSpan.FromDays(2)));
            // A Func<DateTime, DateTime>. In this case, we can use the pre-defined func at Expiry helper class
            // SetPortfolioConstruction(new EqualWeightingPortfolioConstructionModel(Expiry.EndOfWeek));

            SetExecution(new ImmediateExecutionModel());
            SetRiskManagement(new MaximumDrawdownPercentPerSecurity(0.01m));
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
            {"Total Trades", "3"},
            {"Average Win", "0%"},
            {"Average Loss", "-1.01%"},
            {"Compounding Annual Return", "254.782%"},
            {"Drawdown", "2.200%"},
            {"Expectancy", "-1"},
            {"Net Profit", "1.632%"},
            {"Sharpe Ratio", "8.371"},
            {"Probabilistic Sharpe Ratio", "66.555%"},
            {"Loss Rate", "100%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "-0.088"},
            {"Beta", "1.006"},
            {"Annual Standard Deviation", "0.221"},
            {"Annual Variance", "0.049"},
            {"Information Ratio", "-32.586"},
            {"Tracking Error", "0.002"},
            {"Treynor Ratio", "1.839"},
            {"Total Fees", "$9.77"},
            {"Fitness Score", "0.747"},
            {"Kelly Criterion Estimate", "38.64"},
            {"Kelly Criterion Probability Value", "0.229"},
            {"Sortino Ratio", "79228162514264337593543950335"},
            {"Return Over Maximum Drawdown", "85.209"},
            {"Portfolio Turnover", "0.747"},
            {"Total Insights Generated", "100"},
            {"Total Insights Closed", "99"},
            {"Total Insights Analysis Completed", "99"},
            {"Long Insight Count", "100"},
            {"Short Insight Count", "0"},
            {"Long/Short Ratio", "100%"},
            {"Estimated Monthly Alpha Value", "$126657.6305"},
            {"Total Accumulated Estimated Alpha Value", "$20405.9516"},
            {"Mean Population Estimated Insight Value", "$206.1207"},
            {"Mean Population Direction", "54.5455%"},
            {"Mean Population Magnitude", "54.5455%"},
            {"Rolling Averaged Population Direction", "59.8056%"},
            {"Rolling Averaged Population Magnitude", "59.8056%"},
            {"OrderListHash", "17e29d58e5dabd93569da752c4552c70"}
        };
    }
}

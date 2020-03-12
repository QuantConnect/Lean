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

using QuantConnect.Algorithm.Framework.Alphas;
using QuantConnect.Algorithm.Framework.Execution;
using QuantConnect.Algorithm.Framework.Portfolio;
using QuantConnect.Algorithm.Framework.Selection;
using QuantConnect.Brokerages;
using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Orders;
using System;
using System.Collections.Generic;
using System.Linq;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Basic template framework algorithm uses framework components to define the algorithm.
    /// Shows EqualWeightingPortfolioConstructionModel.LongOnly() application
    /// </summary>
    /// <meta name="tag" content="alpha streams" />
    /// <meta name="tag" content="using quantconnect" />
    /// <meta name="tag" content="algorithm framework" />
    public class LongOnlyAlphaStreamAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        public override void Initialize()
        {
            // 1. Required: 
            SetStartDate(2013, 10, 07);
            SetEndDate(2013, 10, 11);

            // 2. Required: Alpha Streams Models:
            SetBrokerageModel(BrokerageName.AlphaStreams);

            // 3. Required: Significant AUM Capacity
            SetCash(1000000);

            // Only SPY will be traded
            SetPortfolioConstruction(new EqualWeightingPortfolioConstructionModel(Resolution.Daily, PortfolioBias.Long));
            SetExecution(new ImmediateExecutionModel());

            // set algorithm framework models
            SetUniverseSelection(
                new ManualUniverseSelectionModel(
                    new[] {"SPY", "IBM"}
                        .Select(x => QuantConnect.Symbol.Create(x, SecurityType.Equity, Market.USA))
                )
            );
        }

        public override void OnData(Slice slice)
        {
            if (Portfolio.Invested) return;

            EmitInsights(
                Insight.Price("SPY", TimeSpan.FromDays(1), InsightDirection.Up),
                Insight.Price("IBM", TimeSpan.FromDays(1), InsightDirection.Down)
            );
        }

        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            if (orderEvent.Status.IsFill())
            {
                if (Securities[orderEvent.Symbol].Holdings.IsShort)
                {
                    throw new Exception("Invalid position, should not be short");
                }
                Debug($"Purchased Stock: {orderEvent}");
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
            {"Average Win", "1.00%"},
            {"Average Loss", "-0.59%"},
            {"Compounding Annual Return", "220.252%"},
            {"Drawdown", "2.300%"},
            {"Expectancy", "0.343"},
            {"Net Profit", "1.499%"},
            {"Sharpe Ratio", "4.367"},
            {"Probabilistic Sharpe Ratio", "64.891%"},
            {"Loss Rate", "50%"},
            {"Win Rate", "50%"},
            {"Profit-Loss Ratio", "1.69"},
            {"Alpha", "-0.101"},
            {"Beta", "1.001"},
            {"Annual Standard Deviation", "0.22"},
            {"Annual Variance", "0.049"},
            {"Information Ratio", "-28.324"},
            {"Tracking Error", "0.004"},
            {"Treynor Ratio", "0.961"},
            {"Total Fees", "$293.12"},
            {"Fitness Score", "0.999"},
            {"Kelly Criterion Estimate", "-7.18"},
            {"Kelly Criterion Probability Value", "0.596"},
            {"Sortino Ratio", "79228162514264337593543950335"},
            {"Return Over Maximum Drawdown", "74.148"},
            {"Portfolio Turnover", "1.741"},
            {"Total Insights Generated", "10"},
            {"Total Insights Closed", "8"},
            {"Total Insights Analysis Completed", "8"},
            {"Long Insight Count", "5"},
            {"Short Insight Count", "5"},
            {"Long/Short Ratio", "100%"},
            {"Estimated Monthly Alpha Value", "$65693.4230"},
            {"Total Accumulated Estimated Alpha Value", "$10583.9404"},
            {"Mean Population Estimated Insight Value", "$1322.9925"},
            {"Mean Population Direction", "62.5%"},
            {"Mean Population Magnitude", "0%"},
            {"Rolling Averaged Population Direction", "73.0394%"},
            {"Rolling Averaged Population Magnitude", "0%"},
            {"OrderListHash", "1985687291"}
        };
    }
}

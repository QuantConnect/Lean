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

            // Order margin value has to have a minimum of 0.5% of Portfolio value, allows filtering out small trades and reduce fees.
            // Commented so regression algorithm is more sensitive
            //Settings.MinimumOrderMarginPortfolioPercentage = 0.005m;

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
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 7843;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "13"},
            {"Average Win", "0.99%"},
            {"Average Loss", "-0.20%"},
            {"Compounding Annual Return", "216.590%"},
            {"Drawdown", "2.300%"},
            {"Expectancy", "0.476"},
            {"Net Profit", "1.484%"},
            {"Sharpe Ratio", "7.296"},
            {"Probabilistic Sharpe Ratio", "64.952%"},
            {"Loss Rate", "75%"},
            {"Win Rate", "25%"},
            {"Profit-Loss Ratio", "4.91"},
            {"Alpha", "-0.36"},
            {"Beta", "1.003"},
            {"Annual Standard Deviation", "0.223"},
            {"Annual Variance", "0.05"},
            {"Information Ratio", "-100.202"},
            {"Tracking Error", "0.004"},
            {"Treynor Ratio", "1.624"},
            {"Total Fees", "$313.73"},
            {"Estimated Strategy Capacity", "$16000000.00"},
            {"Lowest Capacity Asset", "SPY R735QTJ8XC9X"},
            {"Return Over Maximum Drawdown", "68.689"},
            {"Portfolio Turnover", "1.741"},
            {"Total Insights Generated", "10"},
            {"Total Insights Closed", "8"},
            {"Total Insights Analysis Completed", "8"},
            {"Long Insight Count", "5"},
            {"Short Insight Count", "5"},
            {"Long/Short Ratio", "100%"},
            {"OrderListHash", "bdee04be3413e3cd32c3ce95bcc4994b"}
        };
    }
}

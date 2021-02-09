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
using QuantConnect.Algorithm.Framework.Portfolio;
using QuantConnect.Algorithm.Framework.Risk;
using QuantConnect.Algorithm.Framework.Selection;
using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Orders;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression test showcasing an algorithm using the framework models
    /// and directly calling <see cref="QCAlgorithm.EmitInsights"/>
    /// </summary>
    public class EmitInsightsAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private readonly Symbol _symbol = QuantConnect.Symbol.Create("SPY", SecurityType.Equity, Market.USA);
        private bool _toggle;

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            // Set requested data resolution
            UniverseSettings.Resolution = Resolution.Daily;

            SetStartDate(2013, 10, 07);  //Set Start Date
            SetEndDate(2013, 10, 11);    //Set End Date
            SetCash(100000);             //Set Strategy Cash

            // set algorithm framework models
            SetUniverseSelection(new ManualUniverseSelectionModel(_symbol));
            SetAlpha(new ConstantAlphaModel(InsightType.Price, InsightDirection.Up, TimeSpan.FromDays(1), 0.025, null));
            SetPortfolioConstruction(new EqualWeightingPortfolioConstructionModel());
            SetRiskManagement(new MaximumDrawdownPercentPerSecurity(0.01m));
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="data">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice data)
        {
            if (_toggle)
            {
                _toggle = false;
                var order = Transactions.GetOpenOrders(_symbol).FirstOrDefault();

                if (order != null)
                {
                    throw new Exception($"Unexpected open order {order}");
                }

                // we manually emit an insight
                EmitInsights(Insight.Price(_symbol, Resolution.Daily, 1, InsightDirection.Down));

                // emitted insight should have triggered a new order
                order = Transactions.GetOpenOrders(_symbol).FirstOrDefault();

                if (order == null)
                {
                    throw new Exception("Expected open order for emitted insight");
                }
                if (order.Direction != OrderDirection.Sell
                    || order.Symbol != _symbol)
                {
                    throw new Exception($"Unexpected open order for emitted insight: {order}");
                }
            }
            else
            {
                _toggle = true;
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
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "4"},
            {"Average Win", "0.96%"},
            {"Average Loss", "-0.95%"},
            {"Compounding Annual Return", "-44.357%"},
            {"Drawdown", "1.100%"},
            {"Expectancy", "0.005"},
            {"Net Profit", "-0.800%"},
            {"Sharpe Ratio", "-2.218"},
            {"Probabilistic Sharpe Ratio", "32.508%"},
            {"Loss Rate", "50%"},
            {"Win Rate", "50%"},
            {"Profit-Loss Ratio", "1.01"},
            {"Alpha", "-0.64"},
            {"Beta", "0.23"},
            {"Annual Standard Deviation", "0.147"},
            {"Annual Variance", "0.022"},
            {"Information Ratio", "-8.259"},
            {"Tracking Error", "0.204"},
            {"Treynor Ratio", "-1.424"},
            {"Total Fees", "$16.26"},
            {"Fitness Score", "0.003"},
            {"Kelly Criterion Estimate", "12.539"},
            {"Kelly Criterion Probability Value", "0.367"},
            {"Sortino Ratio", "-22.336"},
            {"Return Over Maximum Drawdown", "-38.722"},
            {"Portfolio Turnover", "1.003"},
            {"Total Insights Generated", "7"},
            {"Total Insights Closed", "4"},
            {"Total Insights Analysis Completed", "4"},
            {"Long Insight Count", "5"},
            {"Short Insight Count", "2"},
            {"Long/Short Ratio", "250.0%"},
            {"Estimated Monthly Alpha Value", "$19016880.2887"},
            {"Total Accumulated Estimated Alpha Value", "$3275129.3830"},
            {"Mean Population Estimated Insight Value", "$818782.3458"},
            {"Mean Population Direction", "50%"},
            {"Mean Population Magnitude", "50%"},
            {"Rolling Averaged Population Direction", "50%"},
            {"Rolling Averaged Population Magnitude", "50%"},
            {"OrderListHash", "4178a84209934b1eb6d03c2267654f32"}
        };
    }
}

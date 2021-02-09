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
using QuantConnect.Algorithm.Framework.Portfolio;
using QuantConnect.Algorithm.Framework.Selection;
using QuantConnect.Brokerages;
using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Securities;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm which reproduce GH issue 3784, where *default* <see cref="IAlgorithm.UniverseSettings"/>
    /// Leverage value took precedence over <see cref="IAlgorithm.BrokerageModel"/>
    /// </summary>
    public class LeveragePrecedenceRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _spy;

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2013, 10, 07);
            SetEndDate(2013, 10, 11);

            SetBrokerageModel(new TestBrokerageModel());

            _spy = QuantConnect.Symbol.Create("SPY", SecurityType.Equity, Market.USA);
            SetUniverseSelection(new ManualUniverseSelectionModel(_spy));
            SetAlpha(new ConstantAlphaModel(InsightType.Price, InsightDirection.Up, TimeSpan.FromMinutes(20), 0.025, null));
            SetPortfolioConstruction(new EqualWeightingPortfolioConstructionModel());
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="data">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice data)
        {
            if (!Portfolio.Invested)
            {
                SetHoldings(_spy, 10);
                Debug("Purchased Stock");
            }

            if (Securities[_spy].Leverage != 10)
            {
                throw new Exception($"Expecting leverage to be 10, was {Securities[_spy].Leverage}");
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
            {"Total Trades", "2"},
            {"Average Win", "0%"},
            {"Average Loss", "-0.11%"},
            {"Compounding Annual Return", "235.287%"},
            {"Drawdown", "2.200%"},
            {"Expectancy", "-1"},
            {"Net Profit", "1.559%"},
            {"Sharpe Ratio", "8.763"},
            {"Probabilistic Sharpe Ratio", "67.311%"},
            {"Loss Rate", "100%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "-0.001"},
            {"Beta", "0.999"},
            {"Annual Standard Deviation", "0.219"},
            {"Annual Variance", "0.048"},
            {"Information Ratio", "-13.944"},
            {"Tracking Error", "0"},
            {"Treynor Ratio", "1.925"},
            {"Total Fees", "$61.90"},
            {"Fitness Score", "0.979"},
            {"Kelly Criterion Estimate", "38.64"},
            {"Kelly Criterion Probability Value", "0.229"},
            {"Sortino Ratio", "7.47"},
            {"Return Over Maximum Drawdown", "71.186"},
            {"Portfolio Turnover", "4.74"},
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
            {"OrderListHash", "f3e7f74b397880a3fd4a494409a77012"}
        };

        private class TestBrokerageModel : DefaultBrokerageModel
        {
            public override decimal GetLeverage(Security security)
            {
                return 10;
            }
        }
    }
}

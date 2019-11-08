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
            {"Average Loss", "-0.06%"},
            {"Compounding Annual Return", "220.077%"},
            {"Drawdown", "2.200%"},
            {"Expectancy", "-1"},
            {"Net Profit", "1.606%"},
            {"Sharpe Ratio", "3.923"},
            {"Probabilistic Sharpe Ratio", "67.651%"},
            {"Loss Rate", "100%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0.572"},
            {"Beta", "0.311"},
            {"Annual Standard Deviation", "0.174"},
            {"Annual Variance", "0.03"},
            {"Information Ratio", "1.559"},
            {"Tracking Error", "0.209"},
            {"Treynor Ratio", "2.199"},
            {"Total Fees", "$61.90"}
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
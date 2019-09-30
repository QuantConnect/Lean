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
using QuantConnect.Algorithm.Framework.Execution;
using QuantConnect.Algorithm.Framework.Portfolio;
using QuantConnect.Algorithm.Framework.Selection;
using QuantConnect.Data;
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Test algorithm using <see cref="QCAlgorithm.AddAlphaModel(IAlphaModel)"/>
    /// </summary>
    public class AddAlphaModelAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _spy;
        private Symbol _fb;
        private Symbol _ibm;

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2013, 10, 07);  //Set Start Date
            SetEndDate(2013, 10, 11);    //Set End Date
            SetCash(100000);             //Set Strategy Cash

            UniverseSettings.Resolution = Resolution.Daily;

            _spy = QuantConnect.Symbol.Create("SPY", SecurityType.Equity, Market.USA);
            _fb = QuantConnect.Symbol.Create("FB", SecurityType.Equity, Market.USA);
            _ibm = QuantConnect.Symbol.Create("IBM", SecurityType.Equity, Market.USA);
            SetUniverseSelection(new ManualUniverseSelectionModel(_spy, _fb, _ibm));
            SetPortfolioConstruction(new EqualWeightingPortfolioConstructionModel());
            SetExecution(new ImmediateExecutionModel());

            AddAlpha(new OneTimeAlphaModel(_spy));
            AddAlpha(new OneTimeAlphaModel(_fb));
            AddAlpha(new OneTimeAlphaModel(_ibm));

            InsightsGenerated += OnInsightsGeneratedVerifier;
        }

        private void OnInsightsGeneratedVerifier(IAlgorithm algorithm,
            GeneratedInsightsCollection insightsCollection)
        {
            if (insightsCollection.Insights.Count(insight => insight.Symbol == _fb) != 1
                || insightsCollection.Insights.Count(insight => insight.Symbol == _spy) != 1
                || insightsCollection.Insights.Count(insight => insight.Symbol == _ibm) != 1)
            {
                throw new Exception("Unexpected insights were emitted");
            }
        }

        private class OneTimeAlphaModel : AlphaModel
        {
            private readonly Symbol _symbol;
            private bool _triggered;

            public OneTimeAlphaModel(Symbol symbol)
            {
                _symbol = symbol;
            }

            public override IEnumerable<Insight> Update(QCAlgorithm algorithm, Slice data)
            {
                if (!_triggered)
                {
                    _triggered = true;
                    yield return Insight.Price(
                        _symbol,
                        Resolution.Daily,
                        1,
                        InsightDirection.Down
                        );
                }
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
            {"Total Trades", "6"},
            {"Average Win", "0.91%"},
            {"Average Loss", "-0.23%"},
            {"Compounding Annual Return", "214.278%"},
            {"Drawdown", "1.600%"},
            {"Expectancy", "2.248"},
            {"Net Profit", "1.581%"},
            {"Sharpe Ratio", "2.803"},
            {"Loss Rate", "33%"},
            {"Win Rate", "67%"},
            {"Profit-Loss Ratio", "3.87"},
            {"Alpha", "1.037"},
            {"Beta", "-0.99"},
            {"Annual Standard Deviation", "0.244"},
            {"Annual Variance", "0.06"},
            {"Information Ratio", "0.805"},
            {"Tracking Error", "0.407"},
            {"Treynor Ratio", "-0.691"},
            {"Total Fees", "$10.88"},
            {"Total Insights Generated", "3"},
            {"Total Insights Closed", "3"},
            {"Total Insights Analysis Completed", "3"},
            {"Long Insight Count", "0"},
            {"Short Insight Count", "3"},
            {"Long/Short Ratio", "0%"},
            {"Estimated Monthly Alpha Value", "$13262182.1037"},
            {"Total Accumulated Estimated Alpha Value", "$2284042.4734"},
            {"Mean Population Estimated Insight Value", "$761347.4911"},
            {"Mean Population Direction", "100%"},
            {"Mean Population Magnitude", "0%"},
            {"Rolling Averaged Population Direction", "100%"},
            {"Rolling Averaged Population Magnitude", "0%"}
        };
    }
}

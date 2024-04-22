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
using QuantConnect.Algorithm.Framework.Selection;
using QuantConnect.Data;
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Test algorithm generating insights with custom tags
    /// </summary>
    public class InsightTagAlphaRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _spy;
        private Symbol _fb;
        private Symbol _ibm;

        private List<Symbol> _symbolsWithGeneratedInsights = new();

        public override void Initialize()
        {
            SetStartDate(2013, 10, 07);
            SetEndDate(2013, 10, 11);
            SetCash(100000);

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
            foreach (var insight in insightsCollection.Insights)
            {
                if (insight.Tag != OneTimeAlphaModel.GenerateInsightTag(insight.Symbol))
                {
                    throw new Exception("Unexpected insight tag was emitted");
                }

                _symbolsWithGeneratedInsights.Add(insight.Symbol);
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (_symbolsWithGeneratedInsights.Count != 3)
            {
                throw new Exception("Unexpected number of symbols with generated insights");
            }

            if (!_symbolsWithGeneratedInsights.Contains(_spy))
            {
                throw new Exception("SPY symbol was not found in symbols with generated insights");
            }

            if (!_symbolsWithGeneratedInsights.Contains(_fb))
            {
                throw new Exception("FB symbol was not found in symbols with generated insights");
            }

            if (!_symbolsWithGeneratedInsights.Contains(_ibm))
            {
                throw new Exception("IBM symbol was not found in symbols with generated insights");
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
                        InsightDirection.Down,
                        tag: GenerateInsightTag(_symbol)
                        );
                }
            }

            public static string GenerateInsightTag(Symbol symbol)
            {
                return $"Insight generated for {symbol}";
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
        public long DataPoints => 58;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "9"},
            {"Average Win", "0.86%"},
            {"Average Loss", "-0.27%"},
            {"Compounding Annual Return", "184.364%"},
            {"Drawdown", "1.700%"},
            {"Expectancy", "1.781"},
            {"Start Equity", "100000"},
            {"End Equity", "101441.92"},
            {"Net Profit", "1.442%"},
            {"Sharpe Ratio", "4.836"},
            {"Sortino Ratio", "10.481"},
            {"Probabilistic Sharpe Ratio", "59.497%"},
            {"Loss Rate", "33%"},
            {"Win Rate", "67%"},
            {"Profit-Loss Ratio", "3.17"},
            {"Alpha", "4.164"},
            {"Beta", "-1.322"},
            {"Annual Standard Deviation", "0.321"},
            {"Annual Variance", "0.103"},
            {"Information Ratio", "-0.795"},
            {"Tracking Error", "0.532"},
            {"Treynor Ratio", "-1.174"},
            {"Total Fees", "$14.78"},
            {"Estimated Strategy Capacity", "$47000000.00"},
            {"Lowest Capacity Asset", "IBM R735QTJ8XC9X"},
            {"Portfolio Turnover", "41.18%"},
            {"OrderListHash", "d929e7959f079ad4fed42e8f3b35e39e"}
        };
    }
}

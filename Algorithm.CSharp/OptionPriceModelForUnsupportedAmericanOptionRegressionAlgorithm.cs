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
using System.Linq;
using System.Collections.Generic;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;
using QuantConnect.Securities.Option;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm excersizing an equity covered American style option, using an option price model that does not support American style options and asserting that the option price model is not used.
    /// </summary>
    public class OptionPriceModelForUnsupportedAmericanOptionRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private bool _showGreeks = false;
        private bool _triedGreeksCalculation = false;
        private Symbol _optionSymbol;

        public override void Initialize()
        {
            SetStartDate(2015, 12, 23);
            SetEndDate(2015, 12, 24);
            SetCash(100000);

            var equity = AddEquity("GOOG", Resolution.Minute);
            equity.SetDataNormalizationMode(DataNormalizationMode.Raw);
            var option = AddOption("GOOG", Resolution.Minute);
            // BlackSholes model does not support American style options
            option.PriceModel = OptionPriceModels.BlackScholes();
            _optionSymbol = option.Symbol;

            SetWarmUp(TimeSpan.FromDays(10));

            _showGreeks = true;
            _triedGreeksCalculation = false;
        }
        public override void OnData(Slice slice)
        {
            if (IsWarmingUp)
            {
                return;
            }

            foreach (var kvp in slice.OptionChains)
            {
                if (kvp.Key != _optionSymbol)
                {
                    continue;
                }

                var chain = kvp.Value;
                var contracts = chain.Where(x => x.Right == OptionRight.Call);

                if (!contracts.Any())
                {
                    return;
                }

                if (_showGreeks)
                {
                    _showGreeks = false;
                    _triedGreeksCalculation = true;

                    foreach (var contract in contracts)
                    {
                        Greeks greeks;
                        try
                        {
                            greeks = contract.Greeks;
                            throw new Exception($"Expected greeks not to be calculated for {contract.Symbol.Value}, an American style option, using an option price model that does not support them, but they were");
                        }
                        catch (ArgumentException)
                        {
                            // Expected
                        }
                    }
                }
            }
        }

        public override void OnEndOfDay(Symbol symbol)
        {
            _showGreeks = true;
        }

        public override void OnEndOfAlgorithm()
        {
            if (!_triedGreeksCalculation)
            {
                throw new Exception("Expected greeks to be calculated");
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
        public long DataPoints => 910666;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "0"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "0%"},
            {"Drawdown", "0%"},
            {"Expectancy", "0"},
            {"Net Profit", "0%"},
            {"Sharpe Ratio", "0"},
            {"Probabilistic Sharpe Ratio", "0%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0"},
            {"Beta", "0"},
            {"Annual Standard Deviation", "0"},
            {"Annual Variance", "0"},
            {"Information Ratio", "0"},
            {"Tracking Error", "0"},
            {"Treynor Ratio", "0"},
            {"Total Fees", "$0.00"},
            {"Estimated Strategy Capacity", "$0"},
            {"Lowest Capacity Asset", ""},
            {"Fitness Score", "0"},
            {"Kelly Criterion Estimate", "0"},
            {"Kelly Criterion Probability Value", "0"},
            {"Sortino Ratio", "79228162514264337593543950335"},
            {"Return Over Maximum Drawdown", "79228162514264337593543950335"},
            {"Portfolio Turnover", "0"},
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
            {"OrderListHash", "d41d8cd98f00b204e9800998ecf8427e"}
        };
    }
}

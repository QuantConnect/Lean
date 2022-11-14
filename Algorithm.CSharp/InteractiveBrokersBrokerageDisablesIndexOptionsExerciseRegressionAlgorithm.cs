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
using QuantConnect.Brokerages;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;
using QuantConnect.Orders;
using QuantConnect.Securities.Option;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm asserting that InteractiveBrokers brokerage model does not support index options exercise
    /// </summary>
    public class InteractiveBrokersBrokerageDisablesIndexOptionsExerciseRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Option _option;

        private OptionContract _contract;

        private bool _marketOrderDone;

        private bool _triedExercise;

        public override void Initialize()
        {
            SetStartDate(2021, 1, 4);
            SetEndDate(2021, 2, 1);
            SetCash(200000);

            SetBrokerageModel(new InteractiveBrokersBrokerageModel());

            var index = AddIndex("SPX", Resolution.Hour, fillDataForward: true);
            var indexOption = AddIndexOption(index.Symbol, Resolution.Hour, fillDataForward: true);
            indexOption.SetFilter(filterFunc => filterFunc);

            _option = indexOption;
        }

        public override void OnData(Slice slice)
        {
            if (_triedExercise || !_option.Exchange.ExchangeOpen)
            {
                return;
            }

            if (_contract == null)
            {
                OptionChain contracts;
                if (!slice.OptionChains.TryGetValue(_option.Symbol, out contracts) || !contracts.Any())
                {
                    return;
                }

                _contract = contracts.First();
            }

            var expiry = _contract.Expiry.ConvertToUtc(_option.Exchange.TimeZone).Date;

            if (UtcTime.Date < expiry && !_marketOrderDone)
            {
                if (MarketOrder(_contract.Symbol, 1).Status != OrderStatus.Filled)
                {
                    throw new Exception("Expected market order to fill immediately");
                }

                _marketOrderDone = true;

                return;
            }

            if (!_triedExercise && UtcTime.Date == expiry)
            {
                if (ExerciseOption(_contract.Symbol, 1).Status == OrderStatus.Filled)
                {
                    throw new Exception($"Expected index option to not be exercisable on its expiration date. " +
                                        $"Time: {UtcTime}. Expiry: {_contract.Expiry.ConvertToUtc(_option.Exchange.TimeZone)}");
                }

                _triedExercise = true;

                // We already tested everything, so we can stop the algorithm
                Quit();
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (!_triedExercise)
            {
                throw new Exception("Expected to try to exercise index option before and on expiry");
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
        /// Data Points count of all time slices of algorithm
        /// </summary>
        public long DataPoints => 1757;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "1"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "32.097%"},
            {"Drawdown", "1.900%"},
            {"Expectancy", "0"},
            {"Net Profit", "0.874%"},
            {"Sharpe Ratio", "6.487"},
            {"Probabilistic Sharpe Ratio", "94.319%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0"},
            {"Beta", "0"},
            {"Annual Standard Deviation", "0.122"},
            {"Annual Variance", "0.015"},
            {"Information Ratio", "6.487"},
            {"Tracking Error", "0.122"},
            {"Treynor Ratio", "0"},
            {"Total Fees", "$1.00"},
            {"Estimated Strategy Capacity", "$24000000.00"},
            {"Lowest Capacity Asset", "SPX XL80P3HB5O6M|SPX 31"},
            {"Fitness Score", "0.004"},
            {"Kelly Criterion Estimate", "0"},
            {"Kelly Criterion Probability Value", "0"},
            {"Sortino Ratio", "79228162514264337593543950335"},
            {"Return Over Maximum Drawdown", "20.486"},
            {"Portfolio Turnover", "0.004"},
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
            {"OrderListHash", "acabeaf66c28456d5d3375d80a574b2d"}
        };
    }
}

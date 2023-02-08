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

using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;
using QuantConnect.Orders;
using QuantConnect.Securities.Option;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm asserting that European options cannot be exercised before expiry
    /// </summary>
    public class EuropeanOptionsCannotBeExercisedBeforeExpiryRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Option _option;

        private OptionContract _contract;

        private bool _marketOrderDone;

        private bool _exerciseBeforeExpiryDone;

        private bool _exerciseOnExpiryDone;

        public override void Initialize()
        {
            SetStartDate(2021, 1, 4);
            SetEndDate(2021, 2, 1);
            SetCash(200000);

            var index = AddIndex("SPX", Resolution.Hour, fillDataForward: true);
            var indexOption = AddIndexOption(index.Symbol, Resolution.Hour, fillDataForward: true);
            indexOption.SetFilter(filterFunc => filterFunc);

            _option = indexOption;
        }

        public override void OnData(Slice slice)
        {
            if ((_exerciseBeforeExpiryDone && _exerciseOnExpiryDone) || !_option.Exchange.ExchangeOpen)
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

            if (!_exerciseBeforeExpiryDone && UtcTime.Date < expiry)
            {
                if (!_marketOrderDone)
                {
                    if (MarketOrder(_contract.Symbol, 1).Status != OrderStatus.Filled)
                    {
                        throw new Exception("Expected market order to fill immediately");
                    }

                    _marketOrderDone = true;
                }

                if (ExerciseOption(_contract.Symbol, 1).Status == OrderStatus.Filled)
                {
                    throw new Exception($"Expected European option to not be exercisable before its expiration date. " +
                                        $"Time: {UtcTime}. Expiry: {_contract.Expiry.ConvertToUtc(_option.Exchange.TimeZone)}");
                }

                _exerciseBeforeExpiryDone = true;

                return;
            }

            if (!_exerciseOnExpiryDone && UtcTime.Date == expiry)
            {
                if (ExerciseOption(_contract.Symbol, 1).Status != OrderStatus.Filled)
                {
                    throw new Exception($"Expected European option to be exercisable on its expiration date. " +
                                        $"Time: {UtcTime}. Expiry: {_contract.Expiry.ConvertToUtc(_option.Exchange.TimeZone)}");
                }

                _exerciseOnExpiryDone = true;

                // We already tested everything, so we can stop the algorithm
                Quit();
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (!_exerciseBeforeExpiryDone || !_exerciseOnExpiryDone)
            {
                throw new Exception("Expected to try to exercise option before and on expiry");
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
        public long DataPoints => 1828;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "2"},
            {"Average Win", "0%"},
            {"Average Loss", "-4.10%"},
            {"Compounding Annual Return", "24.075%"},
            {"Drawdown", "1.900%"},
            {"Expectancy", "-1"},
            {"Net Profit", "0.677%"},
            {"Sharpe Ratio", "5.78"},
            {"Probabilistic Sharpe Ratio", "89.644%"},
            {"Loss Rate", "100%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0.95"},
            {"Beta", "-0.354"},
            {"Annual Standard Deviation", "0.123"},
            {"Annual Variance", "0.015"},
            {"Information Ratio", "0.211"},
            {"Tracking Error", "0.176"},
            {"Treynor Ratio", "-2.011"},
            {"Total Fees", "$0.00"},
            {"Estimated Strategy Capacity", "$1700000.00"},
            {"Lowest Capacity Asset", "SPX XL80P3HB5O6M|SPX 31"},
            {"Fitness Score", "0.004"},
            {"Kelly Criterion Estimate", "0"},
            {"Kelly Criterion Probability Value", "0"},
            {"Sortino Ratio", "79228162514264337593543950335"},
            {"Return Over Maximum Drawdown", "20.506"},
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
            {"OrderListHash", "1d50dbdf8503efefc85935040acdd226"}
        };
    }
}

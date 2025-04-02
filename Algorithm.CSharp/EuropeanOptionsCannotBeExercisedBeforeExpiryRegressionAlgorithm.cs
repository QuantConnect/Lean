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

            var index = AddIndex("SPX", Resolution.Hour, fillForward: true);
            var indexOption = AddIndexOption(index.Symbol, Resolution.Hour, fillForward: true);
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
                        throw new RegressionTestException("Expected market order to fill immediately");
                    }

                    _marketOrderDone = true;
                }

                if (ExerciseOption(_contract.Symbol, 1).Status == OrderStatus.Filled)
                {
                    throw new RegressionTestException($"Expected European option to not be exercisable before its expiration date. " +
                                        $"Time: {UtcTime}. Expiry: {_contract.Expiry.ConvertToUtc(_option.Exchange.TimeZone)}");
                }

                _exerciseBeforeExpiryDone = true;

                return;
            }

            if (!_exerciseOnExpiryDone && UtcTime.Date == expiry)
            {
                if (ExerciseOption(_contract.Symbol, 1).Status != OrderStatus.Filled)
                {
                    throw new RegressionTestException($"Expected European option to be exercisable on its expiration date. " +
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
                throw new RegressionTestException("Expected to try to exercise option before and on expiry");
            }

            var optionHoldings = Securities[_contract.Symbol].Holdings;
            if (optionHoldings.NetProfit != Portfolio.TotalNetProfit)
            {
                throw new RegressionTestException($"Unexpected holdings profit result {optionHoldings.Profit}");
            }
            if (Portfolio.Cash != (Portfolio.TotalNetProfit + 200000))
            {
                throw new RegressionTestException($"Unexpected portfolio cash {Portfolio.Cash}");
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public List<Language> Languages { get; } = new() { Language.CSharp };

        /// <summary>
        /// Data Points count of all time slices of algorithm
        /// </summary>
        public long DataPoints => 1461;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// Final status of the algorithm
        /// </summary>
        public AlgorithmStatus AlgorithmStatus => AlgorithmStatus.Completed;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "2"},
            {"Average Win", "0.68%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "24.075%"},
            {"Drawdown", "1.900%"},
            {"Expectancy", "0"},
            {"Start Equity", "200000"},
            {"End Equity", "201354"},
            {"Net Profit", "0.677%"},
            {"Sharpe Ratio", "5.76"},
            {"Sortino Ratio", "0"},
            {"Probabilistic Sharpe Ratio", "89.644%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "100%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0.946"},
            {"Beta", "-0.354"},
            {"Annual Standard Deviation", "0.123"},
            {"Annual Variance", "0.015"},
            {"Information Ratio", "0.211"},
            {"Tracking Error", "0.176"},
            {"Treynor Ratio", "-2.004"},
            {"Total Fees", "$0.00"},
            {"Estimated Strategy Capacity", "$1700000.00"},
            {"Lowest Capacity Asset", "SPX XL80P3HB5O6M|SPX 31"},
            {"Portfolio Turnover", "0.35%"},
            {"OrderListHash", "c511179c15aa167365cc1acb91b20bf3"}
        };
    }
}

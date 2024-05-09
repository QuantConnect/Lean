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

        private bool _automaticallyExercised;

        private decimal _initialCash = 200000;

        public override void Initialize()
        {
            SetStartDate(2021, 1, 4);
            SetEndDate(2021, 1, 30);
            SetCash(_initialCash);

            SetBrokerageModel(new InteractiveBrokersBrokerageModel());

            var index = AddIndex("SPX", Resolution.Hour, fillForward: true);
            var indexOption = AddIndexOption(index.Symbol, Resolution.Hour, fillForward: true);
            indexOption.SetFilter(filterFunc => filterFunc.CallsOnly());

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
            }
        }

        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            // The manual exercise failed and we are not placing any other orders, so this is the automatic exercise
            if (orderEvent.Status == OrderStatus.Filled &&
                _marketOrderDone &&
                _triedExercise &&
                UtcTime.Date >= _contract.Expiry.ConvertToUtc(_option.Exchange.TimeZone).Date)
            {
                var profit = Portfolio.TotalPortfolioValue - _initialCash;
                if (profit < 0)
                {
                    throw new Exception($"Expected profit to be positive. Actual: {profit}");
                }

                _automaticallyExercised = true;
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (!_triedExercise)
            {
                throw new Exception("Expected to try to exercise index option before and on expiry");
            }

            if (!_automaticallyExercised || Portfolio.Cash <= _initialCash)
            {
                throw new Exception("Expected index option to have ben automatically exercised on expiry and to have received cash");
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
        public long DataPoints => 1960;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "3"},
            {"Average Win", "0%"},
            {"Average Loss", "-4.10%"},
            {"Compounding Annual Return", "10.046%"},
            {"Drawdown", "1.900%"},
            {"Expectancy", "0"},
            {"Start Equity", "200000"},
            {"End Equity", "201353"},
            {"Net Profit", "0.676%"},
            {"Sharpe Ratio", "3.253"},
            {"Sortino Ratio", "0"},
            {"Probabilistic Sharpe Ratio", "86.292%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "100%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0"},
            {"Beta", "0"},
            {"Annual Standard Deviation", "0.081"},
            {"Annual Variance", "0.007"},
            {"Information Ratio", "3.284"},
            {"Tracking Error", "0.081"},
            {"Treynor Ratio", "0"},
            {"Total Fees", "$1.00"},
            {"Estimated Strategy Capacity", "$1700000.00"},
            {"Lowest Capacity Asset", "SPX XL80P3HB5O6M|SPX 31"},
            {"Portfolio Turnover", "0.16%"},
            {"OrderListHash", "d0ff308d240d80eb3774f0307a64ac7e"}
        };
    }
}

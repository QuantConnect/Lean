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

using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Util;
using System;
using System.Collections.Generic;
using System.Linq;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm to test we can get and trade option contracts for NQX index option
    /// </summary>
    public class IndexOptionScaledStrikeRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _nqx;
        private HashSet<int> _orderIds = new HashSet<int>();
        private DateTime _expiration = new DateTime(2021, 3, 19);
        private const decimal _initialCash = 100000m;

        public override void Initialize()
        {
            SetStartDate(2021, 3, 18);
            SetEndDate(2021, 3, 23);
            SetCash(_initialCash);
            UniverseSettings.Resolution = Resolution.Hour;

            var index = AddIndex("NDX", Resolution.Hour).Symbol;
            var option = AddIndexOption(index, "NQX", Resolution.Hour);
            option.SetFilter(universe => universe.IncludeWeeklys().Strikes(-1, 1).Expiration(0, 5));

            _nqx = option.Symbol;
        }

        public override void OnData(Slice slice)
        {
            var weekly_chain = slice.OptionChains.get(_nqx);

            if (!weekly_chain.IsNullOrEmpty() && !Portfolio.Invested)
            {
                foreach (var contract in weekly_chain.Where(x => x.Symbol.ID.Date == _expiration))
                {
                    var ticket = MarketOrder(contract.Symbol, 1);
                    _orderIds.Add(ticket.OrderId);
                }
            }
        }

        public override void OnEndOfAlgorithm()
        {
            var exerciseOrders = Transactions.GetOrders().Where(x => !_orderIds.Contains(x.Id));
            if (!exerciseOrders.Where(x => x.Tag.Contains("OTM")).Any())
            {
                throw new RegressionTestException($"At least one order should have been exercised OTM");
            }

            if (!exerciseOrders.Where(x => !x.Tag.Contains("OTM")).Any())
            {
                throw new RegressionTestException($"At least one order should have been exercised ITM");
            }

            if (Portfolio.TotalPortfolioValue <= _initialCash)
            {
                throw new RegressionTestException($"Since one order was expected to be exercised ITM, Total Portfolio Value was expected to be higher than {_initialCash}, but was {Portfolio.TotalPortfolioValue}");
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public virtual List<Language> Languages { get; } = new() { Language.CSharp };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 104;

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
            {"Total Orders", "4"},
            {"Average Win", "0%"},
            {"Average Loss", "-20.04%"},
            {"Compounding Annual Return", "79228162514264337593543950335%"},
            {"Drawdown", "2.100%"},
            {"Expectancy", "-0.5"},
            {"Start Equity", "100000"},
            {"End Equity", "274018.3"},
            {"Net Profit", "174.018%"},
            {"Sharpe Ratio", "6.74816637965336E+27"},
            {"Sortino Ratio", "0"},
            {"Probabilistic Sharpe Ratio", "95.428%"},
            {"Loss Rate", "50%"},
            {"Win Rate", "50%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "7.922816251426434E+28"},
            {"Beta", "4.566"},
            {"Annual Standard Deviation", "11.741"},
            {"Annual Variance", "137.844"},
            {"Information Ratio", "6.749778840887739E+27"},
            {"Tracking Error", "11.738"},
            {"Treynor Ratio", "1.7351225556608623E+28"},
            {"Total Fees", "$0.00"},
            {"Estimated Strategy Capacity", "$7000.00"},
            {"Lowest Capacity Asset", "NQX 31M220FF62ZSE|NDX 31"},
            {"Portfolio Turnover", "6.40%"},
            {"OrderListHash", "ec6881b180c68e6c7a48f6596c73e83d"}
        };
    }
}

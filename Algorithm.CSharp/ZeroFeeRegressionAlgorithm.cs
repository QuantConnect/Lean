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
using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Orders.Fees;
using QuantConnect.Securities;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression test algorithm where custom a <see cref="FeeModel"/> returns <see cref="OrderFee.Zero"/>
    /// </summary>
    public class ZeroFeeRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Security _security;
        // Adding this so we only trade once, so math is easier and clear
        private bool _alreadyTraded;

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2013, 10, 07);  //Set Start Date
            SetEndDate(2013, 10, 11);    //Set End Date
            SetCash(100000);             //Set Strategy Cash

            _security = AddEquity("SPY", Resolution.Minute);
            _security.FeeModel = new ZeroFeeModel();
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="data">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice slice)
        {
            if (!Portfolio.Invested && !_alreadyTraded)
            {
                _alreadyTraded = true;
                SetHoldings(_security.Symbol, 1);
                Debug("Purchased Stock");
            }
            else
            {
                Liquidate(_security.Symbol);
            }
        }

        public override void OnEndOfAlgorithm()
        {
            Log($"TotalPortfolioValue: {Portfolio.TotalPortfolioValue}");
            Log($"CashBook: {Portfolio.CashBook}");
            Log($"Holdings.TotalCloseProfit: {_security.Holdings.TotalCloseProfit()}");

            if (Portfolio.CashBook["USD"].Amount - _security.Holdings.LastTradeProfit != 100000)
            {
                throw new RegressionTestException("Unexpected USD cash amount: " +
                    $"{Portfolio.CashBook["USD"].Amount}");
            }
            if (Portfolio.CashBook.ContainsKey(Currencies.NullCurrency))
            {
                throw new RegressionTestException("Unexpected NullCurrency cash");
            }

            var closedTrade = TradeBuilder.ClosedTrades[0];
            if (closedTrade.TotalFees != 0)
            {
                throw new RegressionTestException($"Unexpected closed trades total fees {closedTrade.TotalFees}");
            }
            if (_security.Holdings.TotalFees != 0)
            {
                throw new RegressionTestException($"Unexpected closed trades total fees {closedTrade.TotalFees}");
            }
        }

        internal class ZeroFeeModel : FeeModel
        {
            public override OrderFee GetOrderFee(OrderFeeParameters parameters)
            {
                return OrderFee.Zero;
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
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 3943;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 10;

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
            {"Average Win", "0%"},
            {"Average Loss", "-0.05%"},
            {"Compounding Annual Return", "-3.660%"},
            {"Drawdown", "0.000%"},
            {"Expectancy", "-1"},
            {"Start Equity", "100000"},
            {"End Equity", "99952.34"},
            {"Net Profit", "-0.048%"},
            {"Sharpe Ratio", "0"},
            {"Sortino Ratio", "0"},
            {"Probabilistic Sharpe Ratio", "0%"},
            {"Loss Rate", "100%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0"},
            {"Beta", "0"},
            {"Annual Standard Deviation", "0"},
            {"Annual Variance", "0"},
            {"Information Ratio", "-8.91"},
            {"Tracking Error", "0.223"},
            {"Treynor Ratio", "0"},
            {"Total Fees", "$0.00"},
            {"Estimated Strategy Capacity", "$18000000.00"},
            {"Lowest Capacity Asset", "SPY R735QTJ8XC9X"},
            {"Portfolio Turnover", "39.91%"},
            {"Drawdown Recovery", "0"},
            {"OrderListHash", "5bd6d98c36a3344f7383557bc375cf83"}
        };
    }
}

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

using System.Collections.Generic;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;
using QuantConnect.Orders;
using QuantConnect.Orders.Slippage;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm asserting that market on open orders using daily data are filled at the bar open
    /// with the slippage referenced to that same open price, not to the bar close which is not known at the open.
    /// See GH issue 9753
    /// </summary>
    public class MarketOnOpenOrderSlippageRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private const decimal SlippagePercent = 0.01m;
        private Symbol _symbol;
        private int _fills;

        public override void Initialize()
        {
            SetStartDate(2013, 10, 07);
            SetEndDate(2013, 10, 11);
            SetCash(100000);

            var security = AddEquity("SPY", Resolution.Daily);
            security.SetSlippageModel(new ConstantSlippageModel(SlippagePercent));
            _symbol = security.Symbol;
        }

        public override void OnData(Slice slice)
        {
            if (!slice.Bars.ContainsKey(_symbol) || Transactions.GetOpenOrders(_symbol).Count > 0)
            {
                return;
            }

            // alternate buys and sells so both directions are checked
            MarketOnOpenOrder(_symbol, Portfolio[_symbol].Invested ? -100 : 100);
        }

        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            if (orderEvent.Status != OrderStatus.Filled)
            {
                return;
            }

            // the fill happens when the daily bar arrives, so this is the bar the order was filled with
            var bar = Securities[_symbol].Cache.GetData<TradeBar>();
            if (bar.Open == bar.Close)
            {
                throw new RegressionTestException($"Expected the fill bar open and close to differ so the slippage reference can be asserted: {bar}");
            }

            var slippage = bar.Open * SlippagePercent;
            var expectedFillPrice = orderEvent.Direction == OrderDirection.Buy ? bar.Open + slippage : bar.Open - slippage;
            if (orderEvent.FillPrice != expectedFillPrice)
            {
                throw new RegressionTestException($"Expected {orderEvent.Direction} fill price {expectedFillPrice} (open {bar.Open} +/- {SlippagePercent:P} slippage) but was {orderEvent.FillPrice}. Bar: {bar}");
            }

            _fills++;
        }

        public override void OnEndOfAlgorithm()
        {
            if (_fills < 2)
            {
                throw new RegressionTestException($"Expected at least a buy and a sell fill but got {_fills}");
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
        public long DataPoints => 48;

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
            {"Total Orders", "5"},
            {"Average Win", "0%"},
            {"Average Loss", "-0.29%"},
            {"Compounding Annual Return", "-36.910%"},
            {"Drawdown", "0.600%"},
            {"Expectancy", "-1"},
            {"Start Equity", "100000"},
            {"End Equity", "99412.82"},
            {"Net Profit", "-0.587%"},
            {"Sharpe Ratio", "-14.31"},
            {"Sortino Ratio", "-19.441"},
            {"Probabilistic Sharpe Ratio", "1.568%"},
            {"Loss Rate", "100%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "-0.502"},
            {"Beta", "0.093"},
            {"Annual Standard Deviation", "0.022"},
            {"Annual Variance", "0"},
            {"Information Ratio", "-11.354"},
            {"Tracking Error", "0.202"},
            {"Treynor Ratio", "-3.402"},
            {"Total Fees", "$4.00"},
            {"Estimated Strategy Capacity", "$1300000000.00"},
            {"Lowest Capacity Asset", "SPY R735QTJ8XC9X"},
            {"Portfolio Turnover", "11.63%"},
            {"Drawdown Recovery", "0"},
            {"OrderListHash", "4bbdfd7aaf0f2e4fa6cc9226fbf3d9e8"}
        };
    }
}

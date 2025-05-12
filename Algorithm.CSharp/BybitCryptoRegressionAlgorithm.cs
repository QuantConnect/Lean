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
using QuantConnect.Brokerages;
using QuantConnect.Indicators;
using QuantConnect.Orders;
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Algorithm demonstrating and ensuring that Bybit crypto brokerage model works as expected
    /// </summary>
    public class BybitCryptoRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _btcUsdt;

        private ExponentialMovingAverage _fast;
        private ExponentialMovingAverage _slow;

        private bool _liquidated;

        public override void Initialize()
        {
            SetStartDate(2022, 12, 13);
            SetEndDate(2022, 12, 13);

            // Set account currency (USDT)
            SetAccountCurrency("USDT");

            // Set strategy cash (USD)
            SetCash(100000);

            // Add some coin as initial holdings
            // When connected to a real brokerage, the amount specified in SetCash
            // will be replaced with the amount in your actual account.
            SetCash("BTC", 1m);

            SetBrokerageModel(BrokerageName.Bybit, AccountType.Cash);

            _btcUsdt = AddCrypto("BTCUSDT").Symbol;

            // create two moving averages
            _fast = EMA(_btcUsdt, 30, Resolution.Minute);
            _slow = EMA(_btcUsdt, 60, Resolution.Minute);
        }

        public override void OnData(Slice slice)
        {
            if (Portfolio.CashBook["USDT"].ConversionRate == 0 || Portfolio.CashBook["BTC"].ConversionRate == 0)
            {
                Log($"USDT conversion rate: {Portfolio.CashBook["USDT"].ConversionRate}");
                Log($"BTC conversion rate: {Portfolio.CashBook["BTC"].ConversionRate}");

                throw new RegressionTestException("Conversion rate is 0");
            }

            if (!_slow.IsReady)
            {
                return;
            }


            var btcAmount = Portfolio.CashBook["BTC"].Amount;
            if (_fast > _slow)
            {
                if (btcAmount == 1m && !_liquidated)
                {
                    Buy(_btcUsdt, 1);
                }
            }
            else
            {
                if (btcAmount > 1m)
                {
                    Liquidate(_btcUsdt);
                    _liquidated = true;
                }
                else if (btcAmount > 0 && _liquidated && Transactions.GetOpenOrders().Count == 0)
                {
                    // Place a limit order to sell our initial BTC holdings at 1% above the current price
                    var limitPrice = Math.Round(Securities[_btcUsdt].Price * 1.01m, 2);
                    LimitOrder(_btcUsdt, -btcAmount, limitPrice);
                }
            }
        }

        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            Debug(Time + " " + orderEvent);
        }

        public override void OnEndOfAlgorithm()
        {
            Log($"{Time} - TotalPortfolioValue: {Portfolio.TotalPortfolioValue}");
            Log($"{Time} - CashBook: {Portfolio.CashBook}");

            var btcAmount = Portfolio.CashBook["BTC"].Amount;
            if (btcAmount > 0)
            {
                throw new RegressionTestException($"BTC holdings should be zero at the end of the algorithm, but was {btcAmount}");
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public List<Language> Languages { get; } = new() { Language.CSharp, Language.Python };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 2883;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 60;

        /// <summary>
        /// Final status of the algorithm
        /// </summary>
        public AlgorithmStatus AlgorithmStatus => AlgorithmStatus.Completed;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "3"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "0%"},
            {"Drawdown", "0%"},
            {"Expectancy", "0"},
            {"Start Equity", "117171.12"},
            {"End Equity", "117244.52"},
            {"Net Profit", "0%"},
            {"Sharpe Ratio", "0"},
            {"Sortino Ratio", "0"},
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
            {"Total Fees", "₮51.65"},
            {"Estimated Strategy Capacity", "₮560000.00"},
            {"Lowest Capacity Asset", "BTCUSDT 2UZ"},
            {"Portfolio Turnover", "44.04%"},
            {"OrderListHash", "47580e88a8cc54b04f3b2bcb5d501150"}
        };
    }
}

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

using QuantConnect.Brokerages;
using System.Collections.Generic;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Binance cash account regression algorithm, reproduces issue https://github.com/QuantConnect/Lean/issues/6123
    /// </summary>
    public class BinanceCashAccountFeeRegressionAlgorithm : CryptoBaseCurrencyFeeRegressionAlgorithm
    {
        /// <summary>
        /// The target account type
        /// </summary>
        protected override AccountType AccountType { get; } = AccountType.Cash;

        public override void Initialize()
        {
            SetAccountCurrency("USDT");
            SetStartDate(2018, 05, 02);
            SetEndDate(2018, 05, 03);
            BrokerageName = BrokerageName.Binance;
            Pair = "BTCUSDT";
            base.Initialize();
        }

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public override long DataPoints => 50;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public override int AlgorithmHistoryDataPoints => 28;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public override Dictionary<string, string> ExpectedStatistics => new()
        {
            {"Total Orders", "49"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "0%"},
            {"Drawdown", "0%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000.00"},
            {"End Equity", "99986.57"},
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
            {"Total Fees", "₮45.62"},
            {"Estimated Strategy Capacity", "₮220000.00"},
            {"Lowest Capacity Asset", "BTCUSDT 18N"},
            {"Portfolio Turnover", "22.80%"},
            {"OrderListHash", "80711e4c1e3c0da20152da8fafc3fd66"}
        };
    }
}

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
using QuantConnect.Data;
using QuantConnect.Brokerages;
using QuantConnect.Interfaces;
using System.Collections.Generic;
using QuantConnect.Securities.Crypto;

namespace QuantConnect.Algorithm.CSharp
{
    public class CoinbaseCryptoYearMarketTradingRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        /// <summary>
        /// The Average amount of day in year
        /// </summary>
        /// <remarks>Regardless of calendar</remarks>
        private const int _daysInYear = 365;

        /// <summary>
        /// Flag prevents same order <see cref="Orders.OrderDirection"/>
        /// </summary>
        private bool _isBuy;

        /// <summary>
        /// Trading security
        /// </summary>
        private Crypto _BTCUSD;

        /// <summary>
        /// Initialize the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        /// <remarks>In fact, you can assign custom value for <see cref="IAlgorithmSettings.TradingDaysPerYear"/></remarks>
        public override void Initialize()
        {
            SetStartDate(2015, 01, 13);
            SetEndDate(2016, 02, 03);

            SetCash(100000);

            // Setup brokerage for current algorithm
            SetBrokerageModel(BrokerageName.Coinbase, AccountType.Cash);

            _BTCUSD = AddCrypto("BTCUSD", Resolution.Daily, Market.Coinbase);
        }

        /// <summary>
        /// Data Event Handler: receiving all subscription data in a single event
        /// </summary>
        /// <param name="slice">The current slice of data keyed by symbol string</param>
        public override void OnData(Slice slice)
        {
            if (!_isBuy)
            {
                MarketOrder(_BTCUSD, 1);
                _isBuy = true;
            }
            else
            {
                Liquidate();
                _isBuy = false;
            }
        }

        /// <summary>
        /// End of algorithm run event handler. This method is called at the end of a backtest or live trading operation. Intended for closing out logs.
        /// </summary>
        public override void OnEndOfAlgorithm()
        {
            if (Settings.TradingDaysPerYear != _daysInYear)
            {
                throw new Exception("The Algorithm was using invalid `TradingDaysPerYear` for this brokerage. The ExpectedStatistics is wrong.");
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally => true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public Language[] Languages { get; } = { Language.CSharp };

        /// <summary>
        /// Data Points count of all time slices of algorithm
        /// </summary>
        public long DataPoints => 673;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 43;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "388"},
            {"Average Win", "0.01%"},
            {"Average Loss", "-0.01%"},
            {"Compounding Annual Return", "-0.597%"},
            {"Drawdown", "0.700%"},
            {"Expectancy", "-0.400"},
            {"Start Equity", "100000.00"},
            {"End Equity", "99365.56"},
            {"Net Profit", "-0.634%"},
            {"Sharpe Ratio", "-7.126"},
            {"Sortino Ratio", "-7.337"},
            {"Probabilistic Sharpe Ratio", "0.000%"},
            {"Loss Rate", "66%"},
            {"Win Rate", "34%"},
            {"Profit-Loss Ratio", "0.79"},
            {"Alpha", "0"},
            {"Beta", "0"},
            {"Annual Standard Deviation", "0.002"},
            {"Annual Variance", "0"},
            {"Information Ratio", "-3.086"},
            {"Tracking Error", "0.002"},
            {"Treynor Ratio", "0"},
            {"Total Fees", "$331.31"},
            {"Estimated Strategy Capacity", "$71000.00"},
            {"Lowest Capacity Asset", "BTCUSD 2XR"},
            {"Portfolio Turnover", "0.29%"},
            {"OrderListHash", "179b672b3c1024bbe49dd3b4974232f1"}
        };
    }
}

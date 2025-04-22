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
using QuantConnect.Indicators;
using QuantConnect.Interfaces;
using QuantConnect.Orders;
using QuantConnect.Brokerages;


namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression test to explain how Beta indicator works
    /// </summary>
    public class AddBetaIndicatorNewAssetsRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Beta _beta;
        private SimpleMovingAverage _sma;
        private decimal _lastSMAValue;

        public override void Initialize()
        {
            SetStartDate(2015, 05, 08);
            SetEndDate(2017, 06, 15);
            SetCash(10000);

            AddCrypto("BTCUSD", Resolution.Daily);
            AddEquity("SPY", Resolution.Daily);

            EnableAutomaticIndicatorWarmUp = true;
            _beta = B("BTCUSD", "SPY", 3, Resolution.Daily);
            _sma = SMA("SPY", 3, Resolution.Daily);
            _lastSMAValue = 0;

            if (!_beta.IsReady)
            {
                throw new RegressionTestException("Beta indicator was expected to be ready");
            }
        }

        public override void OnData(Slice slice)
        {
            var price = Securities["BTCUSD"].Price;

            if (!Portfolio.Invested)
            {
                var quantityToBuy = (int)(Portfolio.Cash * 0.05m / price);
                Buy("BTCUSD", quantityToBuy);
            }

            if (Math.Abs(_beta.Current.Value) > 2)
            {
                Liquidate("BTCUSD");
                Log("Liquidated BTCUSD due to high Beta");
            }

            Log($"Beta between BTCUSD and SPY is: {_beta.Current.Value}");
        }

        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            var order = Transactions.GetOrderById(orderEvent.OrderId);
            var goUpwards = _lastSMAValue < _sma.Current.Value;
            _lastSMAValue = _sma.Current.Value;

            if (order.Status == OrderStatus.Filled)
            {
                if (order.Type == OrderType.Limit && Math.Abs(_beta.Current.Value - 1) < 0.2m && goUpwards)
                {
                    Transactions.CancelOpenOrders(order.Symbol);
                }
            }

            if (order.Status == OrderStatus.Canceled)
            {
                Log(orderEvent.ToString());
            }
        }

        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public virtual List<Language> Languages { get; } = new() { Language.CSharp };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 5798;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 77;

        /// <summary>
        /// Final status of the algorithm
        /// </summary>
        public AlgorithmStatus AlgorithmStatus => AlgorithmStatus.Completed;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "436"},
            {"Average Win", "0.28%"},
            {"Average Loss", "-0.01%"},
            {"Compounding Annual Return", "1.926%"},
            {"Drawdown", "1.000%"},
            {"Expectancy", "1.650"},
            {"Start Equity", "10000.00"},
            {"End Equity", "10411.11"},
            {"Net Profit", "4.111%"},
            {"Sharpe Ratio", "0.332"},
            {"Sortino Ratio", "0.313"},
            {"Probabilistic Sharpe Ratio", "74.084%"},
            {"Loss Rate", "90%"},
            {"Win Rate", "10%"},
            {"Profit-Loss Ratio", "25.26"},
            {"Alpha", "0.003"},
            {"Beta", "0.001"},
            {"Annual Standard Deviation", "0.01"},
            {"Annual Variance", "0"},
            {"Information Ratio", "-0.495"},
            {"Tracking Error", "0.111"},
            {"Treynor Ratio", "2.716"},
            {"Total Fees", "$0.00"},
            {"Estimated Strategy Capacity", "$87000.00"},
            {"Lowest Capacity Asset", "BTCUSD 2XR"},
            {"Portfolio Turnover", "2.22%"},
            {"OrderListHash", "9fce77ef8817cf0159897fc64d01f5e9"}
        };
    }
}

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
using QuantConnect.Securities;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm reproducing issue #5610
    /// </summary>
    public class OptionExerciseRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _equity, _option;
        private Symbol _contractSymbol;
        private bool _purchasedUnderlying;
        private int quantity = 20;

        public override void Initialize()
        {
            SetStartDate(2014, 6, 6);
            SetEndDate(2014, 6, 9);
            SetCash(1000000);

            _equity = AddEquity("AAPL", Resolution.Minute).Symbol;
            var option = AddOption("AAPL", Resolution.Minute);
            _option = option.Symbol;

            option.SetFilter(universe => from symbol in universe
                                .WeeklysOnly()
                                .Strikes(-5, +5)
                                .Expiration(TimeSpan.Zero, TimeSpan.FromDays(29))
                                         select symbol);
        }

        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            Log($"Order Symbol: {orderEvent.Symbol}; Quantity: {orderEvent.Quantity}; Status: {orderEvent.Status}");

            if (orderEvent.Symbol == _equity && orderEvent.Status == OrderStatus.Filled)
            {
                _purchasedUnderlying = true;
            }
        }

        public override void OnData(Slice data)
        {
            if (_contractSymbol != null)
            {
                return;
            }

            // Buy the underlying for our covered put
            if (data.ContainsKey(_equity) && !_purchasedUnderlying)
            {
                MarketOrder(_equity, 100 * quantity);
            }

            // Buy a contract and exercise it immediately
            if (_purchasedUnderlying && data.OptionChains.TryGetValue(_option, out OptionChain chain))
            {
                var contract = chain
                    .Where(x => x.Right == OptionRight.Put)
                    .OrderByDescending(x => x.Strike - data[_equity].Price)
                    .FirstOrDefault();

                _contractSymbol = contract.Symbol;
                MarketOrder(_contractSymbol, quantity);

                // Exercise option
                Log("Quantity before: " + Portfolio[_equity].Quantity);
                ExerciseOption(_contractSymbol, quantity);
                Log("Quantity after: " + Portfolio[_equity].Quantity);
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (Portfolio[_equity].Quantity != 0)
            {
                throw new Exception("Regression equity holdings should be zero after exercise.");
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
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 1864376;

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
            {"Average Win", "2.13%"},
            {"Average Loss", "-2.21%"},
            {"Compounding Annual Return", "-12.347%"},
            {"Drawdown", "0.100%"},
            {"Expectancy", "-0.019"},
            {"Start Equity", "1000000"},
            {"End Equity", "998677"},
            {"Net Profit", "-0.132%"},
            {"Sharpe Ratio", "0"},
            {"Sortino Ratio", "0"},
            {"Probabilistic Sharpe Ratio", "0%"},
            {"Loss Rate", "50%"},
            {"Win Rate", "50%"},
            {"Profit-Loss Ratio", "0.96"},
            {"Alpha", "0"},
            {"Beta", "0"},
            {"Annual Standard Deviation", "0"},
            {"Annual Variance", "0"},
            {"Information Ratio", "-9.486"},
            {"Tracking Error", "0.008"},
            {"Treynor Ratio", "0"},
            {"Total Fees", "$23.00"},
            {"Estimated Strategy Capacity", "$420000.00"},
            {"Lowest Capacity Asset", "AAPL 2ZQA0P58YFYIU|AAPL R735QTJ8XC9X"},
            {"Portfolio Turnover", "66.12%"},
            {"OrderListHash", "e448ec4e631d4215a2cae661e3a219ed"}
        };
    }
}

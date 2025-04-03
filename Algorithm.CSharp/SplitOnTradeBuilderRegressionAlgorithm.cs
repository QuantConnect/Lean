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
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;
using QuantConnect.Orders;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression test for asserting that splits are applied to the <see cref="QCAlgorithm.TradeBuilder"/>
    /// </summary>
    public class SplitOnTradeBuilderRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _symbol;
        private Split _split;
        private OrderEvent _buyFillEvent;

        public override void Initialize()
        {
            SetStartDate(2014, 6, 6);
            SetEndDate(2014, 6, 11);
            SetCash(100000);
            SetBenchmark(x => 0);

            _symbol = AddEquity("AAPL", Resolution.Hour, dataNormalizationMode: DataNormalizationMode.Raw).Symbol;
        }

        public override void OnData(Slice slice)
        {
            if (slice.Splits.TryGetValue(_symbol, out var split) && split.Type == SplitType.SplitOccurred)
            {
                _split = split;
                Debug($"Split occurred on {split.Time}: {split}");
            }

            if (slice.ContainsKey(_symbol))
            {
                if (!Portfolio.Invested)
                {
                    if (_split == null)
                    {
                        Buy(_symbol, 100);
                    }
                }
                else if (_split != null)
                {
                    Liquidate(_symbol);
                }
            }
        }

        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            if (orderEvent.Status == OrderStatus.Filled && orderEvent.Direction == OrderDirection.Buy)
            {
                _buyFillEvent = orderEvent;
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (_split == null)
            {
                throw new RegressionTestException("No split occurred.");
            }

            if (_buyFillEvent == null)
            {
                throw new RegressionTestException("Buy order either never filled or was never placed.");
            }

            if (TradeBuilder.ClosedTrades.Count != 1)
            {
                throw new RegressionTestException($"Expected 1 closed trade, but found {TradeBuilder.ClosedTrades.Count}");
            }

            var trade = TradeBuilder.ClosedTrades[0];

            var expectedEntryPrice = _buyFillEvent.FillPrice * _split.SplitFactor;
            if (trade.EntryPrice != expectedEntryPrice)
            {
                throw new RegressionTestException($"Expected closed trade entry price of {expectedEntryPrice}, but found {trade.EntryPrice}");
            }

            var expectedTradeQuantity = (int)(_buyFillEvent.FillQuantity / _split.SplitFactor);
            if (trade.Quantity != expectedTradeQuantity)
            {
                throw new RegressionTestException($"Expected closed trade quantity of {expectedTradeQuantity}, but found {trade.Quantity}");
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
        public long DataPoints => 31;

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
            {"Average Win", "0.09%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "6.103%"},
            {"Drawdown", "0.400%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "100092.01"},
            {"Net Profit", "0.092%"},
            {"Sharpe Ratio", "7.379"},
            {"Sortino Ratio", "0"},
            {"Probabilistic Sharpe Ratio", "95.713%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "100%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0"},
            {"Beta", "0"},
            {"Annual Standard Deviation", "0.023"},
            {"Annual Variance", "0.001"},
            {"Information Ratio", "7.707"},
            {"Tracking Error", "0.023"},
            {"Treynor Ratio", "0"},
            {"Total Fees", "$4.50"},
            {"Estimated Strategy Capacity", "$61000000.00"},
            {"Lowest Capacity Asset", "AAPL R735QTJ8XC9X"},
            {"Portfolio Turnover", "21.61%"},
            {"OrderListHash", "be48105b9ce730de7bd4e4908f8c3ef5"}
        };
    }
}

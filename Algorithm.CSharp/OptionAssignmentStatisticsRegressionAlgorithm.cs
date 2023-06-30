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
 *
*/

using System;
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Orders;
using QuantConnect.Securities;
using QuantConnect.Statistics;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm asserting final trade statistics for options assignment
    /// </summary>
    public class OptionAssignmentStatisticsRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Security _stock;
        private Security _callOption;
        private Symbol _callOptionSymbol;

        public override void Initialize()
        {
            SetStartDate(2015, 12, 23);
            SetEndDate(2015, 12, 28);
            SetCash(100000);

            _stock = AddEquity("GOOG", Resolution.Minute);

            var contracts = OptionChainProvider.GetOptionContractList(_stock.Symbol, UtcTime).ToList();

            _callOptionSymbol = contracts
                .Where(c => c.ID.OptionRight == OptionRight.Call)
                .OrderBy(c => c.ID.Date)
                .First(c => c.ID.StrikePrice == 600m);

            _callOption = AddOptionContract(_callOptionSymbol);
        }

        public override void OnData(Slice data)
        {
            if (!Portfolio.Invested && _stock.Price != 0 && _callOption.Price != 0)
            {
                if (Time < _callOptionSymbol.ID.Date)
                {
                    MarketOrder(_callOptionSymbol, 1);
                }
            }
            else if (_stock.Invested)
            {
                Liquidate(_stock.Symbol);
            }
        }

        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            if (orderEvent.Status == OrderStatus.Filled)
            {

            }
        }

        public override void OnEndOfAlgorithm()
        {
            var trades = TradeBuilder.ClosedTrades;

            if (trades.Count != 2)
            {
                throw new Exception($"Expected 2 closed trades: 1 for the option, 1 for the underlying. Actual: {trades.Count}");
            }

            var statistics = new TradeStatistics(trades);

            if (statistics.TotalNumberOfTrades != 2)
            {
                throw new Exception($"Expected 2 total trades: 1 for the option, 1 for the underlying. Actual: {statistics.TotalNumberOfTrades}");
            }

            if (statistics.NumberOfWinningTrades != 2)
            {
                throw new Exception($"Expected 2 winning trades (the ITM option and the underlying trades). Actual {statistics.NumberOfWinningTrades}");
            }

            if (statistics.NumberOfLosingTrades != 0)
            {
                throw new Exception($"Expected no losing trades. Actual {statistics.NumberOfLosingTrades}");
            }

            if (statistics.WinRate != 1)
            {
                throw new Exception($"Expected win rate to be 1. Actual {statistics.WinRate}");
            }

            if (statistics.LossRate != 0)
            {
                throw new Exception($"Expected loss rate to be 0. Actual {statistics.LossRate}");
            }

            if (statistics.WinLossRatio != 10)
            {
                throw new Exception($"Expected win-loss ratio to be 10 (give there are no losing trades). Actual {statistics.WinLossRatio}");
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public virtual Language[] Languages { get; } = { Language.CSharp };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 2821;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public virtual Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "3"},
            {"Average Win", "17.53%"},
            {"Average Loss", "-15.52%"},
            {"Compounding Annual Return", "-36.888%"},
            {"Drawdown", "1.100%"},
            {"Expectancy", "1.129"},
            {"Net Profit", "-0.712%"},
            {"Sharpe Ratio", "-8.332"},
            {"Probabilistic Sharpe Ratio", "0.017%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "100%"},
            {"Profit-Loss Ratio", "1.13"},
            {"Alpha", "-0.009"},
            {"Beta", "0.455"},
            {"Annual Standard Deviation", "0.011"},
            {"Annual Variance", "0"},
            {"Information Ratio", "7.483"},
            {"Tracking Error", "0.012"},
            {"Treynor Ratio", "-0.203"},
            {"Total Fees", "$2.00"},
            {"Estimated Strategy Capacity", "$0"},
            {"Lowest Capacity Asset", "GOOCV VP83T1ZUHROL"},
            {"Portfolio Turnover", "25.23%"},
            {"OrderListHash", "5fdcbc7d934bfe55c523a857c8d58eb0"}
        };
    }
}

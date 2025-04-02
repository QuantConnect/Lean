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
    ///
    /// Expected win/loss rate statistics for the regression algorithm:
    ///     Loss Rate 25%
    ///     Win Rate 75%
    /// </summary>
    public class OptionAssignmentStatisticsRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Security _goog;
        private Security _googCall600;
        private Symbol _googCall600Symbol;
        private Security _googCall650;
        private Symbol _googCall650Symbol;

        public override void Initialize()
        {
            SetStartDate(2015, 12, 23);
            SetEndDate(2015, 12, 28);
            SetCash(100000);

            _goog = AddEquity("GOOG", Resolution.Minute);

            var contracts = OptionChain(_goog.Symbol).ToList();

            _googCall600Symbol = contracts
                .Where(c => c.ID.OptionRight == OptionRight.Call)
                .OrderBy(c => c.ID.Date)
                .First(c => c.ID.StrikePrice == 600m);
            _googCall600 = AddOptionContract(_googCall600Symbol);
            _googCall600["closed"] = false;

            _googCall650Symbol = contracts
                .Where(c => c.ID.OptionRight == OptionRight.Call)
                .OrderBy(c => c.ID.Date)
                .First(c => c.ID.StrikePrice == 650m);
            _googCall650 = AddOptionContract(_googCall650Symbol);
            _googCall650["closed"] = false;
            _googCall650["bought"] = false;
        }

        public override void OnData(Slice slice)
        {
            if (_goog.Price == 0 || _googCall600.Price == 0 || _googCall650.Price == 0)
            {
                return;
            }

            if (!Portfolio.Invested)
            {
                if (Time < _googCall600Symbol.ID.Date)
                {
                    // This option assignment is expected to be a losing trade. The option is ITM but the premium paid is higher than the pay off
                    MarketOrder(_googCall600Symbol, 1);
                }

                if (Time < _googCall650Symbol.ID.Date && !(bool)_googCall650["bought"])
                {
                    // This option assignment is expected to be a winning trade
                    LimitOrder(_googCall650Symbol, 1, 0.95m * _googCall650.Price);
                    // This is to avoid placing another order for this option
                    _googCall650["bought"] = true;
                }
            }
            else if (_goog.Invested && (bool)_googCall600["closed"] && (bool)_googCall650["closed"])
            {
                Liquidate(_goog.Symbol);
            }
        }

        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            if (orderEvent.Status == OrderStatus.Filled && orderEvent.Symbol.SecurityType.IsOption())
            {
                Securities[orderEvent.Symbol]["closed"] = true;
            }
        }

        public override void OnEndOfAlgorithm()
        {
            AssertTradeStatistics();
            AssertPortfolioStatistics();
        }

        private void AssertTradeStatistics()
        {
            var trades = TradeBuilder.ClosedTrades;

            if (trades.Count != 4)
            {
                throw new RegressionTestException($@"AssertTradeStatistics(): Expected 4 closed trades: 2 for the options, 2 for the underlying. Actual: {
                    trades.Count}");
            }

            var statistics = new TradeStatistics(trades);

            if (statistics.TotalNumberOfTrades != 4)
            {
                throw new RegressionTestException($@"AssertTradeStatistics(): Expected 4 total trades: 2 for the options, 2 for the underlying. Actual: {
                    statistics.TotalNumberOfTrades}");
            }

            if (statistics.NumberOfWinningTrades != 3)
            {
                throw new RegressionTestException($@"AssertTradeStatistics(): Expected 3 winning trades (the ITM 650 strike option and the underlying trades). Actual {
                    statistics.NumberOfWinningTrades}");
            }

            if (statistics.NumberOfLosingTrades != 1)
            {
                throw new RegressionTestException($@"AssertTradeStatistics(): Expected 1 losing trade (the 600 strike option). Actual {
                    statistics.NumberOfLosingTrades}");
            }

            if (statistics.WinRate != 0.75m)
            {
                throw new RegressionTestException($"AssertTradeStatistics(): Expected win rate to be 0.75. Actual {statistics.WinRate}");
            }

            if (statistics.LossRate != 0.25m)
            {
                throw new RegressionTestException($"AssertTradeStatistics(): Expected loss rate to be 0.25. Actual {statistics.LossRate}");
            }

            if (statistics.WinLossRatio != 3)
            {
                throw new RegressionTestException($"AssertTradeStatistics(): Expected win-loss ratio to be 3. Actual {statistics.WinLossRatio}");
            }

            // Let's assert the trades per symbol just to be sure

            // We expect the first option (600 strike) to be a losing trade
            var googCall600Trade = trades.Where(t => t.Symbol == _googCall600Symbol).FirstOrDefault();
            if (googCall600Trade == null)
            {
                throw new RegressionTestException("AssertTradeStatistics(): Expected a closed trade for the 600 strike option");
            }
            if (googCall600Trade.IsWin)
            {
                throw new RegressionTestException("AssertTradeStatistics(): Expected the 600 strike option to be a losing trade");
            }

            // We expect the second option (650 strike) to be a winning trade
            var googCall650Trade = trades.Where(t => t.Symbol == _googCall650Symbol).FirstOrDefault();
            if (googCall650Trade == null)
            {
                throw new RegressionTestException("AssertTradeStatistics(): Expected a closed trade for the 650 strike option");
            }
            if (!googCall650Trade.IsWin)
            {
                throw new RegressionTestException("AssertTradeStatistics(): Expected the 650 strike option to be a winning trade");
            }

            // We expect the both underlying trades to be winning trades
            var googTrades = trades.Where(t => t.Symbol == _goog.Symbol).ToList();
            if (googTrades.Count != 2)
            {
                throw new RegressionTestException(
                    $@"AssertTradeStatistics(): Expected 2 closed trades for the underlying, one for each option assignment. Actual: {
                        googTrades.Count}");
            }
            if (googTrades.Any(x => !x.IsWin || x.ProfitLoss < 0))
            {
                throw new RegressionTestException("AssertTradeStatistics(): Expected both underlying trades to be winning trades");
            }
        }

        private void AssertPortfolioStatistics()
        {
            // First, let's check the transactions, which are used to build the portfolio statistics

            // We expected 2 winning transactions (one of the options assignment and the underlying liquidation)
            // and 1 losing transaction (the other option assignment)
            if (Transactions.WinCount != 2)
            {
                throw new RegressionTestException($"AssertPortfolioStatistics(): Expected 2 winning transactions. Actual {Transactions.WinCount}");
            }
            if (Transactions.LossCount != 1)
            {
                throw new RegressionTestException($"AssertPortfolioStatistics(): Expected 1 losing transaction. Actual {Transactions.LossCount}");
            }

            var portfolioStatistics = Statistics.TotalPerformance.PortfolioStatistics;

            if (portfolioStatistics.WinRate != 2m / 3m)
            {
                throw new RegressionTestException($"AssertPortfolioStatistics(): Expected win rate to be 2/3. Actual {portfolioStatistics.WinRate}");
            }

            if (portfolioStatistics.LossRate != 1m / 3m)
            {
                throw new RegressionTestException($"AssertPortfolioStatistics(): Expected loss rate to be 1/3. Actual {portfolioStatistics.LossRate}");
            }

            var expectedAverageWinRate = 0.32962000910479m;
            if (!AreEqual(expectedAverageWinRate, portfolioStatistics.AverageWinRate))
            {
                throw new RegressionTestException($@"AssertPortfolioStatistics(): Expected average win rate to be {expectedAverageWinRate}. Actual {
                    portfolioStatistics.AverageWinRate}");
            }

            var expectedAverageLossRate = -0.13556638257576m;
            if (!AreEqual(expectedAverageLossRate, portfolioStatistics.AverageLossRate))
            {
                throw new RegressionTestException($@"AssertPortfolioStatistics(): Expected average loss rate to be {expectedAverageLossRate}. Actual {
                    portfolioStatistics.AverageLossRate}");
            }

            var expectedProfitLossRatio = 2.43142881621545m;
            if (!AreEqual(expectedProfitLossRatio, portfolioStatistics.ProfitLossRatio))
            {
                throw new RegressionTestException($@"AssertPortfolioStatistics(): Expected profit loss ratio to be {expectedProfitLossRatio}. Actual {
                    portfolioStatistics.ProfitLossRatio}");
            }

            var totalNetProfit = -0.00697m;
            if (!AreEqual(totalNetProfit, portfolioStatistics.TotalNetProfit))
            {
                throw new RegressionTestException($@"AssertPortfolioStatistics(): Expected total net profit to be {totalNetProfit}. Actual {
                    portfolioStatistics.TotalNetProfit}");
            }
        }

        private static bool AreEqual(decimal expected, decimal actual)
        {
            return Math.Abs(expected - actual) < 1e-12m;
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
        public long DataPoints => 4358;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 1;

        /// <summary>
        /// Final status of the algorithm
        /// </summary>
        public AlgorithmStatus AlgorithmStatus => AlgorithmStatus.Completed;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public virtual Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "6"},
            {"Average Win", "32.96%"},
            {"Average Loss", "-13.56%"},
            {"Compounding Annual Return", "-36.270%"},
            {"Drawdown", "1.400%"},
            {"Expectancy", "1.288"},
            {"Start Equity", "100000"},
            {"End Equity", "99303"},
            {"Net Profit", "-0.697%"},
            {"Sharpe Ratio", "-8.675"},
            {"Sortino Ratio", "-6.769"},
            {"Probabilistic Sharpe Ratio", "0.012%"},
            {"Loss Rate", "33%"},
            {"Win Rate", "67%"},
            {"Profit-Loss Ratio", "2.43"},
            {"Alpha", "-0.011"},
            {"Beta", "0.825"},
            {"Annual Standard Deviation", "0.02"},
            {"Annual Variance", "0"},
            {"Information Ratio", "1.705"},
            {"Tracking Error", "0.014"},
            {"Treynor Ratio", "-0.207"},
            {"Total Fees", "$3.00"},
            {"Estimated Strategy Capacity", "$0"},
            {"Lowest Capacity Asset", "GOOCV VP83T1ZUHROL"},
            {"Portfolio Turnover", "50.31%"},
            {"OrderListHash", "eaa9f229fb4efb0beaccbbd71881268a"}
        };
    }
}

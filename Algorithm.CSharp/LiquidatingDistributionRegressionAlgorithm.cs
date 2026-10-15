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

using System.Collections.Generic;
using QuantConnect.Data;
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm asserting a cash distribution is paid out of the security price in raw mode. AAA.1 pays a 47.06 liquidating
    /// distribution on 2007-05-15, on a 69.35 close, and is delisted a few days later: until its next trade the portfolio value must not
    /// count the distribution twice, once as cash and again in the holdings.
    /// </summary>
    public class LiquidatingDistributionRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _symbol;
        private decimal _lastClose;
        private decimal _portfolioValueBeforeDividend;
        private bool _receivedDividend;

        public override void Initialize()
        {
            SetStartDate(2007, 05, 10);
            SetEndDate(2007, 05, 25);
            SetCash(100000);

            _symbol = AddEquity("AAA.1", Resolution.Daily, dataNormalizationMode: DataNormalizationMode.Raw).Symbol;
        }

        public override void OnData(Slice slice)
        {
            if (!Portfolio.Invested && !_receivedDividend)
            {
                SetHoldings(_symbol, 1);
            }

            if (slice.Dividends.TryGetValue(_symbol, out var dividend))
            {
                _receivedDividend = true;
                if (!Portfolio[_symbol].Invested)
                {
                    throw new RegressionTestException("Expected to be holding AAA.1 when the distribution is paid");
                }

                var security = Securities[_symbol];
                var expectedPrice = _lastClose - dividend.Distribution;
                if (security.Price != expectedPrice || security.Holdings.Price != expectedPrice)
                {
                    throw new RegressionTestException($"Expected the price to drop by the distribution {dividend.Distribution} to {expectedPrice}, " +
                        $"but was {security.Price} and the holdings {security.Holdings.Price}");
                }
                // the distribution moves from the holdings into the cash
                if (Portfolio.TotalPortfolioValue != _portfolioValueBeforeDividend)
                {
                    throw new RegressionTestException($"Expected the portfolio value to stay at {_portfolioValueBeforeDividend} when the distribution is paid, " +
                        $"but was {Portfolio.TotalPortfolioValue}");
                }
            }

            if (slice.Bars.TryGetValue(_symbol, out var bar))
            {
                _lastClose = bar.Close;
                _portfolioValueBeforeDividend = Portfolio.TotalPortfolioValue;
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (!_receivedDividend)
            {
                throw new RegressionTestException("Expected the AAA.1 distribution");
            }
            if (Portfolio.Invested)
            {
                throw new RegressionTestException("Expected the delisting to liquidate the AAA.1 holdings");
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
        public long DataPoints => 103;

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
            {"Average Win", "0%"},
            {"Average Loss", "-63.44%"},
            {"Compounding Annual Return", "107.984%"},
            {"Drawdown", "2.100%"},
            {"Expectancy", "-1"},
            {"Start Equity", "100000"},
            {"End Equity", "103193.08"},
            {"Net Profit", "3.193%"},
            {"Sharpe Ratio", "2.279"},
            {"Sortino Ratio", "6.416"},
            {"Probabilistic Sharpe Ratio", "57.243%"},
            {"Loss Rate", "100%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "1.03"},
            {"Beta", "-1.207"},
            {"Annual Standard Deviation", "0.311"},
            {"Annual Variance", "0.096"},
            {"Information Ratio", "1.295"},
            {"Tracking Error", "0.341"},
            {"Treynor Ratio", "-0.587"},
            {"Total Fees", "$7.08"},
            {"Estimated Strategy Capacity", "$120000.00"},
            {"Lowest Capacity Asset", "AAA SEVKGI6HF885"},
            {"Portfolio Turnover", "8.62%"},
            {"Drawdown Recovery", "4"},
            {"OrderListHash", "afe343f6390fea6f13d715f288d2ea89"},
        };
    }
}

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
using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Securities.CryptoFuture;
using System.Collections.Generic;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm asserting that margin used and margin remaining update correctly when
    /// changing leverage on a crypto future
    /// </summary>
    public class CryptoFutureLeverageBasedMarginRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private CryptoFuture _cryptoFuture;

        public override void Initialize()
        {
            SetStartDate(2022, 12, 13);
            SetEndDate(2022, 12, 13);

            SetTimeZone(TimeZones.Utc);

            SetAccountCurrency("USDT");
            SetCash(200);

            SetBrokerageModel(BrokerageName.BinanceFutures, AccountType.Margin);

            _cryptoFuture = AddCryptoFuture("ADAUSDT");
            _cryptoFuture.SetLeverage(10);
        }

        public override void OnData(Slice slice)
        {
            if (_cryptoFuture.Price == 0)
            {
                return;
            }

            if (!Portfolio.Invested)
            {
                SetHoldings(_cryptoFuture.Symbol, 10); // Buy all we can with our margin (leverage is 10)

                var marginUsed = Portfolio.TotalMarginUsed;
                var marginRemaining = Portfolio.MarginRemaining;

                if (marginRemaining > 0)
                {
                    throw new RegressionTestException($"Expected no margin remaining after buying with full leverage. " +
                        $"Actual margin remaining is {marginRemaining}");
                }

                _cryptoFuture.SetLeverage(20);

                var newMarginUsed = Portfolio.TotalMarginUsed;
                var newMarginRemaining = Portfolio.MarginRemaining;

                if (newMarginUsed >= marginUsed)
                {
                    throw new RegressionTestException($"Expected margin used to decrease after increasing leverage. " +
                        $"Previous margin used: {marginUsed}, new margin used: {newMarginUsed}");
                }

                if (newMarginRemaining <= 0 || newMarginRemaining <= marginRemaining)
                {
                    throw new RegressionTestException($"Expected margin remaining to increase after increasing leverage. " +
                        $"Previous margin remaining: {marginRemaining}, new margin remaining: {newMarginRemaining}");
                }

                var holdingsQuantity = _cryptoFuture.Holdings.AbsoluteQuantity;

                SetHoldings(_cryptoFuture.Symbol, 20); // Buy all we can with our margin (new leverage is 20)

                var newHoldingsQuantity = _cryptoFuture.Holdings.AbsoluteQuantity;

                if (newHoldingsQuantity <= holdingsQuantity)
                {
                    throw new RegressionTestException($"Expected holdings quantity to increase after increasing leverage and buying more. " +
                        $"Previous holdings quantity: {holdingsQuantity}, new holdings quantity: {newHoldingsQuantity}");
                }

                newMarginRemaining = Portfolio.MarginRemaining;

                if (marginRemaining > 0)
                {
                    throw new RegressionTestException($"Expected no margin remaining after buying with full leverage. " +
                        $"Actual margin remaining is {newMarginRemaining}");
                }

                // We are done testing, exit the algorithm
                Quit();
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
        public long DataPoints => 4;

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
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "0%"},
            {"Drawdown", "0%"},
            {"Expectancy", "0"},
            {"Start Equity", "200"},
            {"End Equity", "195.58"},
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
            {"Total Fees", "₮1.57"},
            {"Estimated Strategy Capacity", "₮0"},
            {"Lowest Capacity Asset", "ADAUSDT 18R"},
            {"Portfolio Turnover", "2009.51%"},
            {"Drawdown Recovery", "0"},
            {"OrderListHash", "f92ad762f77fbf4ee13b1e89a78cb1eb"}
        };
    }
}

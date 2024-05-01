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

using QuantConnect.Data;
using QuantConnect.Interfaces;
using System.Collections.Generic;
using System;
using QuantConnect.Securities;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm asserting that the current price of the security is adjusted after a split.
    /// Specific for minute resolution.
    /// </summary>
    public class EquitySplitHoldingsMinuteRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Security _aapl;

        private decimal _aaplPriceBeforeSplit;
        private decimal _aaplVolumeBeforeSplit;
        private decimal _aaplOpenBeforeSplit;
        private decimal _aaplCloseBeforeSplit;
        private decimal _aaplHighBeforeSplit;
        private decimal _aaplLowBeforeSplit;
        private decimal _aaplAskPriceBeforeSplit;
        private decimal _aaplBidPriceBeforeSplit;
        private decimal _aaplAskSizeBeforeSplit;
        private decimal _aaplBidSizeBeforeSplit;

        private bool _splitOccurred;

        protected virtual Resolution Resolution => Resolution.Minute;

        public override void Initialize()
        {
            SetStartDate(2014, 6, 5);
            SetEndDate(2014, 6, 11);
            SetCash(100000);

            _aapl = AddEquity("AAPL", Resolution, dataNormalizationMode: DataNormalizationMode.Raw);
        }

        public override void OnData(Slice slice)
        {
            if (!Portfolio.Invested && !_splitOccurred)
            {
                SetHoldings(_aapl.Symbol, -1);
            }

            if (slice.Splits.TryGetValue(_aapl.Symbol, out var split) && split.Type == SplitType.SplitOccurred)
            {
                _splitOccurred = true;

                if (!_aapl.Holdings.Invested)
                {
                    throw new Exception("AAPL is not invested after split occurred");
                }

                if (_aapl.Holdings.Price != _aapl.Price)
                {
                    throw new Exception($"AAPL price is not equal to AAPL holdings price. " +
                        $"AAPL price: {_aapl.Price}, AAPL holdings price: {_aapl.Holdings.Price}");
                }

                AssertFactorChange("Price check", _aaplPriceBeforeSplit, _aapl.Price, split.SplitFactor);
                AssertFactorChange("Open price check", _aaplOpenBeforeSplit, _aapl.Open, split.SplitFactor);
                AssertFactorChange("Close price check", _aaplCloseBeforeSplit, _aapl.Close, split.SplitFactor);
                AssertFactorChange("High price check", _aaplHighBeforeSplit, _aapl.High, split.SplitFactor);
                AssertFactorChange("Low price check", _aaplLowBeforeSplit, _aapl.Low, split.SplitFactor);
                AssertFactorChange("Volume check", _aaplVolumeBeforeSplit, _aapl.Volume, 1 / split.SplitFactor);

                if (Resolution < Resolution.Hour)
                {
                    AssertFactorChange("Ask price check", _aaplAskPriceBeforeSplit, _aapl.AskPrice, split.SplitFactor);
                    AssertFactorChange("Bid price check", _aaplBidPriceBeforeSplit, _aapl.BidPrice, split.SplitFactor);
                    AssertFactorChange("Ask size check", _aaplAskSizeBeforeSplit, _aapl.AskSize, 1 / split.SplitFactor);
                    AssertFactorChange("Bid size check", _aaplBidSizeBeforeSplit, _aapl.BidSize, 1 / split.SplitFactor);
                }
            }
            else
            {
                _aaplPriceBeforeSplit = _aapl.Price;
                _aaplOpenBeforeSplit = _aapl.Open;
                _aaplCloseBeforeSplit = _aapl.Close;
                _aaplHighBeforeSplit = _aapl.High;
                _aaplLowBeforeSplit = _aapl.Low;
                _aaplVolumeBeforeSplit = _aapl.Volume;
                _aaplAskPriceBeforeSplit = _aapl.AskPrice;
                _aaplBidPriceBeforeSplit = _aapl.BidPrice;
                _aaplAskSizeBeforeSplit = _aapl.AskSize;
                _aaplBidSizeBeforeSplit = _aapl.BidSize;
            }
        }

        private static void AssertFactorChange(string messagePrefix, decimal priceBeforeSplit, decimal priceAfterSplit, decimal splitFactor)
        {
            if (Math.Abs(priceAfterSplit / priceBeforeSplit - splitFactor) >= 0.0001m)
            {
                throw new Exception($"{messagePrefix}: split factor is not correct. Expected: {splitFactor}, " +
                    $"Actual: {priceAfterSplit / priceBeforeSplit}");
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (!_splitOccurred)
            {
                throw new Exception("Split did not occur");
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
        public virtual long DataPoints => 3945;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public virtual Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "1"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "-56.234%"},
            {"Drawdown", "2.200%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "98502.10"},
            {"Net Profit", "-1.498%"},
            {"Sharpe Ratio", "-4.002"},
            {"Sortino Ratio", "0"},
            {"Probabilistic Sharpe Ratio", "8.037%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "-0.447"},
            {"Beta", "0.159"},
            {"Annual Standard Deviation", "0.108"},
            {"Annual Variance", "0.012"},
            {"Information Ratio", "-4.67"},
            {"Tracking Error", "0.113"},
            {"Treynor Ratio", "-2.711"},
            {"Total Fees", "$1.00"},
            {"Estimated Strategy Capacity", "$41000000.00"},
            {"Lowest Capacity Asset", "AAPL R735QTJ8XC9X"},
            {"Portfolio Turnover", "14.24%"},
            {"OrderListHash", "5d7b0658b66b331ba8159011aa2ec5b4"}
        };
    }
}

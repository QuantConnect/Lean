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
using QuantConnect.Interfaces;
using QuantConnect.Orders;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Basic algorithm demonstrating how to place trailing stop orders.
    /// </summary>
    /// <meta name="tag" content="trading and orders" />
    /// <meta name="tag" content="placing orders" />
    /// <meta name="tag" content="trailing stop order"/>
    public class TrailingStopOrderRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private const decimal BuyTrailingAmount = 2m;
        private const decimal SellTrailingAmount = 0.5m;

        private Symbol _symbol;
        private OrderTicket _buyOrderTicket;
        private OrderTicket _sellOrderTicket;
        private Slice _previousSlice;

        public override void Initialize()
        {
            SetStartDate(2013, 10, 07);
            SetEndDate(2013, 10, 11);
            SetCash(100000);

            _symbol = AddEquity("SPY").Symbol;
        }

        public override void OnData(Slice slice)
        {
            if (!slice.ContainsKey(_symbol))
            {
                return;
            }

            if (_buyOrderTicket == null)
            {
                _buyOrderTicket = TrailingStopOrder(_symbol, 100, trailingAmount: BuyTrailingAmount, trailingAsPercentage: false);
            }
            else if (_buyOrderTicket.Status != OrderStatus.Filled)
            {
                var stopPrice = _buyOrderTicket.Get(OrderField.StopPrice);

                // Get the previous bar to compare to the stop price,
                // because stop price update attempt with the current slice data happens after OnData.
                var low = _previousSlice.QuoteBars.TryGetValue(_symbol, out var quoteBar)
                    ? quoteBar.Ask.Low
                    : _previousSlice.Bars[_symbol].Low;

                var stopPriceToMarketPriceDistance = stopPrice - low;
                if (stopPriceToMarketPriceDistance > BuyTrailingAmount)
                {
                    throw new Exception($"StopPrice {stopPrice} should be within {BuyTrailingAmount} of the previous low price {low} at all times.");
                }
            }

            if (_sellOrderTicket == null)
            {
                if (Portfolio.Invested)
                {
                    _sellOrderTicket = TrailingStopOrder(_symbol, -100, trailingAmount: SellTrailingAmount, trailingAsPercentage: false);
                }
            }
            else if (_sellOrderTicket.Status != OrderStatus.Filled)
            {
                var stopPrice = _sellOrderTicket.Get(OrderField.StopPrice);

                // Get the previous bar to compare to the stop price,
                // because stop price update attempt with the current slice data happens after OnData.
                var high = _previousSlice.QuoteBars.TryGetValue(_symbol, out var quoteBar)
                    ? quoteBar.Bid.High
                    : _previousSlice.Bars[_symbol].High;

                var stopPriceToMarketPriceDistance = high - stopPrice;
                if (stopPriceToMarketPriceDistance > SellTrailingAmount)
                {
                    throw new Exception($"StopPrice {stopPrice} should be within {SellTrailingAmount} of the previous high price {high} at all times.");
                }
            }

            _previousSlice = slice;
        }

        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            if (orderEvent.Status == OrderStatus.Filled)
            {
                if (orderEvent.Direction == OrderDirection.Buy)
                {
                    var stopPrice = _buyOrderTicket.Get(OrderField.StopPrice);
                    if (orderEvent.FillPrice < stopPrice)
                    {
                        throw new Exception($@"Buy trailing stop order should have filled with price greater than or equal to the stop price {
                            stopPrice}. Fill price: {orderEvent.FillPrice}");
                    }
                }
                else
                {
                    var stopPrice = _sellOrderTicket.Get(OrderField.StopPrice);
                    if (orderEvent.FillPrice > stopPrice)
                    {
                        throw new Exception($@"Sell trailing stop order should have filled with price less than or equal to the stop price {
                            stopPrice}. Fill price: {orderEvent.FillPrice}");
                    }
                }
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally => true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public Language[] Languages => new[] { Language.CSharp, Language.Python };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 3943;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "2"},
            {"Average Win", "0.02%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "1.833%"},
            {"Drawdown", "0.000%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "100023.22"},
            {"Net Profit", "0.023%"},
            {"Sharpe Ratio", "3.926"},
            {"Sortino Ratio", "0"},
            {"Probabilistic Sharpe Ratio", "95.977%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "100%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "-0.007"},
            {"Beta", "0.007"},
            {"Annual Standard Deviation", "0.002"},
            {"Annual Variance", "0"},
            {"Information Ratio", "-8.907"},
            {"Tracking Error", "0.221"},
            {"Treynor Ratio", "1.031"},
            {"Total Fees", "$2.00"},
            {"Estimated Strategy Capacity", "$36000000.00"},
            {"Lowest Capacity Asset", "SPY R735QTJ8XC9X"},
            {"Portfolio Turnover", "5.79%"},
            {"OrderListHash", "d56bac89a568c3a45cac595e69a35875"}
        };
    }
}

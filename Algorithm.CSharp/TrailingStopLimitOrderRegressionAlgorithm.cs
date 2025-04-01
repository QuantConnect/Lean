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

using System.Collections.Generic;
using QuantConnect.Data;
using QuantConnect.Indicators;
using QuantConnect.Interfaces;
using QuantConnect.Orders;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Basic algorithm demonstrating how to place trailing stop limit orders.
    /// </summary>
    /// <meta name="tag" content="trading and orders" />
    /// <meta name="tag" content="placing orders" />
    /// <meta name="tag" content="trailing stop limit order"/>
    public class TrailingStopLimitOrderRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _symbol;
        private OrderTicket _buyOrderTicket;
        private OrderTicket _sellOrderTicket;
        private Slice _previousSlice;

        private const decimal Tolerance = 0.001m;
        private const int FastPeriod = 30;
        private const int SlowPeriod = 60;
        private const decimal TrailingAmount = 5m;
        private const decimal LimitOffset = 1m;

        private ExponentialMovingAverage _fast;
        private ExponentialMovingAverage _slow;

        public bool IsReady { get { return _fast.IsReady && _slow.IsReady; } }
        public bool TrendIsUp { get { return IsReady && _fast > _slow * (1 + Tolerance); } }
        public bool TrendIsDown { get { return IsReady && _fast < _slow * (1 + Tolerance); } }

        /// <summary>
        /// Initialize the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2013, 01, 01);
            SetEndDate(2017, 01, 01);
            SetCash(100000);

            _symbol = AddEquity("SPY", Resolution.Daily).Symbol;

            _fast = EMA(_symbol, FastPeriod, Resolution.Daily);
            _slow = EMA(_symbol, SlowPeriod, Resolution.Daily);
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="slice">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice slice)
        {
            if (!slice.ContainsKey(_symbol))
            {
                return;
            }

            if (!IsReady)
            {
                return;
            }

            var security = Securities[_symbol];

            if (_buyOrderTicket == null)
            {
                if (TrendIsUp)
                {
                    _buyOrderTicket = TrailingStopLimitOrder(_symbol, 100, security.Price * 1.10m,
                        (security.Price * 1.10m) + LimitOffset, TrailingAmount, false, LimitOffset);
                }
            }
            else if (_buyOrderTicket.Status != OrderStatus.Filled)
            {
                var stopPrice = _buyOrderTicket.Get(OrderField.StopPrice);
                var limitPrice = _buyOrderTicket.Get(OrderField.LimitPrice);

                // Get the previous bar to compare to the stop and limit prices,
                // because stop and limit price update attempt with the current slice data happens after OnData.
                var low = _previousSlice.QuoteBars.TryGetValue(_symbol, out var quoteBar)
                    ? quoteBar.Ask.Low
                    : _previousSlice.Bars[_symbol].Low;

                var stopPriceToMarketPriceDistance = stopPrice - low;
                if (stopPriceToMarketPriceDistance > TrailingAmount)
                {
                    throw new RegressionTestException($"StopPrice {stopPrice} should be within {TrailingAmount} of the previous low price {low} at all times.");
                }

                var stopPriceToLimitPriceDistance = limitPrice - stopPrice;
                if (stopPriceToLimitPriceDistance != LimitOffset)
                {
                    throw new RegressionTestException($"LimitPrice {limitPrice} should be {LimitOffset} from the stop price {stopPrice} at all times.");
                }
            }

            else if (_sellOrderTicket == null)
            {
                if (TrendIsDown)
                {
                    _sellOrderTicket = TrailingStopLimitOrder(_symbol, -100, security.Price * 0.99m, (security.Price * 0.99m) - LimitOffset,
                        TrailingAmount, false, LimitOffset);
                }
            }
            else if (_sellOrderTicket.Status != OrderStatus.Filled)
            {
                var stopPrice = _sellOrderTicket.Get(OrderField.StopPrice);
                var limitPrice = _sellOrderTicket.Get(OrderField.LimitPrice);

                // Get the previous bar to compare to the stop and limit prices,
                // because stop and limit price update attempt with the current slice data happens after OnData.
                var high = _previousSlice.QuoteBars.TryGetValue(_symbol, out var quoteBar)
                    ? quoteBar.Bid.High
                    : _previousSlice.Bars[_symbol].High;

                var stopPriceToMarketPriceDistance = high - stopPrice;
                if (stopPriceToMarketPriceDistance > TrailingAmount)
                {
                    throw new RegressionTestException($"StopPrice {stopPrice} should be within {TrailingAmount} of the previous high price {high} at all times.");
                }

                var stopPriceToLimitPriceDistance = stopPrice - limitPrice;
                if (stopPriceToLimitPriceDistance != LimitOffset)
                {
                    throw new RegressionTestException($"LimitPrice {limitPrice} should be {LimitOffset} from the stop price {stopPrice} at all times.");
                }
            }

            _previousSlice = slice;
        }

        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            if (orderEvent.Status == OrderStatus.Filled)
            {
                var order = Transactions.GetOrderById(orderEvent.OrderId);
                if (!((TrailingStopLimitOrder)order).StopTriggered)
                {
                    throw new RegressionTestException("TrailingStopLimitOrder StopTriggered should have been set if the order filled.");
                }

                if (orderEvent.Direction == OrderDirection.Buy)
                {
                    var limitPrice = _buyOrderTicket.Get(OrderField.LimitPrice);
                    if (orderEvent.FillPrice > limitPrice)
                    {
                        throw new RegressionTestException($@"Buy trailing stop limit order should have filled with price less than or equal to the limit price {limitPrice}. Fill price: {orderEvent.FillPrice}");
                    }
                }
                else
                {
                    var limitPrice = _sellOrderTicket.Get(OrderField.LimitPrice);
                    if (orderEvent.FillPrice < limitPrice)
                    {
                        throw new RegressionTestException($@"Sell trailing stop limit order should have filled with price greater than or equal to the limit price {limitPrice}. Fill price: {orderEvent.FillPrice}");
                    }
                }
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (_buyOrderTicket == null || _sellOrderTicket == null)
            {
                throw new RegressionTestException("Expected two orders (buy and sell) to have been filled at the end of the algorithm.");
            }

            if (_buyOrderTicket.Status != OrderStatus.Filled || _sellOrderTicket.Status != OrderStatus.Filled)
            {
                throw new RegressionTestException("Expected the two orders (buy and sell) to have been filled at the end of the algorithm.");
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public List<Language> Languages { get; } = new() { Language.CSharp, Language.Python };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 8061;

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
            {"Average Win", "2.59%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "0.641%"},
            {"Drawdown", "1.400%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "102587.73"},
            {"Net Profit", "2.588%"},
            {"Sharpe Ratio", "-0.424"},
            {"Sortino Ratio", "-0.281"},
            {"Probabilistic Sharpe Ratio", "12.205%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "100%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "-0.008"},
            {"Beta", "0.044"},
            {"Annual Standard Deviation", "0.009"},
            {"Annual Variance", "0"},
            {"Information Ratio", "-0.954"},
            {"Tracking Error", "0.103"},
            {"Treynor Ratio", "-0.085"},
            {"Total Fees", "$2.00"},
            {"Estimated Strategy Capacity", "$3800000000.00"},
            {"Lowest Capacity Asset", "SPY R735QTJ8XC9X"},
            {"Portfolio Turnover", "0.02%"},
            {"OrderListHash", "977baf60d0a4640106bd9a0f57e73a3a"}
        };
    }
}

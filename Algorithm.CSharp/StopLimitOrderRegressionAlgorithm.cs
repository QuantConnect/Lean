
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
using QuantConnect.Indicators;
using QuantConnect.Interfaces;
using QuantConnect.Orders;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Basic algorithm demonstrating how to place stop limit orders.
    /// </summary>
    /// <meta name="tag" content="trading and orders" />
    /// <meta name="tag" content="placing orders" />
    /// <meta name="tag" content="stop limit order"/>
    public class StopLimitOrderRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _symbol;
        private OrderTicket _buyOrderTicket;
        private OrderTicket _sellOrderTicket;

        private const decimal _tolerance = 0.001m;
        private const int _fastPeriod = 30;
        private const int _slowPeriod = 60;

        private ExponentialMovingAverage _fast;
        private ExponentialMovingAverage _slow;

        public bool IsReady { get { return _fast.IsReady && _slow.IsReady; } }
        public bool TrendIsUp { get { return IsReady && _fast > _slow * (1 + _tolerance); } }
        public bool TrendIsDown { get { return IsReady && _fast < _slow * (1 + _tolerance); } }

        /// <summary>
        /// Initialize the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2013, 01, 01);
            SetEndDate(2017, 01, 01);
            SetCash(100000);

            _symbol = AddEquity("SPY", Resolution.Daily).Symbol;

            _fast = EMA(_symbol, _fastPeriod, Resolution.Daily);
            _slow = EMA(_symbol, _slowPeriod, Resolution.Daily);
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="data">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice slice)
        {
            if (!IsReady)
            {
                return;
            }

            var security = Securities[_symbol];
            if (_buyOrderTicket == null && TrendIsUp)
            {
                _buyOrderTicket = StopLimitOrder(_symbol, 100, stopPrice: security.High * 1.10m, limitPrice: security.High * 1.11m);
            }
            else if (_buyOrderTicket.Status == OrderStatus.Filled && _sellOrderTicket == null && TrendIsDown)
            {
                _sellOrderTicket = StopLimitOrder(_symbol, -100, stopPrice: security.Low * 0.99m, limitPrice: security.Low * 0.98m);
            }
        }

        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            if (orderEvent.Status == OrderStatus.Filled)
            {
                var order = Transactions.GetOrderById(orderEvent.OrderId);
                if (!((StopLimitOrder)order).StopTriggered)
                {
                    throw new Exception("StopLimitOrder StopTriggered should haven been set if the order filled.");
                }

                if (orderEvent.Direction == OrderDirection.Buy)
                {
                    var limitPrice = _buyOrderTicket.Get(OrderField.LimitPrice);
                    if (orderEvent.FillPrice > limitPrice)
                    {
                        throw new Exception($@"Buy stop limit order should have filled with price less than or equal to the limit price {
                            limitPrice}. Fill price: {orderEvent.FillPrice}");
                    }
                }
                else
                {
                    var limitPrice = _sellOrderTicket.Get(OrderField.LimitPrice);
                    if (orderEvent.FillPrice < limitPrice)
                    {
                        throw new Exception($@"Sell stop limit order should have filled with price greater than or equal to the limit price {
                            limitPrice}. Fill price: {orderEvent.FillPrice}");
                    }
                }
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (_buyOrderTicket == null || _sellOrderTicket == null)
            {
                throw new Exception("Expected two orders (buy and sell) to have been filled at the end of the algorithm.");
            }

            if (_buyOrderTicket.Status != OrderStatus.Filled || _sellOrderTicket.Status != OrderStatus.Filled)
            {
                throw new Exception("Expected the two orders (buy and sell) to have been filled at the end of the algorithm.");
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public Language[] Languages { get; } = { Language.CSharp, Language.Python };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 8062;

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
            {"Average Win", "1.44%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "0.359%"},
            {"Drawdown", "1.500%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "101444.99"},
            {"Net Profit", "1.445%"},
            {"Sharpe Ratio", "-0.749"},
            {"Sortino Ratio", "-0.414"},
            {"Probabilistic Sharpe Ratio", "5.635%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "100%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "-0.009"},
            {"Beta", "0.03"},
            {"Annual Standard Deviation", "0.008"},
            {"Annual Variance", "0"},
            {"Information Ratio", "-0.96"},
            {"Tracking Error", "0.104"},
            {"Treynor Ratio", "-0.188"},
            {"Total Fees", "$2.00"},
            {"Estimated Strategy Capacity", "$2700000000.00"},
            {"Lowest Capacity Asset", "SPY R735QTJ8XC9X"},
            {"Portfolio Turnover", "0.02%"},
            {"OrderListHash", "5fc1779ca4bc3a398a217928b92bb93c"}
        };
    }
}

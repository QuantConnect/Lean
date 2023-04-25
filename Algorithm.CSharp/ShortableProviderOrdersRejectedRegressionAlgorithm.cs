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
using QuantConnect.Brokerages;
using QuantConnect.Data;
using QuantConnect.Securities;
using QuantConnect.Data.Shortable;
using QuantConnect.Interfaces;
using QuantConnect.Orders;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Tests that orders are denied if they exceed the max shortable quantity.
    /// </summary>
    public class ShortableProviderOrdersRejectedRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Security _spy;
        private Security _aig;
        private readonly List<OrderTicket> _ordersAllowed = new List<OrderTicket>();
        private readonly List<OrderTicket> _ordersDenied = new List<OrderTicket>();
        private bool _initialize;
        private OrderEvent _lastOrderEvent;
        private bool _invalidatedAllowedOrder;
        private bool _invalidatedNewOrderWithPortfolioHoldings;

        public override void Initialize()
        {
            SetStartDate(2013, 10, 4);
            SetEndDate(2013, 10, 11);
            SetCash(10000000);

            _spy = AddEquity("SPY", Resolution.Minute);
            _aig = AddEquity("AIG", Resolution.Minute);

            _spy.SetShortableProvider(new RegressionTestShortableProvider());
            _aig.SetShortableProvider(new RegressionTestShortableProvider());
        }

        public override void OnData(Slice data)
        {
            if (!_initialize)
            {
                HandleOrder(LimitOrder(_spy.Symbol, -1001, 10000m)); // Should be canceled, exceeds the max shortable quantity
                HandleOrder(LimitOrder(_spy.Symbol, -1000, 10000m)); // Allowed, orders at or below 1000 should be accepted
                HandleOrder(LimitOrder(_spy.Symbol, -10, 0.01m)); // Should be canceled, the total quantity we would be short would exceed the max shortable quantity.
                _initialize = true;
                return;
            }

            if (!_invalidatedAllowedOrder)
            {
                if (_ordersAllowed.Count != 1)
                {
                    throw new Exception($"Expected 1 successful order, found: {_ordersAllowed.Count}");
                }
                if (_ordersDenied.Count != 2)
                {
                    throw new Exception($"Expected 2 failed orders, found: {_ordersDenied.Count}");
                }

                var allowedOrder = _ordersAllowed[0];
                var orderUpdate = new UpdateOrderFields()
                {
                    LimitPrice = 0.01m,
                    Quantity = -1001,
                    Tag = "Testing updating and exceeding maximum quantity"
                };

                var response = allowedOrder.Update(orderUpdate);
                if (response.ErrorCode != OrderResponseErrorCode.ExceedsShortableQuantity)
                {
                    throw new Exception($"Expected order to fail due to exceeded shortable quantity, found: {response.ErrorCode.ToString()}");
                }

                var cancelResponse = allowedOrder.Cancel();
                if (cancelResponse.IsError)
                {
                    throw new Exception("Expected to be able to cancel open order after bad qty update");
                }

                _invalidatedAllowedOrder = true;
                _ordersDenied.Clear();
                _ordersAllowed.Clear();
                return;
            }

            if (!_invalidatedNewOrderWithPortfolioHoldings)
            {
                HandleOrder(MarketOrder(_spy.Symbol, -1000)); // Should succeed, no holdings and no open orders to stop this
                var spyShares = Portfolio[_spy.Symbol].Quantity;
                if (spyShares != -1000m)
                {
                    throw new Exception($"Expected -1000 shares in portfolio, found: {spyShares}");
                }

                HandleOrder(LimitOrder(_spy.Symbol, -1, 0.01m)); // Should fail, portfolio holdings are at the max shortable quantity.
                if (_ordersDenied.Count != 1)
                {
                    throw new Exception($"Expected limit order to fail due to existing holdings, but found {_ordersDenied.Count} failures");
                }

                _ordersAllowed.Clear();
                _ordersDenied.Clear();

                HandleOrder(MarketOrder(_aig.Symbol, -1001));
                if (_ordersAllowed.Count != 1)
                {
                    throw new Exception($"Expected market order of -1001 BAC to not fail");
                }

                _invalidatedNewOrderWithPortfolioHoldings = true;
            }
        }

        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            _lastOrderEvent = orderEvent;
        }

        private void HandleOrder(OrderTicket orderTicket)
        {
            if (orderTicket.SubmitRequest.Status == OrderRequestStatus.Error)
            {
                if (_lastOrderEvent == null || _lastOrderEvent.Status != OrderStatus.Invalid)
                {
                    throw new Exception($"Expected order event with invalid status for ticket {orderTicket}");
                }

                _lastOrderEvent = null;
                _ordersDenied.Add(orderTicket);
                return;
            }

            _ordersAllowed.Add(orderTicket);
        }

        private class RegressionTestShortableProvider : LocalDiskShortableProvider
        {
            public RegressionTestShortableProvider() : base(SecurityType.Equity, "testbrokerage", Market.USA)
            {
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
        public long DataPoints => 9410;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "2"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "-1.623%"},
            {"Drawdown", "0.100%"},
            {"Expectancy", "0"},
            {"Net Profit", "-0.034%"},
            {"Sharpe Ratio", "-1.535"},
            {"Probabilistic Sharpe Ratio", "33.979%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0.002"},
            {"Beta", "-0.022"},
            {"Annual Standard Deviation", "0.004"},
            {"Annual Variance", "0"},
            {"Information Ratio", "-2.082"},
            {"Tracking Error", "0.179"},
            {"Treynor Ratio", "0.269"},
            {"Total Fees", "$10.01"},
            {"Estimated Strategy Capacity", "$99000000.00"},
            {"Lowest Capacity Asset", "AIG R735QTJ8XC9X"},
            {"Portfolio Turnover", "0.23%"},
            {"OrderListHash", "3ac2d7a61f71c1345eb569e30cc2834c"}
        };
    }
}

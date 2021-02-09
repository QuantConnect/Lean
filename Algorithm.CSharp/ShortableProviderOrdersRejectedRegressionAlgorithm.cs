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
        private Symbol _spy;
        private Symbol _aig;
        private readonly List<OrderTicket> _ordersAllowed = new List<OrderTicket>();
        private readonly List<OrderTicket> _ordersDenied = new List<OrderTicket>();
        private bool _initialize;
        private bool _invalidatedAllowedOrder;
        private bool _invalidatedNewOrderWithPortfolioHoldings;

        public override void Initialize()
        {
            SetStartDate(2013, 10, 4);
            SetEndDate(2013, 10, 11);
            SetCash(10000000);

            _spy = AddEquity("SPY", Resolution.Minute).Symbol;
            _aig = AddEquity("AIG", Resolution.Minute).Symbol;

            SetBrokerageModel(new RegressionTestShortableBrokerageModel());
        }

        public override void OnData(Slice data)
        {
            if (!_initialize)
            {
                HandleOrder(LimitOrder(_spy, -1001, 10000m)); // Should be canceled, exceeds the max shortable quantity
                HandleOrder(LimitOrder(_spy, -1000, 10000m)); // Allowed, orders at or below 1000 should be accepted
                HandleOrder(LimitOrder(_spy, -10, 0.01m)); // Should be canceled, the total quantity we would be short would exceed the max shortable quantity.
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
                HandleOrder(MarketOrder(_spy, -1000)); // Should succeed, no holdings and no open orders to stop this
                var spyShares = Portfolio[_spy].Quantity;
                if (spyShares != -1000m)
                {
                    throw new Exception($"Expected -1000 shares in portfolio, found: {spyShares}");
                }

                HandleOrder(LimitOrder(_spy, -1, 0.01m)); // Should fail, portfolio holdings are at the max shortable quantity.
                if (_ordersDenied.Count != 1)
                {
                    throw new Exception($"Expected limit order to fail due to existing holdings, but found {_ordersDenied.Count} failures");
                }

                _ordersAllowed.Clear();
                _ordersDenied.Clear();

                HandleOrder(MarketOrder(_aig, -1001));
                if (_ordersAllowed.Count != 1)
                {
                    throw new Exception($"Expected market order of -1001 BAC to not fail");
                }

                _invalidatedNewOrderWithPortfolioHoldings = true;
            }
        }

        private void HandleOrder(OrderTicket orderTicket)
        {
            if (orderTicket.SubmitRequest.Status == OrderRequestStatus.Error)
            {
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

        public class RegressionTestShortableBrokerageModel : DefaultBrokerageModel
        {
            public RegressionTestShortableBrokerageModel() : base()
            {
                ShortableProvider = new RegressionTestShortableProvider();
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
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "2"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "-1.719%"},
            {"Drawdown", "0.100%"},
            {"Expectancy", "0"},
            {"Net Profit", "-0.036%"},
            {"Sharpe Ratio", "-1.741"},
            {"Probabilistic Sharpe Ratio", "35.789%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0.004"},
            {"Beta", "-0.023"},
            {"Annual Standard Deviation", "0.005"},
            {"Annual Variance", "0"},
            {"Information Ratio", "-2.512"},
            {"Tracking Error", "0.216"},
            {"Treynor Ratio", "0.367"},
            {"Total Fees", "$10.01"},
            {"Fitness Score", "0"},
            {"Kelly Criterion Estimate", "0"},
            {"Kelly Criterion Probability Value", "0"},
            {"Sortino Ratio", "-4.849"},
            {"Return Over Maximum Drawdown", "-21.738"},
            {"Portfolio Turnover", "0.003"},
            {"Total Insights Generated", "0"},
            {"Total Insights Closed", "0"},
            {"Total Insights Analysis Completed", "0"},
            {"Long Insight Count", "0"},
            {"Short Insight Count", "0"},
            {"Long/Short Ratio", "100%"},
            {"Estimated Monthly Alpha Value", "$0"},
            {"Total Accumulated Estimated Alpha Value", "$0"},
            {"Mean Population Estimated Insight Value", "$0"},
            {"Mean Population Direction", "0%"},
            {"Mean Population Magnitude", "0%"},
            {"Rolling Averaged Population Direction", "0%"},
            {"Rolling Averaged Population Magnitude", "0%"},
            {"OrderListHash", "3c3ed81d3ddd5ff5c21deeaa0e5b7f2d"}
        };
    }
}

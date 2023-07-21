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

using QuantConnect.Interfaces;
using QuantConnect.Orders;
using QuantConnect.Securities;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Base algorithm to assert that the margin call events are fired when trading options
    /// </summary>
    public abstract class OptionsMarginCallEventsAlgorithmBase : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private int _onMarginCallWarningCount;
        private int _onMarginCallCount;
        private bool _firstOrderEventReceived;

        protected abstract int OriginalQuantity { get; }

        protected abstract int ExpectedOrdersCount { get; }

        public override void OnMarginCall(List<SubmitOrderRequest> requests)
        {
            Debug($"OnMarginCall at {Time}");
            _onMarginCallCount++;
        }

        public override void OnMarginCallWarning()
        {
            Debug($"OnMarginCallWarning at {Time}");
            _onMarginCallWarningCount++;
        }

        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            if (orderEvent.Status == OrderStatus.Filled && !_firstOrderEventReceived)
            {
                _firstOrderEventReceived = true;
                // Make sure the algorithms implementing this class place orders with the expected quantity for
                // the check in the OnEndOfAlgorithm method to be accurate.
                if (orderEvent.Quantity != OriginalQuantity)
                {
                    throw new Exception($"Expected order quantity to be {OriginalQuantity} but was {orderEvent.Quantity}");
                }
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (!Portfolio.Invested)
            {
                throw new Exception("Portfolio should be invested");
            }

            if (_onMarginCallCount != 1)
            {
                throw new Exception($"OnMarginCall was called {_onMarginCallCount} times, expected 1");
            }

            if (_onMarginCallWarningCount == 0)
            {
                throw new Exception("OnMarginCallWarning was not called");
            }

            var orders = Transactions.GetOrders().ToList();
            if (orders.Count != ExpectedOrdersCount)
            {
                throw new Exception($"Expected {ExpectedOrdersCount} orders, found {orders.Count}");
            }

            if (orders.Any(order => !order.Status.IsFill()))
            {
                throw new Exception("All orders should be filled");
            }

            var finalStrategyQuantity = Portfolio.Positions.Groups.First().Quantity;
            if (Math.Abs(OriginalQuantity) <= Math.Abs(finalStrategyQuantity))
            {
                throw new Exception($@"Strategy position group quantity should have been decreased from the original quantity {OriginalQuantity
                    }, but was {finalStrategyQuantity}");
            }
        }

        protected class CustomMarginCallModel : DefaultMarginCallModel
        {
            // Setting margin buffer to 0 so we make sure the margin call orders are generated. Otherwise, they will only
            // be generated if the used margin is > 110%TVP, which is unlikely for this case
            public CustomMarginCallModel(SecurityPortfolioManager portfolio, IOrderProperties defaultOrderProperties)
                : base(portfolio, defaultOrderProperties, 0m)
            {
            }
        }

        public abstract bool CanRunLocally { get; }
        public abstract Language[] Languages { get; }
        public abstract long DataPoints { get; }
        public abstract int AlgorithmHistoryDataPoints { get; }
        public abstract Dictionary<string, string> ExpectedStatistics { get; }
    }
}

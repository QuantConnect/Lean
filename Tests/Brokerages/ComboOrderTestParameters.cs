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

using QuantConnect.Interfaces;
using QuantConnect.Orders;

namespace QuantConnect.Tests.Brokerages
{
    public sealed class ComboOrderTestParameters : OrderTestParameters
    {
        /// <summary>
        /// Gets the ask price for the combo order.
        /// </summary>
        public decimal AskPrice { get; }

        /// <summary>
        /// Gets the bid price for the combo order.
        /// </summary>
        public decimal BidPrice { get; }

        /// <summary>
        /// The status to expect when submitting this order in most test cases.
        /// </summary>
        public override OrderStatus ExpectedStatus => OrderStatus.Submitted;

        /// <summary>
        /// Gets a value indicating whether cancellation of this order is expected to succeed.
        /// </summary>
        public override bool ExpectedCancellationResult => true;

        public ComboOrderTestParameters(
            Symbol symbol,
            decimal askPrice = 0,
            decimal bidPrice = 0,
            IOrderProperties properties = null,
            OrderSubmissionData orderSubmissionData = null) : base(symbol, properties, orderSubmissionData)
        {
            AskPrice = askPrice;
            BidPrice = bidPrice;
        }

        public override Order CreateLongOrder(decimal quantity)
        {
            throw new System.NotImplementedException();
        }

        public override Order CreateShortOrder(decimal quantity)
        {
            throw new System.NotImplementedException();
        }

        public override bool ModifyOrderToFill(IBrokerage brokerage, Order order, decimal lastMarketPrice)
        {
            throw new System.NotImplementedException();
        }
    }
}

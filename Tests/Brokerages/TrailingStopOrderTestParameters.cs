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
using QuantConnect.Interfaces;
using QuantConnect.Orders;

namespace QuantConnect.Tests.Brokerages
{
    public class TrailingStopOrderTestParameters : OrderTestParameters
    {
        private readonly decimal _highLimit;
        private readonly decimal _lowLimit;
        private readonly decimal _trailingAmount;
        private readonly bool _trailingAsPercentage;

        public TrailingStopOrderTestParameters(Symbol symbol, decimal highLimit, decimal lowLimit, decimal trailingAmount, bool trailingAsPercentage,
            IOrderProperties properties = null, OrderSubmissionData orderSubmissionData = null)
            : base(symbol, properties, orderSubmissionData)
        {
            _highLimit = highLimit;
            _lowLimit = lowLimit;
            _trailingAmount = trailingAmount;
            _trailingAsPercentage = trailingAsPercentage;
        }

        public override Order CreateShortOrder(decimal quantity)
        {
            return new TrailingStopOrder(Symbol, -Math.Abs(quantity), _lowLimit, _trailingAmount, _trailingAsPercentage, DateTime.UtcNow, properties: Properties)
            {
                Status = OrderStatus.New,
                OrderSubmissionData = OrderSubmissionData,
                PriceCurrency = GetSymbolProperties(Symbol).QuoteCurrency
            };
        }

        public override Order CreateLongOrder(decimal quantity)
        {
            return new TrailingStopOrder(Symbol, Math.Abs(quantity), _highLimit, _trailingAmount, _trailingAsPercentage, DateTime.UtcNow, properties: Properties)
            {
                Status = OrderStatus.New,
                OrderSubmissionData = OrderSubmissionData,
                PriceCurrency = GetSymbolProperties(Symbol).QuoteCurrency
            };
        }

        public override bool ModifyOrderToFill(IBrokerage brokerage, Order order, decimal lastMarketPrice) => false;

        public override OrderStatus ExpectedStatus => OrderStatus.Submitted;

        public override bool ExpectedCancellationResult => false;
    }
}

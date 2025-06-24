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
using QuantConnect.Orders;
using QuantConnect.Interfaces;

namespace QuantConnect.Tests.Brokerages
{
    public class LimitOrderTestParameters : OrderTestParameters
    {
        private readonly decimal _highLimit;
        private readonly decimal _lowLimit;
        private readonly decimal _priceModificationFactor;

        public LimitOrderTestParameters(Symbol symbol, decimal highLimit, decimal lowLimit, IOrderProperties properties = null,
            OrderSubmissionData orderSubmissionData = null, decimal priceModificationFactor = 1.02m)
            : base(symbol, properties, orderSubmissionData)
        {
            _highLimit = highLimit;
            _lowLimit = lowLimit;
            _priceModificationFactor = priceModificationFactor;
        }

        public override Order CreateShortOrder(decimal quantity)
        {
            return new LimitOrder(Symbol, -Math.Abs(quantity), _highLimit, DateTime.UtcNow, properties: Properties)
            {
                Status = OrderStatus.New,
                OrderSubmissionData = OrderSubmissionData,
                PriceCurrency = GetSymbolProperties(Symbol).QuoteCurrency
            };
        }

        public override Order CreateLongOrder(decimal quantity)
        {
            return new LimitOrder(Symbol, Math.Abs(quantity), _lowLimit, DateTime.UtcNow, properties: Properties)
            {
                Status = OrderStatus.New,
                OrderSubmissionData = OrderSubmissionData,
                PriceCurrency = GetSymbolProperties(Symbol).QuoteCurrency
            };
        }

        public override bool ModifyOrderToFill(IBrokerage brokerage, Order order, decimal lastMarketPrice)
        {
            // limit orders will process even if they go beyond the market price
            var limit = (LimitOrder) order;
            if (order.Quantity > 0)
            {
                // for limit buys we need to increase the limit price
                limit.LimitPrice = Math.Max(limit.LimitPrice * _priceModificationFactor, lastMarketPrice * _priceModificationFactor);
            }
            else
            {
                // for limit sells we need to decrease the limit price
                limit.LimitPrice = Math.Min(limit.LimitPrice / _priceModificationFactor, lastMarketPrice / _priceModificationFactor);
            }
            limit.LimitPrice = RoundPrice(order, limit.LimitPrice);
            return true;
        }

        // default limit orders will only be submitted, not filled
        public override OrderStatus ExpectedStatus => OrderStatus.Submitted;

        public override bool ExpectedCancellationResult => true;
    }

    // to be used with brokerages which do not support UpdateOrder
    public class NonUpdateableLimitOrderTestParameters : LimitOrderTestParameters
    {
        public NonUpdateableLimitOrderTestParameters(Symbol symbol, decimal highLimit, decimal lowLimit, IOrderProperties properties = null)
            : base(symbol, highLimit, lowLimit, properties)
        {
        }

        public override bool ModifyUntilFilled => false;
    }
}

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
using QuantConnect.Securities;

namespace QuantConnect.Tests.Brokerages
{
    public class LimitOrderTestParameters : OrderTestParameters
    {
        private readonly decimal _highLimit;
        private readonly decimal _lowLimit;

        public LimitOrderTestParameters(Symbol symbol, decimal highLimit, decimal lowLimit, IOrderProperties properties = null, OrderSubmissionData orderSubmissionData = null)
            : base(symbol, properties, orderSubmissionData)
        {
            _highLimit = highLimit;
            _lowLimit = lowLimit;
        }

        public override Order CreateShortOrder(decimal quantity)
        {
            return new LimitOrder(Symbol, -Math.Abs(quantity), _highLimit, DateTime.Now, properties: Properties)
            {
                Status = OrderStatus.New,
                OrderSubmissionData = OrderSubmissionData
            };
        }

        public override Order CreateLongOrder(decimal quantity)
        {
            return new LimitOrder(Symbol, Math.Abs(quantity), _lowLimit, DateTime.Now, properties: Properties)
            {
                Status = OrderStatus.New,
                OrderSubmissionData = OrderSubmissionData
            };
        }

        public override bool ModifyOrderToFill(IBrokerage brokerage, Order order, decimal lastMarketPrice)
        {
            // limit orders will process even if they go beyond the market price

            var symbolProperties = SPDB.GetSymbolProperties(order.Symbol.ID.Market, order.Symbol, order.SecurityType, order.PriceCurrency);
            var roundOffPlaces = symbolProperties.MinimumPriceVariation.GetDecimalPlaces();
            var limit = (LimitOrder) order;
            if (order.Quantity > 0)
            {
                // for limit buys we need to increase the limit price
                limit.LimitPrice = Math.Round(lastMarketPrice *1.02m, roundOffPlaces);
            }
            else
            {
                // for limit sells we need to decrease the limit price
                limit.LimitPrice = Math.Round(lastMarketPrice / 1.02m, roundOffPlaces);
            }
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

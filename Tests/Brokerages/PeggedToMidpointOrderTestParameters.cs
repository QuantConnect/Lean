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
    public class PeggedToMidpointOrderTestParameters : OrderTestParameters
    {
        private readonly decimal _limitPrice;
        private readonly decimal _limitPriceOffset;

        public PeggedToMidpointOrderTestParameters(
            Symbol symbol,
            decimal limitPrice,
            decimal limitPriceOffset = 0m,
            IOrderProperties properties = null,
            OrderSubmissionData orderSubmissionData = null)
            : base(symbol, properties, orderSubmissionData)
        {
            _limitPrice = limitPrice;
            _limitPriceOffset = limitPriceOffset;
        }

        public override Order CreateShortOrder(decimal quantity)
        {
            return new PeggedToMidpointOrder(Symbol, -Math.Abs(quantity), _limitPrice, _limitPriceOffset, DateTime.UtcNow,
                properties: Properties)
            {
                Status = OrderStatus.New,
                OrderSubmissionData = OrderSubmissionData,
                PriceCurrency = GetSymbolProperties(Symbol).QuoteCurrency
            };
        }

        public override Order CreateLongOrder(decimal quantity)
        {
            return new PeggedToMidpointOrder(Symbol, Math.Abs(quantity), _limitPrice, _limitPriceOffset, DateTime.UtcNow,
                properties: Properties)
            {
                Status = OrderStatus.New,
                OrderSubmissionData = OrderSubmissionData,
                PriceCurrency = GetSymbolProperties(Symbol).QuoteCurrency
            };
        }

        public override bool ModifyOrderToFill(IBrokerage brokerage, Order order, decimal lastMarketPrice)
        {
            var peg = (PeggedToMidpointOrder)order;
            var previous = peg.LimitPrice;
            // Adjust the limit price cap/floor toward market price to allow the peg to fill
            if (order.Quantity > 0)
            {
                peg.LimitPrice = Math.Max(peg.LimitPrice, lastMarketPrice);
            }
            else
            {
                peg.LimitPrice = Math.Min(peg.LimitPrice, lastMarketPrice);
            }
            return peg.LimitPrice != previous;
        }

        // PEG MID orders are submitted to the exchange and won't fill immediately in test
        public override OrderStatus ExpectedStatus => OrderStatus.Submitted;

        public override bool ExpectedCancellationResult => true;

        public override string ToString()
        {
            return $"{OrderType.PeggedToMidpoint}: {SecurityType}, {Symbol}";
        }
    }
}

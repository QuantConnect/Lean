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
using System.Collections.Generic;

namespace QuantConnect.Tests.Brokerages
{
    /// <summary>
    /// Provides test parameters and helper methods for creating combo limit orders, 
    /// such as bull call spreads and bear call spreads.
    /// </summary>
    public class ComboLimitOrderTestParameters
    {
        private readonly Leg _legOne;
        private readonly Leg _legTwo;
        private readonly decimal _askPrice;
        private readonly decimal _bidPrice;
        private readonly IOrderProperties _orderProperties;

        /// <summary>
        /// The status to expect when submitting this order in most test cases.
        /// </summary>
        public static OrderStatus ExpectedStatus => OrderStatus.Submitted;

        /// <summary>
        /// Initializes a new instance of the <see cref="ComboLimitOrderTestParameters"/> class.
        /// </summary>
        /// <param name="legOne">The first option leg of the combo order.</param>
        /// <param name="legTwo">The second option leg of the combo order.</param>
        /// <param name="askPrice">The ask price used when constructing bear call spreads.</param>
        /// <param name="bidPrice">The bid price used when constructing bull call spreads.</param>
        /// <param name="orderProperties">Optional order properties.</param>
        public ComboLimitOrderTestParameters(
            Leg legOne,
            Leg legTwo,
            decimal askPrice,
            decimal bidPrice,
            IOrderProperties orderProperties = null)
        {
            _legOne = legOne;
            _legTwo = legTwo;
            _askPrice = askPrice;
            _bidPrice = bidPrice;
            _orderProperties = orderProperties;
        }

        /// <summary>
        /// Creates a bull call spread using the provided quantity.
        /// </summary>
        /// <param name="quantity">The number of contracts for each leg.</param>
        /// <returns>A collection containing the two <see cref="ComboOrder"/> objects representing the spread.</returns>
        public IReadOnlyCollection<ComboOrder> CreateBullCallSpread(decimal quantity)
        {
            if (_legOne.Symbol.ID.StrikePrice >= _legTwo.Symbol.ID.StrikePrice)
            {
                throw new ArgumentException($"{nameof(CreateBullCallSpread)}: {_legOne.Symbol} must be less than {_legTwo.Symbol}");
            }

            var groupOrderManager = new GroupOrderManager(2, quantity, _bidPrice);

            return
            [
                CreateComboLimitOrder(_legOne, OrderDirection.Buy, groupOrderManager),
                CreateComboLimitOrder(_legTwo, OrderDirection.Sell, groupOrderManager)
            ];
        }

        /// <summary>
        /// Creates a bear call spread using the provided quantity. 
        /// </summary>
        /// <param name="quantity">The number of contracts for each leg.</param>
        /// <returns>A collection containing the two <see cref="ComboOrder"/> objects representing the spread.</returns>
        public IReadOnlyCollection<ComboOrder> CreateBearCallSpread(decimal quantity)
        {
            if (_legOne.Symbol.ID.StrikePrice >= _legTwo.Symbol.ID.StrikePrice)
            {
                throw new ArgumentException($"{nameof(CreateBullCallSpread)}: {_legOne.Symbol} must be less than {_legTwo.Symbol}");
            }

            var groupOrderManager = new GroupOrderManager(2, quantity, _askPrice);

            return
            [
                CreateComboLimitOrder(_legOne, OrderDirection.Sell, groupOrderManager),
                CreateComboLimitOrder(_legTwo, OrderDirection.Buy, groupOrderManager)
            ];
        }

        /// <summary>
        /// Returns a string representation of this instance for debugging and logging purposes.
        /// </summary>
        public override string ToString()
        {
            return $"{OrderType.ComboLimit}, {_legOne.Symbol.Value}, {_legTwo.Symbol.Value}";
        }

        /// <summary>
        /// Creates a <see cref="ComboLimitOrder"/> for the specified leg and direction.
        /// </summary>
        /// <param name="leg">The option leg to create the order for.</param>
        /// <param name="orderDirection">The direction of the order (Buy or Sell).</param>
        /// <param name="groupOrderManager">The <see cref="GroupOrderManager"/> responsible for tracking related combo orders.</param>
        /// <returns>A new <see cref="ComboLimitOrder"/> for the given leg.</returns>
        private ComboLimitOrder CreateComboLimitOrder(Leg leg, OrderDirection orderDirection, GroupOrderManager groupOrderManager)
        {
            var quantity = orderDirection switch
            {
                OrderDirection.Buy => Math.Abs(leg.Quantity),
                OrderDirection.Sell => decimal.Negate(Math.Abs(leg.Quantity)),
                _ => throw new ArgumentException($"{nameof(ComboLimitOrderTestParameters)}.{nameof(CreateComboLimitOrder)}: Not support Order Direction = {orderDirection}")
            };
            return new ComboLimitOrder(leg.Symbol, quantity, groupOrderManager.LimitPrice, DateTime.UtcNow, groupOrderManager, properties: _orderProperties)
            {
                Status = OrderStatus.New
            };
        }
    }
}

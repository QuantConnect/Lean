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
using System.Linq;
using QuantConnect.Orders;
using QuantConnect.Interfaces;
using QuantConnect.Securities;
using System.Collections.Generic;
using QuantConnect.Securities.Option;

namespace QuantConnect.Tests.Brokerages
{
    /// <summary>
    /// Provides test parameters and helper methods for creating combo limit orders.
    /// </summary>
    public class ComboLimitOrderTestParameters
    {
        private readonly OptionStrategy _strategy;
        private readonly decimal _askPrice;
        private readonly decimal _bidPrice;
        private readonly IOrderProperties _orderProperties;
        private readonly decimal _limitPriceAdjustmentFactor;
        private readonly SymbolProperties _strategyUnderlyingSymbolProperties;

        /// <summary>
        /// The status to expect when submitting this order in most test cases.
        /// </summary>
        public OrderStatus ExpectedStatus => OrderStatus.Submitted;

        /// <summary>
        /// The status to expect when cancelling this order
        /// </summary>
        public bool ExpectedCancellationResult => true;

        /// <summary>
        /// True to continue modifying the order until it is filled, false otherwise
        /// </summary>
        public bool ModifyUntilFilled => true;

        /// <summary>
        /// Initializes a new instance of the <see cref="ComboLimitOrderTestParameters"/> class.
        /// </summary>
        /// <param name="strategy">The Specification of the option strategy to trade.</param>
        /// <param name="askPrice">The ask price used when constructing bear call spreads.</param>
        /// <param name="bidPrice">The bid price used when constructing bull call spreads.</param>
        /// <param name="limitPriceAdjustmentFactor">
        /// A factor used to modify the limit price of the order. 
        /// For buy orders, the limit price is increased by this factor; 
        /// for sell orders, the limit price is decreased by this factor. 
        /// Default is 1.02 (2% adjustment).</param>
        /// <param name="orderProperties">Optional order properties to attach to each order.</param>
        public ComboLimitOrderTestParameters(
            OptionStrategy strategy,
            decimal askPrice,
            decimal bidPrice,
            decimal limitPriceAdjustmentFactor = 1.02m,
            IOrderProperties orderProperties = null)
        {
            _strategy = strategy;
            _askPrice = askPrice;
            _bidPrice = bidPrice;
            _orderProperties = orderProperties;
            _limitPriceAdjustmentFactor = limitPriceAdjustmentFactor;
            _strategyUnderlyingSymbolProperties = SymbolPropertiesDatabase.FromDataFolder().GetSymbolProperties(
                strategy.Underlying.ID.Market, strategy.Underlying, strategy.Underlying.SecurityType, Currencies.USD);
        }

        /// <summary>
        /// Creates long combo orders (buy) for the specified quantity.
        /// </summary>
        /// <param name="quantity">The quantity of the combo order to create.</param>
        /// <returns>A collection of combo orders representing a long position.</returns>
        public IReadOnlyCollection<ComboOrder> CreateLong(decimal quantity)
        {
            return CreateOrders(quantity, _bidPrice);
        }

        /// <summary>
        /// Creates short combo orders (sell) for the specified quantity.
        /// </summary>
        /// <param name="quantity">The quantity of the combo order to create (will be negated internally).</param>
        /// <returns>A collection of combo orders representing a short position.</returns>
        public IReadOnlyCollection<ComboOrder> CreateShort(decimal quantity)
        {
            return CreateOrders(decimal.Negate(Math.Abs(quantity)), _askPrice);
        }

        /// <summary>
        /// Creates combo orders for a given quantity and limit price.
        /// </summary>
        /// <param name="quantity">The quantity of each leg in the combo order.</param>
        /// <param name="limitPrice">The limit price to apply to the combo order.</param>
        /// <returns>A collection of <see cref="ComboOrder"/> instances for all legs.</returns>
        private IReadOnlyCollection<ComboOrder> CreateOrders(decimal quantity, decimal limitPrice)
        {
            var targetOption = _strategy.CanonicalOption?.Canonical.ID.Symbol;

            var legs = new List<Leg>(_strategy.UnderlyingLegs);

            foreach (var optionLeg in _strategy.OptionLegs)
            {
                var option = Symbol.CreateOption(
                    _strategy.Underlying,
                    targetOption,
                    _strategy.Underlying.ID.Market,
                    _strategy.Underlying.SecurityType.DefaultOptionStyle(),
                    optionLeg.Right,
                    optionLeg.Strike,
                    optionLeg.Expiration);

                legs.Add(new Leg { Symbol = option, OrderPrice = optionLeg.OrderPrice, Quantity = optionLeg.Quantity });
            }

            var groupOrderManager = new GroupOrderManager(legs.Count, quantity, limitPrice);

            return legs.Select(l => CreateComboLimitOrder(l, groupOrderManager)).ToList();
        }

        /// <summary>
        /// Modifies the limit price of an order to increase the likelihood of being filled.
        /// </summary>
        /// <param name="brokerage">The brokerage instance to apply the order update.</param>
        /// <param name="order">The order to modify.</param>
        /// <param name="lastMarketPrice">The last observed market price of the order's underlying instrument.</param>
        /// <returns>Always returns true.</returns>
        public virtual bool ModifyOrderToFill(IBrokerage brokerage, Order order, decimal lastMarketPrice)
        {
            var groupOrderManager = order.GroupOrderManager;
            var limitPrice = groupOrderManager.LimitPrice;
            // limit orders will process even if they go beyond the market price
            switch (groupOrderManager.Direction)
            {
                case OrderDirection.Buy:
                    limitPrice = Math.Max(limitPrice * _limitPriceAdjustmentFactor, lastMarketPrice * _limitPriceAdjustmentFactor);
                    break;
                case OrderDirection.Sell:
                    limitPrice = Math.Min(limitPrice / _limitPriceAdjustmentFactor, lastMarketPrice / _limitPriceAdjustmentFactor);
                    break;
            }

            limitPrice = RoundPrice(limitPrice);

            order.ApplyUpdateOrderRequest(new UpdateOrderRequest(DateTime.UtcNow, order.Id, new() { LimitPrice = limitPrice }));

            return true;
        }

        /// <summary>
        /// Returns a string representation of this instance for debugging and logging purposes.
        /// </summary>
        public override string ToString()
        {
            return $"{OrderType.ComboLimit}: {_strategy.Name} ({_strategy.CanonicalOption.Value})";
        }

        /// <summary>
        /// Creates a <see cref="ComboLimitOrder"/> for the specified leg and direction.
        /// </summary>
        /// <param name="leg">The option leg to create the order for.</param>
        /// <param name="groupOrderManager">The <see cref="GroupOrderManager"/> responsible for tracking related combo orders.</param>
        /// <returns>A new <see cref="ComboLimitOrder"/> for the given leg.</returns>
        private ComboLimitOrder CreateComboLimitOrder(Leg leg, GroupOrderManager groupOrderManager)
        {
            return new ComboLimitOrder(
                leg.Symbol,
                ((decimal)leg.Quantity).GetOrderLegGroupQuantity(groupOrderManager),
                groupOrderManager.LimitPrice,
                DateTime.UtcNow,
                groupOrderManager,
                properties: _orderProperties)
            {
                Status = OrderStatus.New,
                PriceCurrency = _strategyUnderlyingSymbolProperties.QuoteCurrency
            };
        }

        /// <summary>
        /// Rounds the specified price according to the minimum price variation of the underlying symbol.
        /// </summary>
        /// <param name="price">The price to round.</param>
        /// <returns>The rounded price.</returns>
        private decimal RoundPrice(decimal price)
        {
            var roundOffPlaces = _strategyUnderlyingSymbolProperties.MinimumPriceVariation.GetDecimalPlaces();
            return Math.Round(price / roundOffPlaces) * roundOffPlaces;
        }
    }
}

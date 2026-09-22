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
using QuantConnect.Interfaces;
using System.Collections.Generic;

namespace QuantConnect.Orders
{
    /// <summary>
    /// Creates <see cref="SubmitOrderRequest"/> for an algorithm to be submitted later, so they can be composed into contingent orders before:
    /// an order can trigger others once it fills (<see cref="SubmitOrderRequest.Triggers(SubmitOrderRequest[])"/>), which can in turn
    /// cancel (<see cref="OneCancelsOther(SubmitOrderRequest[])"/>) or update (<see cref="OneUpdatesOther(SubmitOrderRequest[])"/>) each other,
    /// see <see cref="SubmitOrderRequest.Bracket"/>. The order id and the contingency set id of the requests are assigned once they are submitted
    /// </summary>
    public class OrderFactory
    {
        private readonly IAlgorithm _algorithm;

        /// <summary>
        /// Creates a new instance for the given algorithm, which provides the time and default order properties of the requests
        /// </summary>
        /// <param name="algorithm">The algorithm instance</param>
        public OrderFactory(IAlgorithm algorithm)
        {
            _algorithm = algorithm;
        }

        /// <summary>
        /// Market order request
        /// </summary>
        public SubmitOrderRequest MarketOrder(Symbol symbol, decimal quantity, bool asynchronous = false, string tag = "", IOrderProperties orderProperties = null)
        {
            return Create(OrderType.Market, symbol, quantity, 0, 0, 0, 0, false, asynchronous, tag, orderProperties);
        }

        /// <summary>
        /// Market on open order request
        /// </summary>
        public SubmitOrderRequest MarketOnOpenOrder(Symbol symbol, decimal quantity, bool asynchronous = false, string tag = "", IOrderProperties orderProperties = null)
        {
            return Create(OrderType.MarketOnOpen, symbol, quantity, 0, 0, 0, 0, false, asynchronous, tag, orderProperties);
        }

        /// <summary>
        /// Market on close order request
        /// </summary>
        public SubmitOrderRequest MarketOnCloseOrder(Symbol symbol, decimal quantity, bool asynchronous = false, string tag = "", IOrderProperties orderProperties = null)
        {
            return Create(OrderType.MarketOnClose, symbol, quantity, 0, 0, 0, 0, false, asynchronous, tag, orderProperties);
        }

        /// <summary>
        /// Limit order request
        /// </summary>
        public SubmitOrderRequest LimitOrder(Symbol symbol, decimal quantity, decimal limitPrice, bool asynchronous = false, string tag = "",
            IOrderProperties orderProperties = null)
        {
            return Create(OrderType.Limit, symbol, quantity, 0, limitPrice, 0, 0, false, asynchronous, tag, orderProperties);
        }

        /// <summary>
        /// Stop market order request
        /// </summary>
        public SubmitOrderRequest StopMarketOrder(Symbol symbol, decimal quantity, decimal stopPrice, bool asynchronous = false, string tag = "",
            IOrderProperties orderProperties = null)
        {
            return Create(OrderType.StopMarket, symbol, quantity, stopPrice, 0, 0, 0, false, asynchronous, tag, orderProperties);
        }

        /// <summary>
        /// Stop limit order request
        /// </summary>
        public SubmitOrderRequest StopLimitOrder(Symbol symbol, decimal quantity, decimal stopPrice, decimal limitPrice, bool asynchronous = false, string tag = "",
            IOrderProperties orderProperties = null)
        {
            return Create(OrderType.StopLimit, symbol, quantity, stopPrice, limitPrice, 0, 0, false, asynchronous, tag, orderProperties);
        }

        /// <summary>
        /// Trailing stop order request. The initial stop price is calculated based on the market price at the
        /// time the order starts working: once submitted, or once triggered for an order triggered by another
        /// </summary>
        public SubmitOrderRequest TrailingStopOrder(Symbol symbol, decimal quantity, decimal trailingAmount, bool trailingAsPercentage, bool asynchronous = false,
            string tag = "", IOrderProperties orderProperties = null)
        {
            return Create(OrderType.TrailingStop, symbol, quantity, 0, 0, 0, trailingAmount, trailingAsPercentage, asynchronous, tag, orderProperties);
        }

        /// <summary>
        /// Trailing stop order request with an initial stop price
        /// </summary>
        public SubmitOrderRequest TrailingStopOrder(Symbol symbol, decimal quantity, decimal stopPrice, decimal trailingAmount, bool trailingAsPercentage,
            bool asynchronous = false, string tag = "", IOrderProperties orderProperties = null)
        {
            return Create(OrderType.TrailingStop, symbol, quantity, stopPrice, 0, 0, trailingAmount, trailingAsPercentage, asynchronous, tag, orderProperties);
        }

        /// <summary>
        /// Limit if touched order request
        /// </summary>
        public SubmitOrderRequest LimitIfTouchedOrder(Symbol symbol, decimal quantity, decimal triggerPrice, decimal limitPrice, bool asynchronous = false,
            string tag = "", IOrderProperties orderProperties = null)
        {
            return Create(OrderType.LimitIfTouched, symbol, quantity, 0, limitPrice, triggerPrice, 0, false, asynchronous, tag, orderProperties);
        }

        /// <summary>
        /// Option exercise order request
        /// </summary>
        public SubmitOrderRequest ExerciseOption(Symbol optionSymbol, decimal quantity, bool asynchronous = false, string tag = "", IOrderProperties orderProperties = null)
        {
            if (optionSymbol != null && !optionSymbol.SecurityType.IsOption())
            {
                throw new ArgumentException($"Only option contracts can be exercised: {optionSymbol}", nameof(optionSymbol));
            }
            // the quantity indicates the change in holdings quantity, therefore manual exercise quantities must be negative
            return Create(OrderType.OptionExercise, optionSymbol, -Math.Abs(quantity), 0, 0, 0, 0, false, asynchronous, tag, orderProperties);
        }

        /// <summary>
        /// Combo market order requests, one per leg. The legs are a single unit: composed and submitted together
        /// </summary>
        public List<SubmitOrderRequest> ComboMarketOrder(List<Leg> legs, int quantity, bool asynchronous = false, string tag = "", IOrderProperties orderProperties = null)
        {
            if (legs.Any(leg => leg.OrderPrice != null && leg.OrderPrice != 0))
            {
                throw new ArgumentException("ComboMarketOrder does not support limit prices for individual legs, please use ComboLegLimitOrder");
            }
            return Combo(OrderType.ComboMarket, legs, quantity, 0, asynchronous, tag, orderProperties);
        }

        /// <summary>
        /// Combo limit order requests, one per leg, with a single limit price for the combo
        /// </summary>
        public List<SubmitOrderRequest> ComboLimitOrder(List<Leg> legs, int quantity, decimal limitPrice, bool asynchronous = false, string tag = "",
            IOrderProperties orderProperties = null)
        {
            if (limitPrice == 0)
            {
                throw new ArgumentException("ComboLimitOrder requires a limit price");
            }

            if (legs.Any(leg => leg.OrderPrice != null && leg.OrderPrice != 0))
            {
                throw new ArgumentException("ComboLimitOrder does not support limit prices for individual legs");
            }
            return Combo(OrderType.ComboLimit, legs, quantity, limitPrice, asynchronous, tag, orderProperties);
        }

        /// <summary>
        /// Combo leg limit order requests, one per leg, each leg with its own limit price
        /// </summary>
        public List<SubmitOrderRequest> ComboLegLimitOrder(List<Leg> legs, int quantity, bool asynchronous = false, string tag = "", IOrderProperties orderProperties = null)
        {
            if (legs.Any(leg => leg.OrderPrice == null || leg.OrderPrice == 0))
            {
                throw new ArgumentException("ComboLegLimitOrder requires a limit price for each leg");
            }
            return Combo(OrderType.ComboLegLimit, legs, quantity, 0, asynchronous, tag, orderProperties);
        }

        /// <summary>
        /// Option strategy order requests, a combo market order of the strategy legs
        /// </summary>
        public List<SubmitOrderRequest> OptionStrategyOrder(Securities.Option.OptionStrategy strategy, int quantity, bool asynchronous = false, string tag = "",
            IOrderProperties orderProperties = null)
        {
            // Make sure the strategy is initialized, that is, canonical and leg symbols are set.
            strategy.SetSymbols();

            // setting up the tag text for all orders of one strategy
            tag ??= $"{strategy.Name} ({quantity.ToStringInvariant()})";

            var legs = strategy.UnderlyingLegs.Cast<Leg>().Concat(strategy.OptionLegs).ToList();
            return Combo(OrderType.ComboMarket, legs, quantity, 0, asynchronous, tag, orderProperties);
        }

        /// <summary>
        /// Relates the orders so that once one of them fills, even partially, the rest are canceled (One Cancels Other/All)
        /// </summary>
        /// <param name="orders">The orders to relate</param>
        /// <returns>The same orders, so they can be submitted or triggered by another order</returns>
        public List<SubmitOrderRequest> OneCancelsOther(params SubmitOrderRequest[] orders)
        {
            return OneCancelsOther((IEnumerable<SubmitOrderRequest>)orders);
        }

        /// <summary>
        /// Relates the orders so that once one of them fills, even partially, the rest are canceled (One Cancels Other/All)
        /// </summary>
        /// <param name="orders">The orders to relate, including all the legs of combo orders</param>
        /// <returns>The same orders, so they can be submitted or triggered by another order</returns>
        public List<SubmitOrderRequest> OneCancelsOther(IEnumerable<SubmitOrderRequest> orders)
        {
            var members = orders?.ToList();
            OrderContingency.Relate(ContingencyType.OneCancelsOther, members);
            return members;
        }

        /// <summary>
        /// Relates the orders so that once one of them partially fills the remaining quantity of the rest is reduced proportionally,
        /// and canceled once it completely fills (One Updates Other)
        /// </summary>
        /// <param name="orders">The orders to relate</param>
        /// <returns>The same orders, so they can be submitted or triggered by another order</returns>
        public List<SubmitOrderRequest> OneUpdatesOther(params SubmitOrderRequest[] orders)
        {
            return OneUpdatesOther((IEnumerable<SubmitOrderRequest>)orders);
        }

        /// <summary>
        /// Relates the orders so that once one of them partially fills the remaining quantity of the rest is reduced proportionally,
        /// and canceled once it completely fills (One Updates Other)
        /// </summary>
        /// <param name="orders">The orders to relate, including all the legs of combo orders</param>
        /// <returns>The same orders, so they can be submitted or triggered by another order</returns>
        public List<SubmitOrderRequest> OneUpdatesOther(IEnumerable<SubmitOrderRequest> orders)
        {
            var members = orders?.ToList();
            OrderContingency.Relate(ContingencyType.OneUpdatesOther, members);
            return members;
        }

        private SubmitOrderRequest Create(OrderType type, Symbol symbol, decimal quantity, decimal stopPrice, decimal limitPrice, decimal triggerPrice,
            decimal trailingAmount, bool trailingAsPercentage, bool asynchronous, string tag, IOrderProperties orderProperties)
        {
            symbol = GetCurrentSymbol(symbol);
            return new SubmitOrderRequest(type, symbol.SecurityType, symbol, quantity, stopPrice, limitPrice, triggerPrice, trailingAmount, trailingAsPercentage,
                _algorithm.UtcTime, tag, orderProperties ?? _algorithm.DefaultOrderProperties?.Clone(), asynchronous: asynchronous);
        }

        /// <summary>
        /// Gets the current symbol of the security, which can have been renamed since the given one was created
        /// </summary>
        private Symbol GetCurrentSymbol(Symbol symbol)
        {
            return _algorithm.Securities.TryGetValue(symbol, out var security) ? security.Symbol : symbol;
        }

        private List<SubmitOrderRequest> Combo(OrderType type, List<Leg> legs, decimal quantity, decimal limitPrice, bool asynchronous, string tag,
            IOrderProperties orderProperties)
        {
            if (legs == null || legs.Count == 0 || legs.Any(leg => leg == null))
            {
                throw new ArgumentException("Expected at least one leg", nameof(legs));
            }

            var greatestCommonDivisor = Math.Abs(legs.Select(leg => leg.Quantity).GreatestCommonDivisor());
            if (greatestCommonDivisor != 1)
            {
                throw new ArgumentException(
                    "The global combo quantity should be used to increase or reduce the size of the order, " +
                    "while the leg quantities should be used to specify the ratio of the order. " +
                    "The combo order quantities should be reduced " +
                    $"from {quantity}x({string.Join(", ", legs.Select(leg => $"{leg.Quantity} {leg.Symbol}"))}) " +
                    $"to {quantity * greatestCommonDivisor}x({string.Join(", ", legs.Select(leg => $"{leg.Quantity / greatestCommonDivisor} {leg.Symbol}"))}).");
            }

            // the group id is set once submitted
            var groupOrderManager = new GroupOrderManager(legs.Count, quantity, limitPrice);
            var requests = new List<SubmitOrderRequest>(legs.Count);
            foreach (var leg in legs)
            {
                var legType = type;
                var legLimitPrice = limitPrice;
                if (leg.OrderPrice.HasValue)
                {
                    // limit price per leg
                    legLimitPrice = leg.OrderPrice.Value;
                    legType = OrderType.ComboLegLimit;
                }

                var symbol = GetCurrentSymbol(leg.Symbol);
                requests.Add(new SubmitOrderRequest(legType, symbol.SecurityType, symbol, ((decimal)leg.Quantity).GetOrderLegGroupQuantity(groupOrderManager),
                    0, legLimitPrice, 0, 0, false, _algorithm.UtcTime, tag, orderProperties ?? _algorithm.DefaultOrderProperties?.Clone(), groupOrderManager, asynchronous));
            }
            return requests;
        }
    }
}

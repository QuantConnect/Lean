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
using QuantConnect.Logging;
using QuantConnect.Orders;

namespace QuantConnect.Tests.Brokerages
{
    /// <summary>
    /// Provides configuration parameters used to test <see cref="TrailingStopOrder"/> behavior,
    /// including price bounds, trailing values, and modification thresholds.
    /// </summary>
    public class TrailingStopOrderTestParameters : OrderTestParameters
    {
        private readonly decimal _highLimit;
        private readonly decimal _lowLimit;

        /// <summary>
        /// Whether <see cref="_trailingAmount"/> is a percentage (<c>true</c>) or absolute value (<c>false</c>).
        /// </summary>
        private readonly decimal _trailingAmount;

        /// <summary>
        /// Factor used to adjust the trailing stop during fill simulations (e.g., 0.001 = 0.1%, 1 = $1).
        /// </summary>
        private readonly bool _trailingAsPercentage;

        /// <summary>
        ///  The offset amount used when adjusting the trailing stop order during fill simulations.
        /// Typically a small value such as <c>0.001m</c> (for 0.1%) or <c>1m</c> (for $1).
        /// </summary>
        private readonly decimal _trailingOffsetAmount;

        /// <summary>
        /// Initializes a new instance of the <see cref="TrailingStopOrderTestParameters"/> class.
        /// </summary>
        /// <param name="symbol">The symbol associated with the order under test.</param>
        /// <param name="highLimit">The upper price boundary for the simulated test environment.</param>
        /// <param name="lowLimit">The lower price boundary for the simulated test environment.</param>
        /// <param name="trailingAmount">The trailing stop amount, expressed as either a fixed offset or percentage.</param>
        /// <param name="trailingAsPercentage">If <c>true</c>, the <paramref name="trailingAmount"/> is a percentage; otherwise, it’s an absolute price offset.</param>
        /// <param name="properties">Optional order properties used to customize the test order (such as time in force or brokerage-specific parameters).</param>
        /// <param name="orderSubmissionData">Optional submission data containing fill and price context at order creation time.</param>
        /// <param name="trailingOffsetAmount">
        /// Optional offset amount applied when simulating trailing stop order modifications during tests.
        /// Defaults to a typical small value such as <c>0.001m</c> (0.1%) or <c>1m</c> ($1), depending on <paramref name="trailingAsPercentage"/>.
        /// </param>
        public TrailingStopOrderTestParameters(Symbol symbol, decimal highLimit, decimal lowLimit, decimal trailingAmount, bool trailingAsPercentage,
            IOrderProperties properties = null, OrderSubmissionData orderSubmissionData = null, decimal? trailingOffsetAmount = null)
            : base(symbol, properties, orderSubmissionData)
        {
            _highLimit = highLimit;
            _lowLimit = lowLimit;
            _trailingAmount = trailingAmount;
            _trailingAsPercentage = trailingAsPercentage;
            // trailingAsPercentage ? 0.001m (0.1%) or 1$
            _trailingOffsetAmount = trailingOffsetAmount ?? (trailingAsPercentage ? 0.001m : 0.5m);
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

        public override bool ModifyOrderToFill(IBrokerage brokerage, Order order, decimal lastMarketPrice)
        {
            var trailingStopOrder = order as TrailingStopOrder;

            if (trailingStopOrder.TrailingAmount == _trailingOffsetAmount)
            {
                Log.Trace($"{nameof(TrailingStopOrderTestParameters)}.{nameof(ModifyOrderToFill)}: Trailing amount already equals modification factor for order {trailingStopOrder.Id}.");
                return false;
            }

            if (!TrailingStopOrder.TryUpdateStopPrice(lastMarketPrice,
                trailingStopOrder.StopPrice,
                _trailingOffsetAmount,
                trailingStopOrder.TrailingAsPercentage,
                trailingStopOrder.Direction,
                out var updatedStopPrice))
            {
                Log.Error($"Failed to compute updated stop price for order {trailingStopOrder.Id}. " +
                    $"Inputs: LastMarketPrice={lastMarketPrice}, CurrentStopPrice={trailingStopOrder.StopPrice}, " +
                    $"TrailingModificationFactor={_trailingOffsetAmount}, IsPercentage={trailingStopOrder.TrailingAsPercentage}, Direction={trailingStopOrder.Direction}.");
                return false;
            }

            var updateFields = new UpdateOrderFields() { StopPrice = updatedStopPrice, TrailingAmount = _trailingOffsetAmount };

            ApplyUpdateOrderRequest(order, updateFields);

            return true;
        }

        public override OrderStatus ExpectedStatus => OrderStatus.Submitted;

        public override bool ExpectedCancellationResult => true;

        public override string ToString()
        {
            return $"{OrderType.TrailingStop}: {SecurityType}, {Symbol}";
        }
    }
}

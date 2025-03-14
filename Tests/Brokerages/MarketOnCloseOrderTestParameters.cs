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

namespace QuantConnect.Tests.Brokerages;

/// <summary>
/// Represents test parameters for a market-on-close order.
/// </summary>
public class MarketOnCloseOrderTestParameters : MarketOrderTestParameters
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MarketOnCloseOrderTestParameters"/> class.
    /// </summary>
    /// <param name="symbol">The trading symbol associated with the order.</param>
    /// <param name="properties">Optional order properties.</param>
    /// <param name="orderSubmissionData">Optional order submission data.</param>
    public MarketOnCloseOrderTestParameters(Symbol symbol, IOrderProperties properties = null, OrderSubmissionData orderSubmissionData = null)
        : base(symbol, properties, orderSubmissionData)
    {
    }

    /// <summary>
    /// Creates a short market-on-close order.
    /// </summary>
    /// <param name="quantity">The quantity to sell (must be a positive value).</param>
    /// <returns>A new <see cref="MarketOnCloseOrder"/> representing a short order.</returns>
    public override Order CreateShortOrder(decimal quantity)
    {
        return new MarketOnCloseOrder(Symbol, -Math.Abs(quantity), DateTime.UtcNow, properties: Properties)
        {
            OrderSubmissionData = OrderSubmissionData
        };
    }

    /// <summary>
    /// Creates a long market-on-close order.
    /// </summary>
    /// <param name="quantity">The quantity to buy (must be a positive value).</param>
    /// <returns>A new <see cref="MarketOnCloseOrder"/> representing a long order.</returns>
    public override Order CreateLongOrder(decimal quantity)
    {
        return new MarketOnCloseOrder(Symbol, Math.Abs(quantity), DateTime.UtcNow, properties: Properties)
        {
            OrderSubmissionData = OrderSubmissionData
        };
    }

    /// <summary>
    /// Gets the expected status of the market-on-close order during testing.
    /// </summary>
    /// <remarks>
    /// Returns <see cref="OrderStatus.Submitted"/> because this order type is tested 
    /// when the market is open, meaning it remains in the submitted state until market close.
    /// </remarks>
    public override OrderStatus ExpectedStatus => OrderStatus.Submitted;

    /// <summary>
    /// Gets a value indicating whether cancellation is expected for this order type.
    /// </summary>
    /// <remarks>
    /// Always returns <c>true</c> because market-on-close orders can be canceled before execution.
    /// </remarks>
    public override bool ExpectedCancellationResult => true;
}

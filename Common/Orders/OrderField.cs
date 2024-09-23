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

namespace QuantConnect.Orders
{
    /// <summary>
    /// Specifies an order field that does not apply to all order types
    /// </summary>
    public enum OrderField
    {
        /// <summary>
        /// The limit price for a <see cref="LimitOrder"/>, <see cref="StopLimitOrder"/> or <see cref="LimitIfTouchedOrder"/> (0)
        /// </summary>
        LimitPrice,

        /// <summary>
        /// The stop price for stop orders (<see cref="StopMarketOrder"/>, <see cref="StopLimitOrder"/>) (1)
        /// </summary>
        StopPrice,

        /// <summary>
        /// The trigger price for a <see cref="LimitIfTouchedOrder"/> (2)
        /// </summary>
        TriggerPrice,

        /// <summary>
        /// The trailing amount for a <see cref="TrailingStopOrder"/> (3)
        /// </summary>
        TrailingAmount,

        /// <summary>
        /// Whether the trailing amount for a <see cref="TrailingStopOrder"/> is a percentage or an absolute currency value (4)
        /// </summary>
        TrailingAsPercentage,

        /// <summary>
        /// The limit offset amount for a <see cref="TrailingStopLimitOrder"/> (5)
        /// </summary>
        LimitOffset
    }
}

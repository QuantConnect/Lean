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

using System.Collections.Generic;

namespace QuantConnect.Orders
{
    /// <summary>
    /// Provides extension methods for the <see cref="Order"/> class and for the <see cref="OrderStatus"/> enumeration
    /// </summary>
    public static class OrderExtensions
    {
        /// <summary>
        /// Determines if the specified status is in a closed state.
        /// </summary>
        /// <param name="status">The status to check</param>
        /// <returns>True if the status is <see cref="OrderStatus.Filled"/>, <see cref="OrderStatus.Canceled"/>, or <see cref="OrderStatus.Invalid"/></returns>
        public static bool IsClosed(this OrderStatus status)
        {
            return status == OrderStatus.Filled
                || status == OrderStatus.Canceled
                || status == OrderStatus.Invalid;
        }

        /// <summary>
        /// Determines if the specified status is in an open state.
        /// </summary>
        /// <param name="status">The status to check</param>
        /// <returns>True if the status is not <see cref="OrderStatus.Filled"/>, <see cref="OrderStatus.Canceled"/>, or <see cref="OrderStatus.Invalid"/></returns>
        public static bool IsOpen(this OrderStatus status)
        {
            return !status.IsClosed();
        }

        /// <summary>
        /// Determines if the specified status is a fill, that is, <see cref="OrderStatus.Filled"/>
        /// order <see cref="OrderStatus.PartiallyFilled"/>
        /// </summary>
        /// <param name="status">The status to check</param>
        /// <returns>True if the status is <see cref="OrderStatus.Filled"/> or <see cref="OrderStatus.PartiallyFilled"/>, false otherwise</returns>
        public static bool IsFill(this OrderStatus status)
        {
            return status == OrderStatus.Filled || status == OrderStatus.PartiallyFilled;
        }

        /// <summary>
        /// Determines whether or not the specified order is a limit order
        /// </summary>
        /// <param name="orderType">The order to check</param>
        /// <returns>True if the order is a limit order, false otherwise</returns>
        public static bool IsLimitOrder(this OrderType orderType)
        {
            return orderType == OrderType.Limit
                || orderType == OrderType.StopLimit
                || orderType == OrderType.LimitIfTouched;
        }

        /// <summary>
        /// Determines whether or not the specified order is a stop order
        /// </summary>
        /// <param name="orderType">The order to check</param>
        /// <returns>True if the order is a stop order, false otherwise</returns>
        public static bool IsStopOrder(this OrderType orderType)
        {
            return orderType == OrderType.StopMarket || orderType == OrderType.StopLimit || orderType == OrderType.TrailingStop;
        }
    }
}

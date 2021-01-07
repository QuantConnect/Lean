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
    /// Type of the order: market, limit or stop
    /// </summary>
    public enum OrderType
    {
        /// <summary>
        /// Market Order Type
        /// </summary>
        Market,

        /// <summary>
        /// Limit Order Type
        /// </summary>
        Limit,

        /// <summary>
        /// Stop Market Order Type - Fill at market price when break target price
        /// </summary>
        StopMarket,

        /// <summary>
        /// Stop limit order type - trigger fill once pass the stop price; but limit fill to limit price.
        /// </summary>
        StopLimit,

        /// <summary>
        /// Market on open type - executed on exchange open
        /// </summary>
        MarketOnOpen,

        /// <summary>
        /// Market on close type - executed on exchange close
        /// </summary>
        MarketOnClose,

        /// <summary>
        /// Option Exercise Order Type
        /// </summary>
        OptionExercise,
        
        /// <summary>
        ///  Limit if Touched Order Type - a limit order to be placed after first reaching a trigger value.
        /// </summary>
        LimitIfTouched
    }

    /// <summary>
    /// Direction of the order
    /// </summary>
    public enum OrderDirection
    {
        /// <summary>
        /// Buy Order
        /// </summary>
        Buy,

        /// <summary>
        /// Sell Order
        /// </summary>
        Sell,

        /// <summary>
        /// Default Value - No Order Direction
        /// </summary>
        /// <remarks>
        /// Unfortunately this does not have a value of zero because
        /// there are backtests saved that reference the values in this order
        /// </remarks>
        Hold
    }

    /// <summary>
    /// Fill status of the order class.
    /// </summary>
    public enum OrderStatus
    {
        /// <summary>
        /// New order pre-submission to the order processor.
        /// </summary>
        New = 0,

        /// <summary>
        /// Order submitted to the market
        /// </summary>
        Submitted = 1,

        /// <summary>
        /// Partially filled, In Market Order.
        /// </summary>
        PartiallyFilled = 2,

        /// <summary>
        /// Completed, Filled, In Market Order.
        /// </summary>
        Filled = 3,

        /// <summary>
        /// Order cancelled before it was filled
        /// </summary>
        Canceled = 5,

        /// <summary>
        /// No Order State Yet
        /// </summary>
        None = 6,

        /// <summary>
        /// Order invalidated before it hit the market (e.g. insufficient capital)..
        /// </summary>
        Invalid = 7,

        /// <summary>
        /// Order waiting for confirmation of cancellation
        /// </summary>
        CancelPending = 8,

        /// <summary>
        /// Order update submitted to the market
        /// </summary>
        UpdateSubmitted = 9
    }
}
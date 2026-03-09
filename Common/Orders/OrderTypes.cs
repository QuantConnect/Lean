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
        /// Market Order Type (0)
        /// </summary>
        Market,

        /// <summary>
        /// Limit Order Type (1)
        /// </summary>
        Limit,

        /// <summary>
        /// Stop Market Order Type - Fill at market price when break target price (2)
        /// </summary>
        StopMarket,

        /// <summary>
        /// Stop limit order type - trigger fill once pass the stop price; but limit fill to limit price (3)
        /// </summary>
        StopLimit,

        /// <summary>
        /// Market on open type - executed on exchange open (4)
        /// </summary>
        MarketOnOpen,

        /// <summary>
        /// Market on close type - executed on exchange close (5)
        /// </summary>
        MarketOnClose,

        /// <summary>
        /// Option Exercise Order Type (6)
        /// </summary>
        OptionExercise,

        /// <summary>
        ///  Limit if Touched Order Type - a limit order to be placed after first reaching a trigger value (7)
        /// </summary>
        LimitIfTouched,

        /// <summary>
        ///  Combo Market Order Type - (8)
        /// </summary>
        ComboMarket,

        /// <summary>
        ///  Combo Limit Order Type - (9)
        /// </summary>
        ComboLimit,

        /// <summary>
        ///  Combo Leg Limit Order Type - (10)
        /// </summary>
        ComboLegLimit,

        /// <summary>
        /// Trailing Stop Order Type - (11)
        /// </summary>
        TrailingStop
    }

    /// <summary>
    /// Direction of the order
    /// </summary>
    public enum OrderDirection
    {
        /// <summary>
        /// Buy Order (0)
        /// </summary>
        Buy,

        /// <summary>
        /// Sell Order (1)
        /// </summary>
        Sell,

        /// <summary>
        /// Default Value - No Order Direction (2)
        /// </summary>
        /// <remarks>
        /// Unfortunately this does not have a value of zero because
        /// there are backtests saved that reference the values in this order
        /// </remarks>
        Hold
    }

    /// <summary>
    /// Position of the order
    /// </summary>
    public enum OrderPosition
    {
        /// <summary>
        /// Indicates the buy order will result in a long position, starting either from zero or an existing long position (0)
        /// </summary>
        BuyToOpen,

        /// <summary>
        /// Indicates the buy order is starting from an existing short position, resulting in a closed or long position (1)
        /// </summary>
        BuyToClose,

        /// <summary>
        /// Indicates the sell order will result in a short position, starting either from zero or an existing short position (2)
        /// </summary>
        SellToOpen,

        /// <summary>
        /// Indicates the sell order is starting from an existing long position, resulting in a closed or short position (3)
        /// </summary>
        SellToClose,
    }

    /// <summary>
    /// Fill status of the order class.
    /// </summary>
    public enum OrderStatus
    {
        /// <summary>
        /// New order pre-submission to the order processor (0)
        /// </summary>
        New = 0,

        /// <summary>
        /// Order submitted to the market (1)
        /// </summary>
        Submitted = 1,

        /// <summary>
        /// Partially filled, In Market Order (2)
        /// </summary>
        PartiallyFilled = 2,

        /// <summary>
        /// Completed, Filled, In Market Order (3)
        /// </summary>
        Filled = 3,

        /// <summary>
        /// Order cancelled before it was filled (5)
        /// </summary>
        Canceled = 5,

        /// <summary>
        /// No Order State Yet (6)
        /// </summary>
        None = 6,

        /// <summary>
        /// Order invalidated before it hit the market (e.g. insufficient capital) (7)
        /// </summary>
        Invalid = 7,

        /// <summary>
        /// Order waiting for confirmation of cancellation (8)
        /// </summary>
        CancelPending = 8,

        /// <summary>
        /// Order update submitted to the market (9)
        /// </summary>
        UpdateSubmitted = 9
    }
}

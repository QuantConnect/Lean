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

/**********************************************************
* USING NAMESPACES
**********************************************************/

namespace QuantConnect.Orders
{
    /********************************************************
    * ORDER CLASS DEFINITION
    *********************************************************/
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
        StopLimit
    }

    /// <summary>
    /// Order duration in market
    /// </summary>
    public enum OrderDuration
    {
        /// <summary>
        /// Order good until its filled.
        /// </summary>
        GTC,

        /*
        /// <summary>
        /// Order valid for today only: -- CURRENTLY ONLY GTC ORDER DURATION TYPE IN BACKTESTS.
        /// </summary>
        Day
        */
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
        New,

        /// <summary>
        /// Order flagged for updating the inmarket order.
        /// </summary>
        Update,

        /// <summary>
        /// Order submitted to the market
        /// </summary>
        Submitted,

        /// <summary>
        /// Partially filled, In Market Order.
        /// </summary>
        PartiallyFilled,

        /// <summary>
        /// Completed, Filled, In Market Order.
        /// </summary>
        Filled,

        /// <summary>
        /// Order cancelled before it was filled
        /// </summary>
        Canceled,

        /// <summary>
        /// No Order State Yet
        /// </summary>
        None,

        /// <summary>
        /// Order invalidated before it hit the market (e.g. insufficient capital)..
        /// </summary>
        Invalid
    }

} // End QC Namespace:

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
using QuantConnect.Securities;

namespace QuantConnect.Orders
{
    /// <summary>
    /// Market on close order type - submits a market order on exchange close
    /// </summary>
    public class MarketOnCloseOrder : Order
    {
        /// <summary>
        /// Gets the default interval before market close that an MOC order may be submitted.
        /// For example, US equity exchanges typically require MOC orders to be placed no later
        /// than 15 minutes before market close, which yields a nominal time of 3:45PM.
        /// This buffer value takes into account the 15 minutes and adds an additional 30 seconds
        /// to account for other potential delays, such as LEAN order processing and placement of
        /// the order to the exchange.
        /// </summary>
        public static readonly TimeSpan DefaultSubmissionTimeBuffer = TimeSpan.FromMinutes(15.5);

        /// <summary>
        /// MarketOnClose Order Type
        /// </summary>
        public override OrderType Type
        {
            get { return OrderType.MarketOnClose; }
        }

        /// <summary>
        /// Intiializes a new instance of the <see cref="MarketOnCloseOrder"/> class.
        /// </summary>
        public MarketOnCloseOrder()
        {
        }

        /// <summary>
        /// Intiializes a new instance of the <see cref="MarketOnCloseOrder"/> class.
        /// </summary>
        /// <param name="symbol">The security's symbol being ordered</param>
        /// <param name="quantity">The number of units to order</param>
        /// <param name="time">The current time</param>
        /// <param name="tag">A user defined tag for the order</param>
        /// <param name="properties">The order properties for this order</param>
        public MarketOnCloseOrder(Symbol symbol, decimal quantity, DateTime time, string tag = "", IOrderProperties properties = null)
            : base(symbol, quantity, time, tag, properties)
        {
        }

        /// <summary>
        /// Gets the order value in units of the security's quote currency
        /// </summary>
        /// <param name="security">The security matching this order's symbol</param>
        protected override decimal GetValueImpl(Security security)
        {
            return Quantity*security.Price;
        }

        /// <summary>
        /// Creates a deep-copy clone of this order
        /// </summary>
        /// <returns>A copy of this order</returns>
        public override Order Clone()
        {
            var order = new MarketOnCloseOrder();
            CopyTo(order);
            return order;
        }
    }
}

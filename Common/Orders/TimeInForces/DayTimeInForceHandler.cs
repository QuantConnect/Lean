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

namespace QuantConnect.Orders.TimeInForces
{
    /// <summary>
    /// Handles the Day time in force for an order (DAY)
    /// </summary>
    public class DayTimeInForceHandler : ITimeInForceHandler
    {
        /// <summary>
        /// Checks if an order has expired
        /// </summary>
        /// <param name="security">The security matching the order</param>
        /// <param name="order">The order to be checked</param>
        /// <returns>Returns true if the order has expired, false otherwise</returns>
        public bool HasOrderExpired(Security security, Order order)
        {
            var exchangeHours = security.Exchange.Hours;

            var orderTime = order.Time.ConvertFromUtc(exchangeHours.TimeZone);
            var time = security.LocalTime;

            bool expired;
            switch (order.SecurityType)
            {
                case SecurityType.Forex:
                case SecurityType.Cfd:
                    // expires at 5 PM NewYork time
                    var expiryTime = new DateTime(orderTime.Date.Year, orderTime.Date.Month, orderTime.Date.Day, 17, 0, 0);
                    expired = time.Date > orderTime.Date || time.ConvertTo(exchangeHours.TimeZone, TimeZones.NewYork) >= expiryTime;
                    break;

                case SecurityType.Crypto:
                    // expires at midnight
                    expired = time.Date > orderTime.Date;
                    break;

                case SecurityType.Equity:
                case SecurityType.Option:
                case SecurityType.Future:
                default:
                    // expires at market close
                    expired = time.Date > orderTime.Date || !exchangeHours.IsOpen(time, false);
                    break;
            }

            return expired;
        }

        /// <summary>
        /// Checks if an order fill is valid
        /// </summary>
        /// <param name="security">The security matching the order</param>
        /// <param name="order">The order to be checked</param>
        /// <param name="fill">The order fill to be checked</param>
        /// <returns>Returns true if the order fill can be emitted, false otherwise</returns>
        public bool IsFillValid(Security security, Order order, OrderEvent fill)
        {
            return true;
        }
    }
}

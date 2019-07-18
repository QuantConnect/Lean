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
using QuantConnect.Securities;

namespace QuantConnect.Orders.TimeInForces
{
    /// <summary>
    /// Day Time In Force - order expires at market close
    /// </summary>
    public class DayTimeInForce : TimeInForce
    {
        /// <summary>
        /// Checks if an order is expired
        /// </summary>
        /// <param name="security">The security matching the order</param>
        /// <param name="order">The order to be checked</param>
        /// <returns>Returns true if the order has expired, false otherwise</returns>
        public override bool IsOrderExpired(Security security, Order order)
        {
            var exchangeHours = security.Exchange.Hours;

            var orderTime = order.Time.ConvertFromUtc(exchangeHours.TimeZone);
            var time = security.LocalTime;

            bool expired;
            switch (order.SecurityType)
            {
                case SecurityType.Forex:
                case SecurityType.Cfd:
                    // With real brokerages (IB, Oanda, FXCM have been verified) FX orders expire at 5 PM NewYork time.
                    // For now we use this fixed cut-off time, in future we might get this value from brokerage models,
                    // to support custom brokerage implementations.

                    var cutOffTimeZone = TimeZones.NewYork;
                    var cutOffTimeSpan = TimeSpan.FromHours(17);

                    orderTime = order.Time.ConvertFromUtc(cutOffTimeZone);
                    var expiryTime = orderTime.Date.Add(cutOffTimeSpan);
                    if (orderTime.TimeOfDay > cutOffTimeSpan)
                    {
                        // order submitted after 5 PM, expiry on next date
                        expiryTime = expiryTime.AddDays(1);
                    }

                    expired = time.ConvertTo(exchangeHours.TimeZone, cutOffTimeZone) >= expiryTime;
                    break;

                case SecurityType.Crypto:
                    // expires at midnight UTC
                    expired = time.Date > orderTime.Date;
                    break;

                case SecurityType.Equity:
                case SecurityType.Option:
                case SecurityType.Future:
                default:
                    // expires at market close
                    expired = time >= exchangeHours.GetNextMarketClose(orderTime, false);
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
        public override bool IsFillValid(Security security, Order order, OrderEvent fill)
        {
            return true;
        }
    }
}

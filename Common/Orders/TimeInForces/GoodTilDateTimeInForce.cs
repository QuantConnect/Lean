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
    /// Good Til Date Time In Force - order expires and will be cancelled on a fixed date/time
    /// </summary>
    public class GoodTilDateTimeInForce : TimeInForce
    {
        /// <summary>
        /// The date/time on which the order will expire and will be cancelled
        /// </summary>
        /// <remarks>The private set is required for JSON deserialization</remarks>
        public DateTime Expiry { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="GoodTilDateTimeInForce"/> class
        /// </summary>
        /// <remarks>This constructor is required for JSON deserialization</remarks>
        private GoodTilDateTimeInForce()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GoodTilDateTimeInForce"/> class
        /// </summary>
        public GoodTilDateTimeInForce(DateTime expiry)
        {
            Expiry = expiry;
        }

        /// <summary>
        /// Checks if an order is expired
        /// </summary>
        /// <param name="security">The security matching the order</param>
        /// <param name="order">The order to be checked</param>
        /// <returns>Returns true if the order has expired, false otherwise</returns>
        public override bool IsOrderExpired(Security security, Order order)
        {
            var exchangeHours = security.Exchange.Hours;

            var time = security.LocalTime;

            bool expired;
            switch (order.SecurityType)
            {
                case SecurityType.Forex:
                case SecurityType.Cfd:
                    // With real brokerages (IB, Oanda, FXCM have been verified) FX orders expire at 5 PM NewYork time.
                    // For now we use this fixed cut-off time, in future we might get this value from brokerage models,
                    // to support custom brokerage implementations.
                    expired = time.ConvertToUtc(exchangeHours.TimeZone) >= GetForexOrderExpiryDateTime(order);
                    break;

                case SecurityType.Crypto:
                case SecurityType.CryptoFuture:
                    // expires at midnight after expiry date
                    expired = time.Date > Expiry.Date;
                    break;

                case SecurityType.Equity:
                case SecurityType.Option:
                case SecurityType.Future:
                case SecurityType.FutureOption:
                case SecurityType.IndexOption:
                default:
                    // expires at market close of expiry date
                    expired = time >= exchangeHours.GetNextMarketClose(Expiry.Date, false);
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

        /// <summary>
        /// Returns the expiry date and time (UTC) for a Forex order
        /// </summary>
        public DateTime GetForexOrderExpiryDateTime(Order order)
        {
            var cutOffTimeZone = TimeZones.NewYork;
            var cutOffTimeSpan = TimeSpan.FromHours(17);

            var expiryTime = Expiry.Date.Add(cutOffTimeSpan);
            if (order.Time.Date == Expiry.Date)
            {
                // expiry date same as order date
                var orderTime = order.Time.ConvertFromUtc(cutOffTimeZone);
                if (orderTime.TimeOfDay > cutOffTimeSpan)
                {
                    // order submitted after 5 PM, expiry on next date
                    expiryTime = expiryTime.AddDays(1);
                }
            }

            return expiryTime.ConvertToUtc(cutOffTimeZone);
        }
    }
}

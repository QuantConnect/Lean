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

using QuantConnect.Interfaces;
using QuantConnect.Securities;

namespace QuantConnect.Orders.TimeInForces
{
    /// <summary>
    /// Handles the Day time in force for an order (DAY)
    /// </summary>
    public class DayTimeInForceHandler : ITimeInForceHandler
    {
        private readonly IAlgorithm _algorithm;

        /// <summary>
        /// Initializes a new instance of the <see cref="DayTimeInForceHandler"/> class
        /// </summary>
        /// <param name="algorithm">The instance of the algorithm</param>
        public DayTimeInForceHandler(IAlgorithm algorithm)
        {
            _algorithm = algorithm;
        }

        /// <summary>
        /// Checks if an order has expired
        /// </summary>
        /// <param name="order">The order to be checked</param>
        /// <returns>Returns true if the order has expired, false otherwise</returns>
        public bool HasOrderExpired(Order order)
        {
            var exchangeHours = MarketHoursDatabase
                .FromDataFolder()
                .GetExchangeHours(order.Symbol.ID.Market, order.Symbol, order.Symbol.SecurityType);

            var time = _algorithm.UtcTime.ConvertFromUtc(exchangeHours.TimeZone);

            return !exchangeHours.IsOpen(time, false);
        }

        /// <summary>
        /// Checks if an order fill is valid
        /// </summary>
        /// <param name="order">The order to be checked</param>
        /// <param name="fill">The order fill to be checked</param>
        /// <returns>Returns true if the order fill can be emitted, false otherwise</returns>
        public bool IsFillValid(Order order, OrderEvent fill)
        {
            return true;
        }
    }
}

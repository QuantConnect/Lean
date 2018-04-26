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
        private readonly IBrokerage _brokerage;

        /// <summary>
        /// Initializes a new instance of the <see cref="DayTimeInForceHandler"/> class
        /// </summary>
        /// <param name="algorithm">The instance of the algorithm</param>
        /// <param name="brokerage">The instance of the backtesting brokerage</param>
        public DayTimeInForceHandler(IAlgorithm algorithm, IBrokerage brokerage)
        {
            _algorithm = algorithm;
            _brokerage = brokerage;
        }

        /// <summary>
        /// Handles the time in force for an order before any order fill is generated
        /// </summary>
        /// <param name="order">The order to be handled</param>
        /// <returns>Returns true if the order fills can be generated, false otherwise</returns>
        public bool HandleOrderPreFill(Order order)
        {
            var exchangeHours = MarketHoursDatabase
                .FromDataFolder()
                .GetExchangeHours(order.Symbol.ID.Market, order.Symbol, order.Symbol.SecurityType);

            var time = _algorithm.UtcTime.ConvertFromUtc(exchangeHours.TimeZone);

            if (!exchangeHours.IsOpen(time, false))
            {
                _brokerage.CancelOrder(order);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Handles the time in force for an order fill
        /// </summary>
        /// <param name="order">The order fill to be handled</param>
        /// <param name="fill">The order fill to be handled</param>
        /// <returns>Returns true if the order fill can be emitted, false otherwise</returns>
        public bool HandleOrderPostFill(Order order, OrderEvent fill)
        {
            return true;
        }
    }
}

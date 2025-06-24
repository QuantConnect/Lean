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
using QuantConnect.Orders;
using System.Collections.Generic;
using QuantConnect.Orders.TimeInForces;

namespace QuantConnect.Brokerages
{
    /// <summary>
    /// Provides properties specific to interactive brokers
    /// </summary>
    public class InteractiveBrokersFixModel : InteractiveBrokersBrokerageModel
    {
        protected override Type[] SupportedTimeInForces { get; } =
        {
            typeof(GoodTilCanceledTimeInForce),
            typeof(DayTimeInForce),
        };

        protected override HashSet<OrderType> SupportedOrderTypes { get; } = new HashSet<OrderType>
        {
            OrderType.Market,
            OrderType.MarketOnOpen,
            OrderType.MarketOnClose,
            OrderType.Limit,
            OrderType.StopMarket,
            OrderType.StopLimit,
            OrderType.TrailingStop,
            OrderType.ComboMarket,
            OrderType.ComboLimit
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="InteractiveBrokersFixModel"/> class
        /// </summary>
        /// <param name="accountType">The type of account to be modelled, defaults to
        /// <see cref="AccountType.Margin"/></param>
        public InteractiveBrokersFixModel(AccountType accountType = AccountType.Margin)
            : base(accountType)
        {
        }
    }
}

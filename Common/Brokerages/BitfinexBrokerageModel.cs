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
using System.Collections.Generic;
using QuantConnect.Orders;
using QuantConnect.Securities;
using QuantConnect.Orders.Fills;
using QuantConnect.Orders.Fees;
using System.Linq;

namespace QuantConnect.Brokerages
{
    /// <summary>
    /// Provides Bitfinex specific properties
    /// </summary>
    public class BitfinexBrokerageModel : DefaultBrokerageModel
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BitfinexBrokerageModel"/> class
        /// </summary>
        /// <param name="accountType">The type of account to be modelled, defaults to
        /// <see cref="AccountType.Margin"/></param>
        public BitfinexBrokerageModel(AccountType accountType = AccountType.Margin)
            : base(accountType)
        {
        }
    }
}
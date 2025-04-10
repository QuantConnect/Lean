/*
* QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
* Lean Algorithmic Trading Engine v2.0. Copyright 2023 QuantConnect Corporation.
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
    /// Contains additional properties and settings for an order submitted to EZE brokerage
    /// </summary>
    public class EzeOrderProperties : OrderProperties
    {
        /// <summary>
        /// Gets or sets the route name as shown in Eze EMS.
        /// </summary>
        public string Route { get; set; }

        /// <summary>
        /// Gets or sets a semi-colon separated list of trade or neutral accounts 
        /// the user has permission for, e.g., "TAL;TEST;USER1;TRADE" or "TAL;TEST;USER2;NEUTRAL".
        /// </summary>
        public string Account { get; set; }

        /// <summary>
        /// Gets or sets the user message or notes.
        /// </summary>
        public string Notes { get; set; }

        /// <summary>
        /// Initializes a new instance with optional route, account, and notes.
        /// </summary>
        /// <param name="route">The trading route name (optional).</param>
        /// <param name="account">The trading account with specific permissions (optional).</param>
        /// <param name="notes">Optional notes about the order.</param>
        public EzeOrderProperties(string route = default, string account = default, string notes = default)
            : base()
        {
            Route = route;
            Account = account;
            Notes = notes;
        }
    }
}

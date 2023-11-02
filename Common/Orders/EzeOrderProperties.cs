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
    public class EzeOrderProperties: OrderProperties
    {
        /// <summary>
        /// Route name as shown in Eze EMS.
        /// America | Europe | Asia
        /// </summary>
        public string Route { get; internal set; }

        /// <summary>
        /// Semi-colon separated values that represent either Trade or Neutral accounts the user has permission 
        /// e.g.,TAL;TEST;USER1;TRADE or TAL;TEST;USER2;NEUTRAL
        /// </summary>
        public string Account { get; internal set; }

        /// <summary>
        /// User message/notes
        /// </summary>
        public string UserMessage { get; internal set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="route"></param>
        /// <param name="account"></param>
        /// <param name="exchange"></param>
        /// <param name="userMessage"></param>
        public EzeOrderProperties(string route, string account, Exchange exchange, string userMessage = "") : base(exchange)
        {
            Route = route;
            Account = account;
            UserMessage = userMessage;
        }
    }
}

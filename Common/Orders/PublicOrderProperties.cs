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
 *
*/

namespace QuantConnect.Orders
{
    /// <summary>
    /// Represents the properties of an order in Public.com.
    /// </summary>
    public class PublicOrderProperties : OrderProperties
    {
        /// <summary>
        /// If set to <c>true</c>, allows the order to trigger or fill outside of regular trading hours
        /// (the extended session).
        /// </summary>
        /// <remarks>
        /// Applicable to day time-in-force equity orders only.
        /// </remarks>
        public bool OutsideRegularTradingHours { get; set; }

        /// <summary>
        /// Controls the buying power used by the order.
        /// <c>true</c> uses margin buying power when the account allows it; <c>false</c> uses cash-only buying power.
        /// When left <c>null</c>, the brokerage model fills it in from the account type before the order is sent.
        /// </summary>
        public bool? UseMargin { get; set; }
    }
}

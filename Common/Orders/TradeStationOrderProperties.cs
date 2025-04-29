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
    /// Represents the properties of an order in TradeStation.
    /// </summary>
    public class TradeStationOrderProperties : OrderProperties
    {
        /// <summary>
        /// Enables the "All or None" feature for your order, ensuring it will only be filled completely or not at all. 
        /// Set to true to activate this feature, or false to allow partial fills.
        /// </summary>
        /// <remarks>
        /// Applicable to Equities and Options.
        /// </remarks>
        public bool AllOrNone { get; set; }

        /// <summary>
        /// If set to true, allows orders to also trigger or fill outside of regular trading hours.
        /// </summary>
        public bool OutsideRegularTradingHours { get; set; }
    }
}

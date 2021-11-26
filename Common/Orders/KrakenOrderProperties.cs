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
    /// Kraken order properties
    /// </summary>
    public class KrakenOrderProperties : OrderProperties
    {
        /// <summary>
        /// Post-only order (available when ordertype = limit)
        /// </summary>
        public bool PostOnly { get; set; }
        
        /// <summary>
        /// Prefer fee in base currency (default if selling)
        /// </summary>
        public bool FeeInBase { get; set; }
        
        /// <summary>
        /// Prefer fee in quote currency (default if buying, mutually exclusive with FeeInBase)
        /// </summary>
        public bool FeeInQuote { get; set; }
        
        /// <summary>
        /// https://support.kraken.com/hc/en-us/articles/201648183-Market-Price-Protection
        /// </summary>
        public bool NoMarketPriceProtection { get; set; }

        /// <summary>
        /// Conditional close orders are triggered by execution of the primary order in the same quantity and opposite direction. Ordertypes can be the same with primary order.
        /// </summary>
        public Order ConditionalOrder { get; set; }
    }
}

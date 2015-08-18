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

namespace QuantConnect.Statistics
{
    /// <summary>
    /// Represents an execution (fill) of an order
    /// </summary>
    public class TradeExecution
    {
        /// <summary>
        /// The symbol of the filled order
        /// </summary>
        public string Symbol { get; set; }

        /// <summary>
        /// The date and time the order was filled
        /// </summary>
        public DateTime Time { get; set; }

        /// <summary>
        /// The quantity which was filled (positive=buy, negative=sell)
        /// </summary>
        public decimal Quantity { get; set; }

        /// <summary>
        /// The price at which the order was filled
        /// </summary>
        public decimal Price { get; set; }
    }
}

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
using IBApi;

namespace QuantConnect.Brokerages.InteractiveBrokers.Client
{
    /// <summary>
    /// Event arguments class for the <see cref="InteractiveBrokersClient.MarketRule"/> event
    /// </summary>
    public class MarketRuleEventArgs : EventArgs
    {
        /// <summary>
        /// The market rule id.
        /// </summary>
        public int MarketRuleId { get; set; }

        /// <summary>
        /// The price increments.
        /// </summary>
        public PriceIncrement[] PriceIncrements { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MarketRuleEventArgs"/> class
        /// </summary>
        public MarketRuleEventArgs(int marketRuleId, PriceIncrement[] priceIncrements)
        {
            MarketRuleId = marketRuleId;
            PriceIncrements = priceIncrements;
        }
    }
}

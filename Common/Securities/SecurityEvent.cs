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

namespace QuantConnect.Securities
{
    /// <summary>
    /// Messaging class signifying a change in a user's portfolio.
    /// </summary>
    public class SecurityEvent
    {
        /// <summary>
        /// Gets the symbol that was changed
        /// </summary>
        /// <remarks>
        /// This is not a <see cref="Symbol"/> object because it's coming from the brokerage
        /// and only used in live trading where it is the symbol
        /// </remarks>
        public string Symbol { get; private set; }

        /// <summary>
        /// Gets the quantity held in the symbol
        /// </summary>
        public int Quantity { get; private set; }

        /// <summary>
        /// Gets the average price of the holding
        /// </summary>
        public decimal AveragePrice { get; private set; }

        /// <summary>
        /// Creates a SecurityEvent
        /// </summary>
        /// <param name="symbol">The symbol that was changed</param>
        /// <param name="quantity">The quantity held in the symbol</param>
        /// <param name="averagePrice">The average price of each holding</param>
        public SecurityEvent(string symbol, int quantity, decimal averagePrice)
        {
            Symbol = symbol;
            Quantity = quantity;
            AveragePrice = averagePrice;
        }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>
        /// A string that represents the current object.
        /// </returns>
        /// <filterpriority>2</filterpriority>
        public override string ToString()
        {
            return string.Format("Symbol: {0} Quantity: {1} Price: {2}", Symbol, Quantity, AveragePrice);
        }
    }
}

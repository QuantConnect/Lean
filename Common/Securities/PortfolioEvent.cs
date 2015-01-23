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
    public class PortfolioEvent
    {
        /// <summary>
        /// The symbol that was changed
        /// </summary>
        public string Symbol { get; private set; }

        /// <summary>
        /// The quantity held in the symbol
        /// </summary>
        public int Quantity { get; private set; }

        /// <summary>
        /// Creates a PortfolioEvent
        /// </summary>
        /// <param name="symbol">The symbol that was changed</param>
        /// <param name="quantity">The quantity held in the symbol</param>
        public PortfolioEvent(string symbol, int quantity)
        {
            Symbol = symbol;
            Quantity = quantity;
        }
    }
}

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
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect.Data.Market
{
    /// <summary>
    /// Defines a single option contract at a specific expiration and strike price
    /// </summary>
    public class OptionContract
    {
        /// <summary>
        /// Gets the option contract's symbol
        /// </summary>
        public Symbol Symbol
        {
            get; private set;
        }

        /// <summary>
        /// Gets the underlying security's symbol
        /// </summary>
        public Symbol UnderlyingSymbol
        {
            get; private set;
        }

        /// <summary>
        /// Gets the strike price
        /// </summary>
        public decimal Strike
        {
            get { return Symbol.ID.StrikePrice; }
        }

        /// <summary>
        /// Gets the expiration date
        /// </summary>
        public DateTime Expiry
        {
            get { return Symbol.ID.Date; }
        }

        /// <summary>
        /// Gets the right being purchased (call [right to buy] or put [right to sell])
        /// </summary>
        public OptionRight Right
        {
            get { return Symbol.ID.OptionRight; }
        }

        /// <summary>
        /// Gets or sets the greeks for this contract
        /// </summary>
        public FirstOrderGreeks Greeks
        {
            get; set;
        }

        /// <summary>
        /// Gets the local date time this contract's data was last updated
        /// </summary>
        public DateTime Time
        {
            get; set;
        }

        /// <summary>
        /// Gets the open interest
        /// </summary>
        public decimal OpenInterest
        {
            get; set;
        }

        /// <summary>
        /// Gets the last price this contract traded at
        /// </summary>
        public decimal LastPrice
        {
            get; set;
        }

        /// <summary>
        /// Gets the current bid price
        /// </summary>
        public decimal BidPrice
        {
            get; set;
        }

        /// <summary>
        /// Get the current bid size
        /// </summary>
        public long BidSize
        {
            get; set;
        }

        /// <summary>
        /// Gets the ask price
        /// </summary>
        public decimal AskPrice
        {
            get; set;
        }

        /// <summary>
        /// Gets the current ask size
        /// </summary>
        public long AskSize
        {
            get; set;
        }

        /// <summary>
        /// Gets the last price the underlying security traded at
        /// </summary>
        public decimal UnderlyingLastPrice
        {
            get; set;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OptionContract"/> class
        /// </summary>
        /// <param name="symbol">The option contract symbol</param>
        /// <param name="underlyingSymbol">The symbol of the underlying security</param>
        public OptionContract(Symbol symbol, Symbol underlyingSymbol)
        {
            Symbol = symbol;
            UnderlyingSymbol = underlyingSymbol;

            // empty default instance - models can populate this later in the pipelines
            Greeks = new FirstOrderGreeks();
        }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>
        /// A string that represents the current object.
        /// </returns>
        public override string ToString()
        {
            return string.Format("{0}{1}{2}{3:00000000}", Symbol.ID.Symbol, Expiry.ToString(DateFormat.EightCharacter), Right.ToString()[0], Strike*1000m);
        }
    }
}

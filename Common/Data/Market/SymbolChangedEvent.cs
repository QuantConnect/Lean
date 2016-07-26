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

using System;

namespace QuantConnect.Data.Market
{
    /// <summary>
    /// Symbol changed event of a security. This is generated when a symbol is remapped for a given
    /// security, for example, at EOD 2014.04.02 GOOG turned into GOOGL, but are the same
    /// </summary>
    public class SymbolChangedEvent : BaseData
    {
        /// <summary>
        /// Gets the symbol before the change
        /// </summary>
        public string OldSymbol { get; private set; }

        /// <summary>
        /// Gets the symbol after the change
        /// </summary>
        public string NewSymbol { get; private set; }

        /// <summary>
        /// Initializes a new default instance of the <see cref="SymbolChangedEvent"/> class
        /// </summary>
        public SymbolChangedEvent()
        {
            DataType = MarketDataType.Auxiliary;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SymbolChangedEvent"/>
        /// </summary>
        /// <param name="requestedSymbol">The symbol that was originally requested</param>
        /// <param name="date">The date/time this symbol remapping took place</param>
        /// <param name="oldSymbol">The old symbol mapping</param>
        /// <param name="newSymbol">The new symbol mapping</param>
        public SymbolChangedEvent(Symbol requestedSymbol, DateTime date, string oldSymbol, string newSymbol)
            : this()
        {
            Time = date;
            Symbol = requestedSymbol;
            OldSymbol = oldSymbol;
            NewSymbol = newSymbol;
        }

        /// <summary>
        /// Return a new instance clone of this object, used in fill forward
        /// </summary>
        /// <remarks>
        /// This base implementation uses reflection to copy all public fields and properties
        /// </remarks>
        /// <returns>A clone of the current object</returns>
        public override BaseData Clone()
        {
            return new SymbolChangedEvent(Symbol, Time, OldSymbol, NewSymbol);
        }
    }
}

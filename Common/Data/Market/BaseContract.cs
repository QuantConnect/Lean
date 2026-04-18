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

using QuantConnect.Python;
using System;

namespace QuantConnect.Data.Market
{
    /// <summary>
    /// Defines a base for a single contract, like an option or future contract
    /// </summary>
    public abstract class BaseContract : ISymbolProvider
    {
        /// <summary>
        /// Gets the contract's symbol
        /// </summary>
        [PandasIgnore]
        public Symbol Symbol
        {
            get; set;
        }

        /// <summary>
        /// The security identifier of the symbol
        /// </summary>
        [PandasIgnore]
        public SecurityIdentifier ID => Symbol.ID;

        /// <summary>
        /// Gets the underlying security's symbol
        /// </summary>
        public Symbol UnderlyingSymbol => Symbol.Underlying;

        /// <summary>
        /// Gets the expiration date
        /// </summary>
        public DateTime Expiry => Symbol.ID.Date;

        /// <summary>
        /// Gets the local date time this contract's data was last updated
        /// </summary>
        [PandasIgnore]
        public DateTime Time
        {
            get; set;
        }

        /// <summary>
        /// Gets the open interest
        /// </summary>
        public virtual decimal OpenInterest { get; set; }

        /// <summary>
        /// Gets the last price this contract traded at
        /// </summary>
        public virtual decimal LastPrice { get; set; }

        /// <summary>
        /// Gets the last volume this contract traded at
        /// </summary>
        public virtual long Volume { get; set; }

        /// <summary>
        /// Gets the current bid price
        /// </summary>
        public virtual decimal BidPrice { get; set; }

        /// <summary>
        /// Get the current bid size
        /// </summary>
        public virtual long BidSize { get; set; }

        /// <summary>
        /// Gets the ask price
        /// </summary>
        public virtual decimal AskPrice { get; set; }

        /// <summary>
        /// Gets the current ask size
        /// </summary>
        public virtual long AskSize { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseContract"/> class
        /// </summary>
        /// <param name="symbol">The contract symbol</param>
        protected BaseContract(Symbol symbol)
        {
            Symbol = symbol;
        }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>
        /// A string that represents the current object.
        /// </returns>
        public override string ToString() => Symbol.Value;

        /// <summary>
        /// Updates the contract with the new data, which can be a <see cref="Tick"/> or <see cref="TradeBar"/> or <see cref="QuoteBar"/>
        /// </summary>
        internal abstract void Update(BaseData data);
    }
}

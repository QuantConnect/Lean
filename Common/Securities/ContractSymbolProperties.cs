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
    /// Represents common properties for contract-based securities such as options and CFDs
    /// </summary>
    public class ContractSymbolProperties : SymbolProperties
    {
        /// <summary>
        /// The contract multiplier for the security.
        /// </summary>
        /// <remarks>
        /// If manually set by a consumer, this value will be used instead of the
        /// <see cref="SymbolProperties.ContractMultiplier"/> and also allows to make
        /// sure it is not overridden when the symbol properties database gets updated.
        /// </remarks>
        private decimal? _contractMultiplier;

        /// <summary>
        /// The contract multiplier for the security
        /// </summary>
        public override decimal ContractMultiplier => _contractMultiplier ?? base.ContractMultiplier;

        /// <summary>
        /// Creates an instance of the <see cref="ContractSymbolProperties"/> class from a <see cref="SymbolProperties"/> instance
        /// </summary>
        public ContractSymbolProperties(SymbolProperties properties)
            : base(properties)
        {
        }

        /// <summary>
        /// Creates an instance of the <see cref="ContractSymbolProperties"/> class
        /// </summary>
        public ContractSymbolProperties(string description, string quoteCurrency, decimal contractMultiplier,
            decimal minimumPriceVariation, decimal lotSize, string marketTicker,
            decimal? minimumOrderSize = null, decimal priceMagnifier = 1, decimal strikeMultiplier = 1)
            : base(description, quoteCurrency, contractMultiplier, minimumPriceVariation, lotSize, marketTicker,
                minimumOrderSize, priceMagnifier, strikeMultiplier)
        {
        }

        /// <summary>
        /// Sets a custom contract multiplier that persists through symbol properties database updates
        /// </summary>
        internal void SetContractMultiplier(decimal multiplier)
        {
            _contractMultiplier = multiplier;
        }
    }
}

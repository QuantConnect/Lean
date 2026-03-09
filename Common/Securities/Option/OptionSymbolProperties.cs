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

namespace QuantConnect.Securities.Option
{
    /// <summary>
    /// Represents common properties for a specific option contract
    /// </summary>
    public class OptionSymbolProperties : SymbolProperties
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
        /// When the holder of an equity option exercises one contract, or when the writer of an equity option is assigned
        /// an exercise notice on one contract, this unit of trade, usually 100 shares of the underlying security, changes hands.
        /// </summary>
        public int ContractUnitOfTrade
        {
            get; protected set;
        }

        /// <summary>
        /// Creates an instance of the <see cref="OptionSymbolProperties"/> class
        /// </summary>
        public OptionSymbolProperties(string description, string quoteCurrency, decimal contractMultiplier, decimal pipSize, decimal lotSize)
            : this(new SymbolProperties(description, quoteCurrency, contractMultiplier, pipSize, lotSize, string.Empty))
        {
        }

        /// <summary>
        /// Creates an instance of the <see cref="OptionSymbolProperties"/> class from <see cref="SymbolProperties"/> class
        /// </summary>
        public OptionSymbolProperties(SymbolProperties properties)
            : base(properties)
        {
            ContractUnitOfTrade = (int)properties.ContractMultiplier;
        }

        internal void SetContractUnitOfTrade(int unitOfTrade)
        {
            ContractUnitOfTrade = unitOfTrade;
        }

        internal void SetContractMultiplier(decimal multiplier)
        {
            _contractMultiplier = multiplier;
        }
    }
}

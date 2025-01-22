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
        private SymbolProperties _baseProperties;

        /// <summary>
        /// The description of the security
        /// </summary>
        public override string Description => _baseProperties.Description;

        /// <summary>
        /// The quote currency of the security
        /// </summary>
        public override string QuoteCurrency => _baseProperties.QuoteCurrency;

        /// <summary>
        /// The contract multiplier for the security
        /// </summary>
        public override decimal ContractMultiplier => _baseProperties.ContractMultiplier;

        /// <summary>
        /// When the holder of an equity option exercises one contract, or when the writer of an equity option is assigned
        /// an exercise notice on one contract, this unit of trade, usually 100 shares of the underlying security, changes hands.
        /// </summary>
        public int ContractUnitOfTrade
        {
            get; protected set;
        }

        /// <summary>
        /// Minimum price variation, required for index options contracts with
        /// variable sized quoted prices depending on the premium of the option.
        /// </summary>
        public override decimal MinimumPriceVariation => _baseProperties.MinimumPriceVariation;

        /// <summary>
        /// The lot size (lot size of the order) for the security
        /// </summary>
        public override decimal LotSize => _baseProperties.LotSize;

        /// <summary>
        /// The market ticker
        /// </summary>
        public override string MarketTicker => _baseProperties.MarketTicker;

        /// <summary>
        /// The minimum order size allowed
        /// </summary>
        public override decimal? MinimumOrderSize => _baseProperties.MinimumOrderSize;

        /// <summary>
        /// Allows normalizing live asset prices to US Dollars for Lean consumption. In some exchanges,
        /// for some securities, data is expressed in cents like for example for corn futures ('ZC').
        /// </summary>
        public override decimal PriceMagnifier => _baseProperties.PriceMagnifier;

        /// <summary>
        /// Scale factor for option's strike price. For some options, such as NQX, the strike price
        /// is based on a fraction of the underlying, thus this paramater scales the strike price so
        /// that it can be used in comparation with the underlying such as
        /// in <see cref="OptionFilterUniverse.Strikes(int, int)"/>
        /// </summary>
        public override decimal StrikeMultiplier => _baseProperties.StrikeMultiplier;

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
        {
            _baseProperties = properties;
            ContractUnitOfTrade = (int)properties.ContractMultiplier;
        }

        internal void SetContractUnitOfTrade(int unitOfTrade)
        {
            ContractUnitOfTrade = unitOfTrade;
        }

        internal void SetContractMultiplier(decimal multiplier)
        {
            _baseProperties.ContractMultiplier = multiplier;
        }

        /// <summary>
        /// Updates the symbol properties with the values from the specified <paramref name="other"/>
        /// </summary>
        /// <param name="other">The symbol properties to take values from</param>
        internal override void Update(SymbolProperties other)
        {
            _baseProperties.Update(other);
            if (other is OptionSymbolProperties optionSymbolProperties)
            {
                ContractUnitOfTrade = optionSymbolProperties.ContractUnitOfTrade;
            }
        }
    }
}

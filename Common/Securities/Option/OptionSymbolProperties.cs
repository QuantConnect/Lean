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
        /// When the holder of an equity option exercises one contract, or when the writer of an equity option is assigned
        /// an exercise notice on one contract, this unit of trade, usually 100 shares of the underlying security, changes hands.
        /// </summary>
        public int ContractUnitOfTrade
        {
            get; protected set;
        }

        /// <summary>
        /// Overridable minimum price variation, required for index options contracts with
        /// variable sized quoted prices depending on the premium of the option.
        /// </summary>
        public override decimal MinimumPriceVariation
        {
            get;
            protected set;
        }

        /// <summary>
        /// Creates an instance of the <see cref="OptionSymbolProperties"/> class
        /// </summary>
        public OptionSymbolProperties(string description, string quoteCurrency, decimal contractMultiplier, decimal pipSize, decimal lotSize)
            : base(description, quoteCurrency, contractMultiplier, pipSize, lotSize, string.Empty)
        {
            ContractUnitOfTrade = (int)contractMultiplier;
        }

        /// <summary>
        /// Creates an instance of the <see cref="OptionSymbolProperties"/> class from <see cref="SymbolProperties"/> class
        /// </summary>
        public OptionSymbolProperties(SymbolProperties properties)
            : base(properties.Description,
                 properties.QuoteCurrency,
                 properties.ContractMultiplier,
                 properties.MinimumPriceVariation,
                 properties.LotSize,
                 properties.MarketTicker,
                 properties.MinimumOrderSize,
                 properties.PriceMagnifier,
                 properties.StrikeMultiplier)
        {
            ContractUnitOfTrade = (int)properties.ContractMultiplier;
        }

        internal void SetContractUnitOfTrade(int unitOfTrade)
        {
            ContractUnitOfTrade = unitOfTrade;
        }

        internal void SetContractMultiplier(decimal multiplier)
        {
            ContractMultiplier = multiplier;
        }
    }
}

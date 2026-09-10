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
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Data.Market;

namespace QuantConnect.Securities
{
    /// <summary>
    /// Option contracts filter over the contracts of an <see cref="OptionChain"/>, so chains offer
    /// the same filters as the option universe selection (<see cref="OptionFilterUniverse"/>)
    /// </summary>
    internal class OptionChainFilterUniverse : BaseOptionFilterUniverse<OptionChainFilterUniverse, OptionContract>
    {
        private readonly Symbol _symbol;
        private SecurityExchangeHours _exchangeHours;

        /// <summary>
        /// The option exchange hours
        /// </summary>
        protected override SecurityExchangeHours ExchangeHours =>
            _exchangeHours ??= MarketHoursDatabase.FromDataFolder().GetExchangeHours(_symbol.ID.Market, _symbol, _symbol.SecurityType);

        /// <summary>
        /// The option security type
        /// </summary>
        protected override SecurityType SecurityType => _symbol.SecurityType;

        /// <summary>
        /// Initializes a new instance of the <see cref="OptionChainFilterUniverse"/> class over the contracts of the given chain
        /// </summary>
        /// <param name="chain">The option chain to filter</param>
        public OptionChainFilterUniverse(OptionChain chain)
            : base(GetContracts(chain), GetUnderlying(chain), chain.ExchangeTime, GetStrikeMultiplier(chain))
        {
            _symbol = chain.Symbol;
        }

        /// <summary>
        /// Not supported: the chain filters only ever select contracts that are already in the chain
        /// </summary>
        protected override OptionContract CreateDataInstance(Symbol symbol)
        {
            throw new InvalidOperationException($"OptionChainFilterUniverse.CreateDataInstance(): {symbol} is not part of the chain");
        }

        /// <summary>
        /// Gets the greeks of the given contract
        /// </summary>
        protected override Greeks GetGreeks(OptionContract contract) => contract.Greeks;

        /// <summary>
        /// Gets the implied volatility of the given contract
        /// </summary>
        protected override decimal GetImpliedVolatility(OptionContract contract) => contract.ImpliedVolatility;

        /// <summary>
        /// Gets the open interest of the given contract
        /// </summary>
        protected override decimal GetOpenInterest(OptionContract contract) => contract.OpenInterest;

        private static IReadOnlyList<OptionContract> GetContracts(OptionChain chain)
        {
            // The dictionary caches its values as a list that is replaced, never mutated, so it is safe to share
            return chain.Contracts.Values as IReadOnlyList<OptionContract> ?? chain.Contracts.Values.ToList();
        }

        private static BaseData GetUnderlying(OptionChain chain)
        {
            // A chain without underlying data carries an empty placeholder, which must not be used as a zero price
            var underlying = chain.Underlying;
            return underlying != null && underlying.Price != 0 ? underlying : null;
        }

        private static decimal GetStrikeMultiplier(OptionChain chain)
        {
            return chain.Contracts.Values.FirstOrDefault()?.SymbolProperties?.StrikeMultiplier ?? 1;
        }
    }
}

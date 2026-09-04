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

namespace QuantConnect.Data.Market
{
    /// <summary>
    /// The strike criterion <see cref="OptionChain.Select"/> uses to pick a contract: the strike closest to the underlying price
    /// (<see cref="AtTheMoney"/>), to a fraction of it (<see cref="Moneyness"/>), to a distance from it in price units
    /// (<see cref="FromAtm"/>) or the contract whose delta is closest to a target (<see cref="Delta"/>).
    /// Only one criterion can be expressed, so there is nothing to validate at the call site
    /// </summary>
    public class StrikeTarget
    {
        private enum Criterion
        {
            Moneyness,
            FromAtm,
            Delta
        }

        private readonly Criterion _criterion;
        private readonly decimal _value;

        /// <summary>
        /// The strike closest to the underlying price
        /// </summary>
        public static StrikeTarget AtTheMoney { get; } = new(Criterion.Moneyness, 0);

        private StrikeTarget(Criterion criterion, decimal value)
        {
            _criterion = criterion;
            _value = value;
        }

        /// <summary>
        /// The strike closest to the underlying price times one plus the given fraction, regardless of right:
        /// negative values target strikes below the underlying price, positive values above,
        /// e.g. -0.15 targets the strike closest to 85% of the underlying price
        /// </summary>
        /// <param name="moneyness">The signed distance from the underlying price as a fraction of it</param>
        public static StrikeTarget Moneyness(decimal moneyness)
        {
            return new StrikeTarget(Criterion.Moneyness, moneyness);
        }

        /// <summary>
        /// The strike closest to the underlying price plus the given distance in price units,
        /// like the universe strategy filters take, e.g. -5 targets the strike closest to 5 below the underlying price
        /// </summary>
        /// <param name="strikeFromAtm">The signed distance from the underlying price</param>
        public static StrikeTarget FromAtm(decimal strikeFromAtm)
        {
            return new StrikeTarget(Criterion.FromAtm, strikeFromAtm);
        }

        /// <summary>
        /// The contract whose absolute delta is closest to the absolute value of the given target,
        /// so a 30 delta put can be requested as either 0.3 or -0.3. Contracts without greeks are ignored
        /// </summary>
        /// <param name="targetDelta">The target delta</param>
        public static StrikeTarget Delta(decimal targetDelta)
        {
            return new StrikeTarget(Criterion.Delta, Math.Abs(targetDelta));
        }

        /// <summary>
        /// Selects the contract that best matches this target
        /// </summary>
        /// <param name="contracts">The candidate contracts</param>
        /// <param name="underlyingPrice">The underlying price, null when unknown</param>
        /// <returns>The best matching contract, or null when there is none or the target needs an unknown underlying price</returns>
        internal OptionContract Select(IEnumerable<OptionContract> contracts, decimal? underlyingPrice)
        {
            if (_criterion == Criterion.Delta)
            {
                // Contracts without greeks report a zero delta: they are excluded so a chain without greeks returns null
                return contracts
                    .Where(contract => contract.Greeks.Delta != 0)
                    .OrderBy(contract => Math.Abs(Math.Abs(contract.Greeks.Delta) - _value))
                    .ThenBy(contract => contract.Expiry)
                    .ThenBy(contract => contract.Strike)
                    .ThenBy(contract => contract.Right)
                    .FirstOrDefault();
            }

            if (!underlyingPrice.HasValue)
            {
                return null;
            }

            var targetStrike = _criterion == Criterion.FromAtm
                ? underlyingPrice.Value + _value
                : underlyingPrice.Value * (1 + _value);
            // Scaled strikes are in underlying price units, see SymbolProperties.StrikeMultiplier.
            // Ties go to the lower strike, then the nearest expiration, then calls
            return contracts
                .OrderBy(contract => Math.Abs(contract.ScaledStrike - targetStrike))
                .ThenBy(contract => contract.Strike)
                .ThenBy(contract => contract.Expiry)
                .ThenBy(contract => contract.Right)
                .FirstOrDefault();
        }

        /// <summary>
        /// Returns a string that represents the target
        /// </summary>
        public override string ToString()
        {
            return _criterion switch
            {
                Criterion.Delta => $"Delta({_value})",
                Criterion.FromAtm => $"FromAtm({_value})",
                _ => _value == 0 ? "AtTheMoney" : $"Moneyness({_value})"
            };
        }
    }
}

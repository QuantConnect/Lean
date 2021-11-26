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

namespace QuantConnect.Securities.Option.StrategyMatcher
{
    /// <summary>
    /// Stub class providing an idea towards an optimal <see cref="IOptionPositionCollectionEnumerator"/> implementation
    /// that still needs to be implemented.
    /// </summary>
    public class AbsoluteRiskOptionPositionCollectionEnumerator : IOptionPositionCollectionEnumerator
    {
        private readonly Func<Symbol, decimal> _marketPriceProvider;

        /// <summary>
        /// Intializes a new instance of the <see cref="AbsoluteRiskOptionPositionCollectionEnumerator"/> class
        /// </summary>
        /// <param name="marketPriceProvider">Function providing the current market price for a provided symbol</param>
        public AbsoluteRiskOptionPositionCollectionEnumerator(Func<Symbol, decimal> marketPriceProvider)
        {
            _marketPriceProvider = marketPriceProvider;
        }

        /// <summary>
        /// Enumerates the provided <paramref name="positions"/>. Positions enumerated first are more
        /// likely to be matched than those appearing later in the enumeration.
        /// </summary>
        public IEnumerable<OptionPosition> Enumerate(OptionPositionCollection positions)
        {
            if (positions.IsEmpty)
            {
                yield break;
            }

            var marketPrice = _marketPriceProvider(positions.Underlying);

            var longPositions = new List<OptionPosition>();
            var shortPuts = new SortedDictionary<decimal, OptionPosition>();
            var shortCalls = new SortedDictionary<decimal, OptionPosition>();
            foreach (var position in positions)
            {
                if (!position.Symbol.HasUnderlying)
                {
                    yield return position;
                }

                if (position.Quantity > 0)
                {
                    longPositions.Add(position);
                }
                else
                {
                    switch (position.Right)
                    {
                        case OptionRight.Put:
                            shortPuts.Add(position.Strike, position);
                            break;

                        case OptionRight.Call:
                            shortCalls.Add(position.Strike, position);
                            break;

                        default:
                            throw new ApplicationException(
                                "The skies are falling, the oceans rising - you're having a bad time"
                            );
                    }
                }
            }

            throw new NotImplementedException("This implementation needs to be completed.");
        }
    }
}
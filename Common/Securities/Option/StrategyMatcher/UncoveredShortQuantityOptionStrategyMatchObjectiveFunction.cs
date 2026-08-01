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

namespace QuantConnect.Securities.Option.StrategyMatcher
{
    /// <summary>
    /// Provides an implementation of <see cref="IOptionStrategyMatchObjectiveFunction"/> that minimizes the total
    /// quantity of short option contracts left uncovered, either within their matched strategy (such as the second
    /// short leg of a ladder) or unmatched entirely. Uncovered shorts are charged naked option margin, typically an
    /// order of magnitude larger than the margin of covered, risk-defined strategies, which makes this quantity a
    /// cheap and deterministic proxy for the total margin required to hold the positions.
    /// </summary>
    public class UncoveredShortQuantityOptionStrategyMatchObjectiveFunction : IOptionStrategyMatchObjectiveFunction
    {
        /// <summary>
        /// Naked short equity option margin has a floor of 10% of the underlying value (see <see cref="OptionMarginModel"/>).
        /// The matcher holds no security prices, so the short leg's strike stands in for the underlying price: a long
        /// covering a short from the credit side (higher strike for calls, lower strike for puts) is margined at the
        /// strike width, so a width beyond this fraction of the short strike likely requires more margin than leaving
        /// the short naked, and such a short is counted as uncovered instead
        /// </summary>
        private const decimal MaximumCreditCoverWidthFactor = 0.1m;

        /// <summary>
        /// Computes the score as the negated total quantity of uncovered short option contracts, so the solution
        /// covering the most short contracts wins and a solution without uncovered shorts scores zero, the maximum.
        /// A short leg is covered when its strategy holds, quantity for quantity, long options of the same right on
        /// the debit side (margin free) or within <see cref="MaximumCreditCoverWidthFactor"/> on the credit side,
        /// or the underlying lots with the offsetting sign
        /// </summary>
        public decimal ComputeScore(OptionPositionCollection input, OptionStrategyMatch match, OptionPositionCollection unmatched)
        {
            var uncovered = 0m;
            foreach (var strategy in match.Strategies)
            {
                // at the matching level underlying legs are expressed in lots,
                // long lots cover short calls and short lots cover short puts
                var underlyingLots = strategy.UnderlyingLegs.Sum(leg => leg.Quantity);
                uncovered += GetUncoveredQuantity(strategy.OptionLegs, OptionRight.Call, Math.Max(0, underlyingLots));
                uncovered += GetUncoveredQuantity(strategy.OptionLegs, OptionRight.Put, Math.Max(0, -underlyingLots));
            }

            foreach (var position in unmatched)
            {
                if (position.Quantity < 0 && !position.IsUnderlying)
                {
                    // unmatched short options fall through to stand-alone groups charged naked option margin
                    uncovered -= position.Quantity;
                }
            }

            return -uncovered;
        }

        /// <summary>
        /// Determines the quantity of short contracts of the given right which the strategy's own long legs and
        /// underlying lots don't cover at a margin below the naked short margin proxy
        /// </summary>
        private static decimal GetUncoveredQuantity(IEnumerable<OptionStrategy.OptionLegData> optionLegs, OptionRight right,
            decimal underlyingCover)
        {
            List<StrikeQuantity> shorts = null;
            List<StrikeQuantity> longs = null;
            foreach (var leg in optionLegs)
            {
                if (leg.Right != right || leg.Quantity == 0)
                {
                    continue;
                }

                if (leg.Quantity < 0)
                {
                    (shorts ??= new List<StrikeQuantity>()).Add(new StrikeQuantity(leg.Strike, -leg.Quantity));
                }
                else
                {
                    (longs ??= new List<StrikeQuantity>()).Add(new StrikeQuantity(leg.Strike, leg.Quantity));
                }
            }

            if (shorts == null)
            {
                return 0;
            }

            // debit-side longs cover for free: at or below the short strike for calls, at or above for puts. sorting
            // ascending for calls (descending for puts) makes each short's set of debit-side longs contain the sets
            // of the shorts before it, so covering shorts in order never wastes a long another short needed. it also
            // leaves credit-side longs enumerated nearest first, minimizing the width of credit-side covers below
            var sign = right == OptionRight.Call ? 1 : -1;
            shorts.Sort((left, other) => sign * left.Strike.CompareTo(other.Strike));
            longs?.Sort((left, other) => sign * left.Strike.CompareTo(other.Strike));

            foreach (var shortLeg in shorts)
            {
                if (longs != null)
                {
                    foreach (var longLeg in longs)
                    {
                        if (shortLeg.Quantity == 0)
                        {
                            break;
                        }

                        if (sign * (shortLeg.Strike - longLeg.Strike) >= 0)
                        {
                            Cover(shortLeg, longLeg);
                        }
                    }
                }

                var lots = Math.Min(shortLeg.Quantity, underlyingCover);
                shortLeg.Quantity -= lots;
                underlyingCover -= lots;
            }

            var uncovered = 0m;
            foreach (var shortLeg in shorts)
            {
                if (longs != null)
                {
                    foreach (var longLeg in longs)
                    {
                        if (shortLeg.Quantity == 0)
                        {
                            break;
                        }

                        // a credit-side long caps the risk at the strike width, worth it only below the naked margin proxy
                        if (sign * (longLeg.Strike - shortLeg.Strike) <= MaximumCreditCoverWidthFactor * shortLeg.Strike)
                        {
                            Cover(shortLeg, longLeg);
                        }
                    }
                }

                uncovered += shortLeg.Quantity;
            }

            return uncovered;
        }

        private static void Cover(StrikeQuantity shortLeg, StrikeQuantity longLeg)
        {
            var quantity = Math.Min(shortLeg.Quantity, longLeg.Quantity);
            shortLeg.Quantity -= quantity;
            longLeg.Quantity -= quantity;
        }

        private sealed class StrikeQuantity
        {
            public decimal Strike { get; }
            public decimal Quantity { get; set; }

            public StrikeQuantity(decimal strike, decimal quantity)
            {
                Strike = strike;
                Quantity = quantity;
            }
        }
    }
}

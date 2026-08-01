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
        /// A short leg is covered when its strategy holds, quantity for quantity, the underlying lots with the
        /// offsetting sign or long options of the same right which outlive it and whose strike is on the debit side
        /// of the short strike or within <see cref="MaximumCreditCoverWidthFactor"/> of it on the credit side
        /// </summary>
        public decimal ComputeScore(OptionPositionCollection input, OptionStrategyMatch match, OptionPositionCollection unmatched)
        {
            var uncovered = 0m;
            foreach (var strategy in match.Strategies)
            {
                // at the matching level underlying legs are expressed in lots,
                // long lots cover short calls and short lots cover short puts
                var underlyingLots = 0m;
                for (var i = 0; i < strategy.UnderlyingLegs.Count; i++)
                {
                    underlyingLots += strategy.UnderlyingLegs[i].Quantity;
                }

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
        private static decimal GetUncoveredQuantity(List<OptionStrategy.OptionLegData> legs, OptionRight right, decimal underlyingCover)
        {
            var shortLegCount = 0;
            var longLegCount = 0;
            var shortQuantity = 0m;
            for (var i = 0; i < legs.Count; i++)
            {
                var leg = legs[i];
                if (leg.Right != right || leg.Quantity == 0)
                {
                    continue;
                }

                if (leg.Quantity < 0)
                {
                    shortLegCount++;
                    shortQuantity -= leg.Quantity;
                }
                else
                {
                    longLegCount++;
                }
            }

            if (shortLegCount == 0)
            {
                // nothing short of this right, the most common case by far
                return 0;
            }

            if (longLegCount == 0)
            {
                // no long of this right to pair with, only the underlying lots can cover
                return Math.Max(0, shortQuantity - underlyingCover);
            }

            // calls are covered by lower strikes and puts by higher ones
            var sign = right == OptionRight.Call ? 1 : -1;

            if (shortLegCount == 1)
            {
                // a single short leg takes from every long leg allowed to cover it, no ordering required
                var shortStrike = 0m;
                var shortExpiration = DateTime.MinValue;
                for (var i = 0; i < legs.Count; i++)
                {
                    if (legs[i].Right == right && legs[i].Quantity < 0)
                    {
                        shortStrike = legs[i].Strike;
                        shortExpiration = legs[i].Expiration;
                        break;
                    }
                }

                var cover = underlyingCover;
                for (var i = 0; i < legs.Count; i++)
                {
                    var leg = legs[i];
                    if (leg.Right == right && leg.Quantity > 0
                        && Covers(sign, shortStrike, shortExpiration, leg.Strike, leg.Expiration))
                    {
                        cover += leg.Quantity;
                    }
                }

                return Math.Max(0, shortQuantity - cover);
            }

            // several short legs of the same right, which only ladders and short butterflies produce. the set of long
            // legs allowed to cover a short grows with the short's strike for calls, and shrinks for puts, so the sets
            // are nested: taking from the shorts in that order never spends a long leg that a later short needed
            var shortStrikes = new decimal[shortLegCount];
            var shortQuantities = new decimal[shortLegCount];
            var shortExpirations = new DateTime[shortLegCount];
            var longStrikes = new decimal[longLegCount];
            var longQuantities = new decimal[longLegCount];
            var longExpirations = new DateTime[longLegCount];
            var shorts = 0;
            var longs = 0;
            for (var i = 0; i < legs.Count; i++)
            {
                var leg = legs[i];
                if (leg.Right != right || leg.Quantity == 0)
                {
                    continue;
                }

                if (leg.Quantity < 0)
                {
                    // insertion sort: ascending strike for calls, descending for puts
                    var index = shorts++;
                    while (index > 0 && sign * (shortStrikes[index - 1] - leg.Strike) > 0)
                    {
                        shortStrikes[index] = shortStrikes[index - 1];
                        shortQuantities[index] = shortQuantities[index - 1];
                        shortExpirations[index] = shortExpirations[index - 1];
                        index--;
                    }
                    shortStrikes[index] = leg.Strike;
                    shortQuantities[index] = -leg.Quantity;
                    shortExpirations[index] = leg.Expiration;
                }
                else
                {
                    longStrikes[longs] = leg.Strike;
                    longQuantities[longs] = leg.Quantity;
                    longExpirations[longs++] = leg.Expiration;
                }
            }

            var uncovered = 0m;
            for (var i = 0; i < shortLegCount; i++)
            {
                var remaining = shortQuantities[i];
                for (var j = 0; j < longLegCount && remaining > 0; j++)
                {
                    if (longQuantities[j] > 0
                        && Covers(sign, shortStrikes[i], shortExpirations[i], longStrikes[j], longExpirations[j]))
                    {
                        var quantity = Math.Min(remaining, longQuantities[j]);
                        remaining -= quantity;
                        longQuantities[j] -= quantity;
                    }
                }

                var lots = Math.Min(remaining, underlyingCover);
                remaining -= lots;
                underlyingCover -= lots;
                uncovered += remaining;
            }

            return uncovered;
        }

        /// <summary>
        /// Determines whether a long leg covers a short leg of the same right at a margin below the naked short margin
        /// proxy. The long must outlive the short, since a long expiring first leaves the short naked for the rest of
        /// its life and the margin models charge those groups, the short calendar spreads, the naked short margin. It
        /// must also sit on the debit side of the short strike, where the width is not positive and the strategy
        /// requires no margin at all, or up to <see cref="MaximumCreditCoverWidthFactor"/> beyond it
        /// </summary>
        private static bool Covers(int sign, decimal shortStrike, DateTime shortExpiration, decimal longStrike, DateTime longExpiration)
        {
            return longExpiration >= shortExpiration
                && sign * (longStrike - shortStrike) <= MaximumCreditCoverWidthFactor * shortStrike;
        }
    }
}

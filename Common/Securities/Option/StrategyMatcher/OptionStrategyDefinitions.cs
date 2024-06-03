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
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;

namespace QuantConnect.Securities.Option.StrategyMatcher
{
    /// <summary>
    /// Provides a listing of pre-defined <see cref="OptionStrategyDefinition"/>
    /// These definitions are blueprints for <see cref="OptionStrategy"/> instances.
    /// Factory functions for those can be found at <see cref="OptionStrategies"/>
    /// </summary>
    public static class OptionStrategyDefinitions
    {
        // lazy since 'AllDefinitions' is at top of file and static members are evaluated in order
        private static readonly Lazy<ImmutableList<OptionStrategyDefinition>> All
            = new Lazy<ImmutableList<OptionStrategyDefinition>>(() =>
                typeof(OptionStrategyDefinitions)
                    .GetProperties(BindingFlags.Public | BindingFlags.Static)
                    .Where(property => property.PropertyType == typeof(OptionStrategyDefinition))
                    .Select(property => (OptionStrategyDefinition) property.GetValue(null))
                    .ToImmutableList()
            );

        /// <summary>
        /// Collection of all OptionStrategyDefinitions
        /// </summary>
        public static ImmutableList<OptionStrategyDefinition> AllDefinitions
        {
            get
            {
                var strategies = All.Value;

                return strategies
                    .SelectMany(optionStrategy => {
                        // when selling the strategy can get reverted and it's still valid, we need the definition to match against
                        var inverted = new OptionStrategyDefinition(optionStrategy.Name, optionStrategy.UnderlyingLots * -1,
                            optionStrategy.Legs.Select(leg => new OptionStrategyLegDefinition(leg.Right, leg.Quantity * -1, leg)));

                        if (strategies.Any(strategy => strategy.UnderlyingLots == inverted.UnderlyingLots
                            && strategy.Legs.Count == inverted.Legs.Count
                            && strategy.Legs.All(leg => inverted.Legs.
                                Any(invertedLeg => invertedLeg.Right == leg.Right
                                    && leg.Quantity == invertedLeg.Quantity
                                    && leg.All(predicate => invertedLeg.Any(invertedPredicate => invertedPredicate.ToString() == predicate.ToString()))))))
                        {
                            // some strategies inverted have a different name we already know, let's skip those
                            return new[] { optionStrategy };
                        }
                        return new[] { optionStrategy, inverted };
                    })
                    .ToImmutableList();
            }
        }

        /// <summary>
        /// Hold 1 lot of the underlying and sell 1 call contract
        /// </summary>
        /// <remarks>Inverse of the <see cref="ProtectiveCall"/></remarks>
        public static OptionStrategyDefinition CoveredCall { get; }
            = OptionStrategyDefinition.Create("Covered Call", 1,
                OptionStrategyDefinition.CallLeg(-1)
            );

        /// <summary>
        /// Hold -1 lot of the underlying and buy 1 call contract
        /// </summary>
        /// <remarks>Inverse of the <see cref="CoveredCall"/></remarks>
        public static OptionStrategyDefinition ProtectiveCall { get; }
            = OptionStrategyDefinition.Create("Protective Call", -1,
                OptionStrategyDefinition.CallLeg(1)
            );

        /// <summary>
        /// Hold -1 lot of the underlying and sell 1 put contract
        /// </summary>
        /// <remarks>Inverse of the <see cref="ProtectivePut"/></remarks>
        public static OptionStrategyDefinition CoveredPut { get; }
            = OptionStrategyDefinition.Create("Covered Put", -1,
                OptionStrategyDefinition.PutLeg(-1)
            );

        /// <summary>
        /// Hold 1 lot of the underlying and buy 1 put contract
        /// </summary>
        /// <remarks>Inverse of the <see cref="CoveredPut"/></remarks>
        public static OptionStrategyDefinition ProtectivePut { get; }
            = OptionStrategyDefinition.Create("Protective Put", 1,
                OptionStrategyDefinition.PutLeg(1)
            );

        /// <summary>
        /// Hold 1 lot of the underlying, sell 1 call contract and buy 1 put contract.
        /// The strike price of the short call is below the strike of the long put with the same expiration.
        /// </summary>
        /// <remarks>Combination of <see cref="CoveredCall"/> and <see cref="ProtectivePut"/></remarks>
        public static OptionStrategyDefinition ProtectiveCollar { get; }
            = OptionStrategyDefinition.Create("Protective Collar", 1,
                OptionStrategyDefinition.CallLeg(-1),
                OptionStrategyDefinition.PutLeg(1, (legs, p) => p.Strike < legs[0].Strike,
                                                   (legs, p) => p.Expiration == legs[0].Expiration)
            );

        /// <summary>
        /// Hold 1 lot of the underlying, sell 1 call contract and buy 1 put contract.
        /// The strike price of the call and put are the same, with the same expiration.
        /// </summary>
        /// <remarks>A special case of <see cref="ProtectiveCollar"/>
        public static OptionStrategyDefinition Conversion { get; }
            = OptionStrategyDefinition.Create("Conversion", 1,
                OptionStrategyDefinition.CallLeg(-1),
                OptionStrategyDefinition.PutLeg(1, (legs, p) => p.Strike == legs[0].Strike,
                                                   (legs, p) => p.Expiration == legs[0].Expiration)
            );

        /// <summary>
        /// Hold 1 lot of the underlying, sell 1 call contract and buy 1 put contract.
        /// The strike price of the call and put are the same, with the same expiration.
        /// </summary>
        /// <remarks>Inverse of <see cref="Conversion"/>
        public static OptionStrategyDefinition ReverseConversion { get; }
            = OptionStrategyDefinition.Create("Reverse Conversion", -1,
                OptionStrategyDefinition.CallLeg(1),
                OptionStrategyDefinition.PutLeg(-1, (legs, p) => p.Strike == legs[0].Strike,
                                                   (legs, p) => p.Expiration == legs[0].Expiration)
            );

        /// <summary>
        /// Sell 1 call contract without holding the underlying
        /// </summary>
        public static OptionStrategyDefinition NakedCall { get; }
            = OptionStrategyDefinition.Create("Naked Call",
                OptionStrategyDefinition.CallLeg(-1)
            );

        /// <summary>
        /// Sell 1 put contract without holding the underlying
        /// </summary>
        public static OptionStrategyDefinition NakedPut { get; }
            = OptionStrategyDefinition.Create("Naked Put",
                OptionStrategyDefinition.PutLeg(-1)
            );

        /// <summary>
        /// Bear Call Spread strategy consists of two calls with the same expiration but different strikes.
        /// The strike price of the short call is below the strike of the long call. This is a credit spread.
        /// </summary>
        public static OptionStrategyDefinition BearCallSpread { get; }
            = OptionStrategyDefinition.Create("Bear Call Spread",
                OptionStrategyDefinition.CallLeg(-1),
                OptionStrategyDefinition.CallLeg(+1, (legs, p) => p.Strike > legs[0].Strike,
                                                     (legs, p) => p.Expiration == legs[0].Expiration)
            );

        /// <summary>
        /// Bear Put Spread strategy consists of two puts with the same expiration but different strikes.
        /// The strike price of the short put is below the strike of the long put. This is a debit spread.
        /// </summary>
        public static OptionStrategyDefinition BearPutSpread { get; }
            = OptionStrategyDefinition.Create("Bear Put Spread",
                OptionStrategyDefinition.PutLeg(1),
                OptionStrategyDefinition.PutLeg(-1, (legs, p) => p.Strike < legs[0].Strike,
                                                    (legs, p) => p.Expiration == legs[0].Expiration)
            );

        /// <summary>
        /// Bull Call Spread strategy consists of two calls with the same expiration but different strikes.
        /// The strike price of the short call is higher than the strike of the long call. This is a debit spread.
        /// </summary>
        public static OptionStrategyDefinition BullCallSpread { get; }
            = OptionStrategyDefinition.Create("Bull Call Spread",
                OptionStrategyDefinition.CallLeg(+1),
                OptionStrategyDefinition.CallLeg(-1, (legs, p) => p.Strike > legs[0].Strike,
                                                     (legs, p) => p.Expiration == legs[0].Expiration)
            );

        /// <summary>
        /// Method creates new Bull Put Spread strategy, that consists of two puts with the same expiration but
        /// different strikes. The strike price of the short put is above the strike of the long put. This is a
        /// credit spread.
        /// </summary>
        public static OptionStrategyDefinition BullPutSpread { get; }
            = OptionStrategyDefinition.Create("Bull Put Spread",
                OptionStrategyDefinition.PutLeg(-1),
                OptionStrategyDefinition.PutLeg(+1, (legs, p) => p.Strike < legs[0].Strike,
                                                    (legs, p) => p.Expiration == legs[0].Expiration)
            );

        /// <summary>
        /// Straddle strategy is a combination of buying a call and buying a put, both with the same strike price
        /// and expiration.
        /// </summary>
        public static OptionStrategyDefinition Straddle { get; }
            = OptionStrategyDefinition.Create("Straddle",
                OptionStrategyDefinition.CallLeg(+1),
                OptionStrategyDefinition.PutLeg(+1, (legs, p) => p.Strike == legs[0].Strike,
                                                    (legs, p) => p.Expiration == legs[0].Expiration)
            );

        /// <summary>
        /// Short Straddle strategy is a combination of selling a call and selling a put, both with the same strike price
        /// and expiration.
        /// </summary>
        /// <remarks>Inverse of the <see cref="Straddle"/></remarks>
        public static OptionStrategyDefinition ShortStraddle { get; }
            = OptionStrategyDefinition.Create("Short Straddle",
                OptionStrategyDefinition.CallLeg(-1),
                OptionStrategyDefinition.PutLeg(-1, (legs, p) => p.Strike == legs[0].Strike,
                                                    (legs, p) => p.Expiration == legs[0].Expiration)
            );

        /// <summary>
        /// Strangle strategy consists of buying a call option and a put option with the same expiration date.
        /// The strike price of the call is above the strike of the put.
        /// </summary>
        public static OptionStrategyDefinition Strangle { get; }
            = OptionStrategyDefinition.Create("Strangle",
                OptionStrategyDefinition.CallLeg(+1),
                OptionStrategyDefinition.PutLeg(+1, (legs, p) => p.Strike < legs[0].Strike,
                                                    (legs, p) => p.Expiration == legs[0].Expiration)
            );

        /// <summary>
        /// Strangle strategy consists of selling a call option and a put option with the same expiration date.
        /// The strike price of the call is above the strike of the put.
        /// </summary>
        /// <remarks>Inverse of the <see cref="Strangle"/></remarks>
        public static OptionStrategyDefinition ShortStrangle { get; }
            = OptionStrategyDefinition.Create("Short Strangle",
                OptionStrategyDefinition.CallLeg(-1),
                OptionStrategyDefinition.PutLeg(-1, (legs, p) => p.Strike < legs[0].Strike,
                                                    (legs, p) => p.Expiration == legs[0].Expiration)
            );

        /// <summary>
        /// Short Butterfly Call strategy consists of two short calls at a middle strike, and one long call each at a lower
        /// and upper strike. The upper and lower strikes must both be equidistant from the middle strike.
        /// </summary>
        public static OptionStrategyDefinition ButterflyCall { get; }
            = OptionStrategyDefinition.Create("Butterfly Call",
                OptionStrategyDefinition.CallLeg(+1),
                OptionStrategyDefinition.CallLeg(-2, (legs, p) => p.Strike >= legs[0].Strike,
                                                     (legs, p) => p.Expiration == legs[0].Expiration),
                OptionStrategyDefinition.CallLeg(+1, (legs, p) => p.Strike >= legs[1].Strike,
                                                     (legs, p) => p.Expiration == legs[0].Expiration,
                                                     (legs, p) => p.Strike - legs[1].Strike == legs[1].Strike - legs[0].Strike)
            );

        /// <summary>
        /// Butterfly Call strategy consists of two long calls at a middle strike, and one short call each at a lower
        /// and upper strike. The upper and lower strikes must both be equidistant from the middle strike.
        /// </summary>
        public static OptionStrategyDefinition ShortButterflyCall { get; }
            = OptionStrategyDefinition.Create("Short Butterfly Call",
                OptionStrategyDefinition.CallLeg(-1),
                OptionStrategyDefinition.CallLeg(+2, (legs, p) => p.Strike >= legs[0].Strike,
                    (legs, p) => p.Expiration == legs[0].Expiration),
                OptionStrategyDefinition.CallLeg(-1, (legs, p) => p.Strike >= legs[1].Strike,
                    (legs, p) => p.Expiration == legs[0].Expiration,
                    (legs, p) => p.Strike - legs[1].Strike == legs[1].Strike - legs[0].Strike)
            );

        /// <summary>
        /// Butterfly Put strategy consists of two short puts at a middle strike, and one long put each at a lower and
        /// upper strike. The upper and lower strikes must both be equidistant from the middle strike.
        /// </summary>
        public static OptionStrategyDefinition ButterflyPut { get; }
            = OptionStrategyDefinition.Create("Butterfly Put",
                OptionStrategyDefinition.PutLeg(+1),
                OptionStrategyDefinition.PutLeg(-2, (legs, p) => p.Strike >= legs[0].Strike,
                                                    (legs, p) => p.Expiration == legs[0].Expiration),
                OptionStrategyDefinition.PutLeg(+1, (legs, p) => p.Strike >= legs[1].Strike,
                                                    (legs, p) => p.Expiration == legs[0].Expiration,
                                                    (legs, p) => p.Strike - legs[1].Strike == legs[1].Strike - legs[0].Strike)
            );


        /// <summary>
        /// Short Butterfly Put strategy consists of two long puts at a middle strike, and one short put each at a lower and
        /// upper strike. The upper and lower strikes must both be equidistant from the middle strike.
        /// </summary>
        public static OptionStrategyDefinition ShortButterflyPut { get; }
            = OptionStrategyDefinition.Create("Short Butterfly Put",
                OptionStrategyDefinition.PutLeg(-1),
                OptionStrategyDefinition.PutLeg(+2, (legs, p) => p.Strike >= legs[0].Strike,
                    (legs, p) => p.Expiration == legs[0].Expiration),
                OptionStrategyDefinition.PutLeg(-1, (legs, p) => p.Strike >= legs[1].Strike,
                    (legs, p) => p.Expiration == legs[0].Expiration,
                    (legs, p) => p.Strike - legs[1].Strike == legs[1].Strike - legs[0].Strike)
            );

        /// <summary>
        /// Call Calendar Spread strategy is a short one call option and long a second call option with a more distant
        /// expiration.
        /// </summary>
        public static OptionStrategyDefinition CallCalendarSpread { get; }
            = OptionStrategyDefinition.Create("Call Calendar Spread",
                OptionStrategyDefinition.CallLeg(-1),
                OptionStrategyDefinition.CallLeg(+1, (legs, p) => p.Strike == legs[0].Strike,
                                                     (legs, p) => p.Expiration > legs[0].Expiration)
            );

        /// <summary>
        /// Short Call Calendar Spread strategy is long one call option and short a second call option with a more distant
        /// expiration.
        /// </summary>
        /// <remarks>Inverse of the <see cref="CallCalendarSpread"/></remarks>
        public static OptionStrategyDefinition ShortCallCalendarSpread { get; }
            = OptionStrategyDefinition.Create("Short Call Calendar Spread",
                OptionStrategyDefinition.CallLeg(+1),
                OptionStrategyDefinition.CallLeg(-1, (legs, p) => p.Strike == legs[0].Strike,
                                                     (legs, p) => p.Expiration > legs[0].Expiration)
            );

        /// <summary>
        /// Put Calendar Spread strategy is a short one put option and long a second put option with a more distant
        /// expiration.
        /// </summary>
        public static OptionStrategyDefinition PutCalendarSpread { get; }
            = OptionStrategyDefinition.Create("Put Calendar Spread",
                OptionStrategyDefinition.PutLeg(-1),
                OptionStrategyDefinition.PutLeg(+1, (legs, p) => p.Strike == legs[0].Strike,
                                                    (legs, p) => p.Expiration > legs[0].Expiration)
            );

        /// <summary>
        /// Short Put Calendar Spread strategy is long one put option and short a second put option with a more distant
        /// expiration.
        /// </summary>
        /// <remarks>Inverse of the <see cref="PutCalendarSpread"/></remarks>
        public static OptionStrategyDefinition ShortPutCalendarSpread { get; }
            = OptionStrategyDefinition.Create("Short Put Calendar Spread",
                OptionStrategyDefinition.PutLeg(+1),
                OptionStrategyDefinition.PutLeg(-1, (legs, p) => p.Strike == legs[0].Strike,
                                                    (legs, p) => p.Expiration > legs[0].Expiration)
            );

        /// <summary>
        /// Iron Condor strategy is buying a put, selling a put with a higher strike price, selling a call and buying a call with a higher strike price.
        /// All at the same expiration date
        /// </summary>
        public static OptionStrategyDefinition IronCondor { get; }
            = OptionStrategyDefinition.Create("Iron Condor",
                OptionStrategyDefinition.PutLeg(+1),
                OptionStrategyDefinition.PutLeg(-1, (legs, p) => p.Strike > legs[0].Strike,
                    (legs, p) => p.Expiration == legs[0].Expiration),
                OptionStrategyDefinition.CallLeg(-1, (legs, p) => p.Strike > legs[1].Strike,
                    (legs, p) => p.Expiration == legs[0].Expiration),
                OptionStrategyDefinition.CallLeg(1, (legs, p) => p.Strike > legs[2].Strike,
                    (legs, p) => p.Expiration == legs[0].Expiration)
            );

        /// <summary>
        /// Long Box Spread strategy is long 1 call and short 1 put with the same strike,
        /// while short 1 call and long 1 put with a higher, same strike. All options have the same expiry.
        /// expiration.
        /// </summary>
        public static OptionStrategyDefinition BoxSpread { get; }
            = OptionStrategyDefinition.Create("Box Spread",
                OptionStrategyDefinition.PutLeg(+1),
                OptionStrategyDefinition.PutLeg(-1, (legs, p) => p.Strike < legs[0].Strike,
                                                    (legs, p) => p.Expiration == legs[0].Expiration),
                OptionStrategyDefinition.CallLeg(+1, (legs, c) => c.Strike == legs[1].Strike,
                                                    (legs, c) => c.Expiration == legs[0].Expiration),
                OptionStrategyDefinition.CallLeg(-1, (legs, c) => c.Strike == legs[0].Strike,
                                                    (legs, c) => c.Expiration == legs[0].Expiration)
            );

        /// <summary>
        /// Short Box Spread strategy is short 1 call and long 1 put with the same strike,
        /// while long 1 call and short 1 put with a higher, same strike. All options have the same expiry.
        /// expiration.
        /// </summary>
        public static OptionStrategyDefinition ShortBoxSpread { get; }
            = OptionStrategyDefinition.Create("Short Box Spread",
                OptionStrategyDefinition.PutLeg(-1),
                OptionStrategyDefinition.PutLeg(+1, (legs, p) => p.Strike < legs[0].Strike,
                                                    (legs, p) => p.Expiration == legs[0].Expiration),
                OptionStrategyDefinition.CallLeg(-1, (legs, c) => c.Strike == legs[1].Strike,
                                                    (legs, c) => c.Expiration == legs[0].Expiration),
                OptionStrategyDefinition.CallLeg(+1, (legs, c) => c.Strike == legs[0].Strike,
                                                    (legs, c) => c.Expiration == legs[0].Expiration)
            );
    }
}

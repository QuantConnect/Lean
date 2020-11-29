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

        public static ImmutableList<OptionStrategyDefinition> AllDefinitions => All.Value;

        /// <summary>
        /// Hold 1 lot of the underlying and sell 1 call contract
        /// </summary>
        public static OptionStrategyDefinition CoveredCall { get; }
            = OptionStrategyDefinition.Create("Covered Call", 1,
                OptionStrategyDefinition.CallLeg(-1)
            );

        /// <summary>
        /// Bear Call Spread strategy consists of two calls with the same expiration but different strikes.
        /// The strike price of the short call is below the strike of the long call. This is a credit spread.
        /// </summary>
        public static OptionStrategyDefinition BearCallSpread { get; }
            = OptionStrategyDefinition.Create("Bear Call Spread",
                OptionStrategyDefinition.CallLeg(-1),
                OptionStrategyDefinition.CallLeg(+1, (legs, p) => p.Strike <= legs[0].Strike,
                                                     (legs, p) => p.Expiration == legs[0].Expiration)
            );

        /// <summary>
        /// Bear Put Spread strategy consists of two puts with the same expiration but different strikes.
        /// The strike price of the short put is below the strike of the long put. This is a debit spread.
        /// </summary>
        public static OptionStrategyDefinition BearPutSpread { get; }
            = OptionStrategyDefinition.Create("Bear Put Spread",
                OptionStrategyDefinition.PutLeg(1),
                OptionStrategyDefinition.PutLeg(-1, (legs, p) => p.Strike >= legs[0].Strike,
                                                    (legs, p) => p.Expiration == legs[0].Expiration)
            );

        /// <summary>
        /// Bull Call Spread strategy consists of two calls with the same expiration but different strikes.
        /// The strike price of the short call is higher than the strike of the long call. This is a debit spread.
        /// </summary>
        public static OptionStrategyDefinition BullCallSpread { get; }
            = OptionStrategyDefinition.Create("Bull Call Spread",
                OptionStrategyDefinition.CallLeg(+1),
                OptionStrategyDefinition.CallLeg(-1, (legs, p) => p.Strike <= legs[0].Strike,
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
                OptionStrategyDefinition.PutLeg(+1, (legs, p) => p.Strike <= legs[0].Strike,
                                                    (legs, p) => p.Expiration == legs[0].Expiration)
            );

        /// <summary>
        /// Straddle strategy is a combination of buying a call and buying a put, both with the same strike price
        /// and expiration.
        /// </summary>
        public static OptionStrategyDefinition Straddle { get; }
            = OptionStrategyDefinition.Create("Straddle",
                OptionStrategyDefinition.CallLeg(+1),
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
                OptionStrategyDefinition.PutLeg(+1, (legs, p) => p.Strike <= legs[0].Strike,
                                                    (legs, p) => p.Expiration == legs[0].Expiration)
            );

        /// <summary>
        /// Call Butterfly strategy consists of two short calls at a middle strike, and one long call each at a lower
        /// and upper strike. The upper and lower strikes must both be equidistant from the middle strike.
        /// </summary>
        public static OptionStrategyDefinition CallButterfly { get; }
            = OptionStrategyDefinition.Create("Call Butterfly",
                OptionStrategyDefinition.CallLeg(+1),
                OptionStrategyDefinition.CallLeg(-2, (legs, p) => p.Strike >= legs[0].Strike,
                                                     (legs, p) => p.Expiration == legs[0].Expiration),
                OptionStrategyDefinition.CallLeg(+1, (legs, p) => p.Strike >= legs[1].Strike,
                                                     (legs, p) => p.Expiration == legs[0].Expiration,
                                                     (legs, p) => p.Strike - legs[1].Strike == legs[1].Strike - legs[0].Strike)
            );

        /// <summary>
        /// Put Butterfly strategy consists of two short puts at a middle strike, and one long put each at a lower and
        /// upper strike. The upper and lower strikes must both be equidistant from the middle strike.
        /// </summary>
        public static OptionStrategyDefinition PutButterfly { get; }
            = OptionStrategyDefinition.Create("Put Butterfly",
                OptionStrategyDefinition.PutLeg(+1),
                OptionStrategyDefinition.PutLeg(-2, (legs, p) => p.Strike >= legs[0].Strike,
                                                    (legs, p) => p.Expiration == legs[0].Expiration),
                OptionStrategyDefinition.PutLeg(+1, (legs, p) => p.Strike >= legs[1].Strike,
                                                    (legs, p) => p.Expiration == legs[0].Expiration,
                                                    (legs, p) => p.Strike - legs[1].Strike == legs[1].Strike - legs[0].Strike)
            );

        /// <summary>
        /// Call Calendar Spread strategy is a short one call option and long a second call option with a more distant
        /// expiration.
        /// </summary>
        public static OptionStrategyDefinition CallCalendarSpread { get; }
            = OptionStrategyDefinition.Create("Call Calendar Spread",
                OptionStrategyDefinition.CallLeg(+1),
                OptionStrategyDefinition.CallLeg(+1, (legs, p) => p.Strike == legs[0].Strike,
                                                     (legs, p) => p.Expiration <= legs[0].Expiration)
            );

        /// <summary>
        /// Put Calendar Spread strategy is a short one put option and long a second put option with a more distant
        /// expiration.
        /// </summary>
        public static OptionStrategyDefinition PutCalendarSpread { get; }
            = OptionStrategyDefinition.Create("Put Calendar Spread",
                OptionStrategyDefinition.PutLeg(+1),
                OptionStrategyDefinition.PutLeg(+1, (legs, p) => p.Strike == legs[0].Strike,
                                                    (legs, p) => p.Expiration <= legs[0].Expiration)
            );
    }
}

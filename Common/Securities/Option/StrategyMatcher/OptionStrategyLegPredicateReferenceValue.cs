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
    /// Provides an implementation of <see cref="IOptionStrategyLegPredicateReferenceValue"/> that references an option
    /// leg from the list of already matched legs by index. The property referenced is defined by <see cref="PredicateTargetValue"/>
    /// </summary>
    public class OptionStrategyLegPredicateReferenceValue : IOptionStrategyLegPredicateReferenceValue
    {
        private readonly int _index;

        /// <summary>
        /// Gets the target of this value
        /// </summary>
        public PredicateTargetValue Target { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="IOptionStrategyLegPredicateReferenceValue"/> class
        /// </summary>
        /// <param name="index">The legs list index</param>
        /// <param name="target">The property value being referenced</param>
        public OptionStrategyLegPredicateReferenceValue(int index, PredicateTargetValue target)
        {
            _index = index;
            Target = target;
        }

        /// <summary>
        /// Resolves the value of the comparand specified in an <see cref="OptionStrategyLegPredicate"/>.
        /// For example, the predicate may include ... > legs[0].Strike, and upon evaluation, we need to
        /// be able to extract leg[0].Strike for the currently contemplated set of legs adhering to a
        /// strategy's definition.
        /// </summary>
        public object Resolve(IReadOnlyList<OptionPosition> legs)
        {
            if (_index >= legs.Count)
            {
                throw new InvalidOperationException(
                    $"OptionStrategyLegPredicateReferenceValue[{_index}] is unable to be resolved. Only {legs.Count} legs were provided."
                );
            }

            var leg = legs[_index];
            switch (Target)
            {
                case PredicateTargetValue.Right:      return leg.Right;
                case PredicateTargetValue.Strike:     return leg.Strike;
                case PredicateTargetValue.Expiration: return leg.Expiration;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
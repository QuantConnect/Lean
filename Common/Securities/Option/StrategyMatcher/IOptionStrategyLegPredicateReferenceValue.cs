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

using System.Collections.Generic;

namespace QuantConnect.Securities.Option.StrategyMatcher
{
    /// <summary>
    /// When decoding leg predicates, we extract the value we're comparing against
    /// If we're comparing against another leg's value (such as legs[0].Strike), then
    /// we'll create a OptionStrategyLegPredicateReferenceValue. If we're comparing against a literal/constant value,
    /// then we'll create a ConstantOptionStrategyLegPredicateReferenceValue. These reference values are used to slice
    /// the <see cref="OptionPositionCollection"/> to only include positions matching the
    /// predicate.
    /// </summary>
    public interface IOptionStrategyLegPredicateReferenceValue
    {
        /// <summary>
        /// Gets the target of this value
        /// </summary>
        PredicateTargetValue Target { get; }

        /// <summary>
        /// Resolves the value of the comparand specified in an <see cref="OptionStrategyLegPredicate"/>.
        /// For example, the predicate may include ... > legs[0].Strike, and upon evaluation, we need to
        /// be able to extract leg[0].Strike for the currently contemplated set of legs adhering to a
        /// strategy's definition.
        /// </summary>
        object Resolve(IReadOnlyList<OptionPosition> legs);
    }
}
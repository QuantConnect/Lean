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
    /// Defines options that influence how the matcher operates.
    /// </summary>
    /// <remarks>
    /// Many properties in this type are not implemented in the matcher but are provided to document
    /// the types of things that can be added to the matcher in the future as necessary. Some of the
    /// features contemplated in this class would require updating the various matching/filtering/slicing
    /// functions to accept these options, or a particular property. This is the case for the enumerators
    /// which would be used to prioritize which positions to try and match first. A great implementation
    /// of the <see cref="IOptionPositionCollectionEnumerator"/> would be to yield positions with the
    /// highest margin requirements first. At time of writing, the goal is to achieve a workable rev0,
    /// and we can later improve the efficiency/optimization of the matching process.
    /// </remarks>
    public class OptionStrategyMatcherOptions
    {
        /// <summary>
        /// The maximum amount of time spent trying to find an optimal solution.
        /// </summary>
        public TimeSpan MaximumDuration { get; }

        /// <summary>
        /// The maximum number of matches to evaluate for the entire portfolio.
        /// </summary>
        public int MaximumSolutionCount { get; }

        /// <summary>
        /// Indexed by leg index, defines the max matches to evaluate per leg.
        /// For example, MaximumCountPerLeg[1] is the max matches to evaluate
        /// for the second leg (index=1).
        /// </summary>
        public IReadOnlyList<int> MaximumCountPerLeg { get; }

        /// <summary>
        /// The definitions to be used for matching.
        /// </summary>
        public IEnumerable<OptionStrategyDefinition> Definitions
            => _definitionEnumerator.Enumerate(_definitions);

        /// <summary>
        /// Objective function used to compare different match solutions for a given set of positions/definitions
        /// </summary>
        public IOptionStrategyMatchObjectiveFunction ObjectiveFunction { get; }

        private readonly IReadOnlyList<OptionStrategyDefinition> _definitions;
        private readonly IOptionPositionCollectionEnumerator _positionEnumerator;
        private readonly IOptionStrategyDefinitionEnumerator _definitionEnumerator;

        /// <summary>
        /// Initializes a new instance of the <see cref="OptionStrategyMatcherOptions"/> class, providing
        /// options that control the behavior of the <see cref="OptionStrategyMatcher"/>
        /// </summary>
        public OptionStrategyMatcherOptions(
            IReadOnlyList<OptionStrategyDefinition> definitions,
            IReadOnlyList<int> maximumCountPerLeg,
            TimeSpan maximumDuration = default(TimeSpan),
            int maximumSolutionCount = 100,
            IOptionStrategyDefinitionEnumerator definitionEnumerator = null,
            IOptionStrategyMatchObjectiveFunction objectiveFunction = null,
            IOptionPositionCollectionEnumerator positionEnumerator = null
            )
        {
            if (maximumDuration == default(TimeSpan))
            {
                maximumDuration = Time.OneMinute;
            }

            if (definitionEnumerator == null)
            {
                definitionEnumerator = new IdentityOptionStrategyDefinitionEnumerator();
            }

            if (objectiveFunction == null)
            {
                objectiveFunction = new UnmatchedPositionCountOptionStrategyMatchObjectiveFunction();
            }

            if (positionEnumerator == null)
            {
                positionEnumerator = new DefaultOptionPositionCollectionEnumerator();
            }

            _definitions = definitions;
            MaximumDuration = maximumDuration;
            ObjectiveFunction = objectiveFunction;
            MaximumCountPerLeg = maximumCountPerLeg;
            _positionEnumerator = positionEnumerator;
            _definitionEnumerator = definitionEnumerator;
            MaximumSolutionCount = maximumSolutionCount;
        }

        /// <summary>
        /// Gets the maximum number of leg matches to be evaluated. This is to limit evaluating exponential
        /// numbers of potential matches as a result of large numbers of unique option positions for the same
        /// underlying security.
        /// </summary>
        public int GetMaximumLegMatches(int legIndex)
        {
            return MaximumCountPerLeg[legIndex];
        }

        /// <summary>
        /// Enumerates the specified <paramref name="positions"/> according to the configured
        /// <see cref="IOptionPositionCollectionEnumerator"/>
        /// </summary>
        public IEnumerable<OptionPosition> Enumerate(OptionPositionCollection positions)
        {
            return _positionEnumerator.Enumerate(positions);
        }

        /// <summary>
        /// Creates a new <see cref="OptionStrategyMatcherOptions"/> with the specified <paramref name="definitions"/>,
        /// with no limits of maximum matches per leg and default values for the remaining options
        /// </summary>
        public static OptionStrategyMatcherOptions ForDefinitions(params OptionStrategyDefinition[] definitions)
        {
            return ForDefinitions(definitions.AsEnumerable());
        }

        /// <summary>
        /// Creates a new <see cref="OptionStrategyMatcherOptions"/> with the specified <paramref name="definitions"/>,
        /// with no limits of maximum matches per leg and default values for the remaining options
        /// </summary>
        public static OptionStrategyMatcherOptions ForDefinitions(IEnumerable<OptionStrategyDefinition> definitions)
        {
            var maximumCountPerLeg = new[] {int.MaxValue, int.MaxValue, int.MaxValue, int.MaxValue, int.MaxValue};
            return new OptionStrategyMatcherOptions(definitions.ToList(), maximumCountPerLeg);
        }

        /// <summary>
        /// Specifies the maximum time provided for obtaining an optimal solution.
        /// </summary>
        public OptionStrategyMatcherOptions WithMaximumDuration(TimeSpan duration)
        {
            return new OptionStrategyMatcherOptions(
                _definitions,
                MaximumCountPerLeg,
                duration,
                MaximumSolutionCount,
                _definitionEnumerator,
                ObjectiveFunction,
                _positionEnumerator
            );
        }

        /// <summary>
        /// Specifies the maximum number of solutions to evaluate via the objective function.
        /// </summary>
        public OptionStrategyMatcherOptions WithMaximumSolutionCount(int count)
        {
            return new OptionStrategyMatcherOptions(
                _definitions,
                MaximumCountPerLeg,
                MaximumDuration,
                count,
                _definitionEnumerator,
                ObjectiveFunction,
                _positionEnumerator
            );
        }

        /// <summary>
        /// Specifies the maximum number of solutions per leg index in a solution. Matching is a recursive
        /// process, for example, we'll find a very large number of positions to match the first leg. Matching
        /// the second leg we'll see less, and third still even less. This is because each subsequent leg must
        /// abide by all the previous legs. This parameter defines how many potential matches to evaluate at
        /// each leg. For the first leg, we'll evaluate counts[0] matches. For the second leg we'll evaluate
        /// counts[1] matches and so on. By decreasing this parameter we can evaluate more total, complete
        /// solutions for the entire portfolio rather than evaluation every single permutation of matches for
        /// a particular strategy definition, which grows in absurd exponential fashion as the portfolio grows.
        /// </summary>
        public OptionStrategyMatcherOptions WithMaximumCountPerLeg(IReadOnlyList<int> counts)
        {
            return new OptionStrategyMatcherOptions(
                _definitions,
                counts,
                MaximumDuration,
                MaximumSolutionCount,
                _definitionEnumerator,
                ObjectiveFunction,
                _positionEnumerator
            );
        }

        /// <summary>
        /// Specifies a function used to evaluate how desirable a particular solution is. A good implementation for
        /// this would be to minimize the total margin required to hold all of the positions.
        /// </summary>
        public OptionStrategyMatcherOptions WithObjectiveFunction(IOptionStrategyMatchObjectiveFunction function)
        {
            return new OptionStrategyMatcherOptions(
                _definitions,
                MaximumCountPerLeg,
                MaximumDuration,
                MaximumSolutionCount,
                _definitionEnumerator,
                function,
                _positionEnumerator
            );
        }

        /// <summary>
        /// Specifies the order in which definitions are evaluated. Definitions evaluated sooner are more likely to
        /// find matches than ones evaluated later.
        /// </summary>
        public OptionStrategyMatcherOptions WithDefinitionEnumerator(IOptionStrategyDefinitionEnumerator enumerator)
        {
            return new OptionStrategyMatcherOptions(
                _definitions,
                MaximumCountPerLeg,
                MaximumDuration,
                MaximumSolutionCount,
                enumerator,
                ObjectiveFunction,
                _positionEnumerator
            );
        }

        /// <summary>
        /// Specifies the order in which positions are evaluated. Positions evaluated sooner are more likely to
        /// find matches than ones evaluated later. A good implementation for this is its stand-alone margin required,
        /// which would encourage the algorithm to match higher margin positions before matching lower margin positiosn.
        /// </summary>
        public OptionStrategyMatcherOptions WithPositionEnumerator(IOptionPositionCollectionEnumerator enumerator)
        {
            return new OptionStrategyMatcherOptions(
                _definitions,
                MaximumCountPerLeg,
                MaximumDuration,
                MaximumSolutionCount,
                _definitionEnumerator,
                ObjectiveFunction,
                enumerator
            );
        }
    }
}

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
    /// Matches <see cref="OptionPositionCollection"/> against a collection of <see cref="OptionStrategyDefinition"/>
    /// according to the <see cref="OptionStrategyMatcherOptions"/> provided.
    /// </summary>
    public class OptionStrategyMatcher
    {
        /// <summary>
        /// Specifies options controlling how the matcher operates
        /// </summary>
        public OptionStrategyMatcherOptions Options { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="OptionStrategyMatcher"/> class
        /// </summary>
        /// <param name="options">Specifies definitions and other options controlling the matcher</param>
        public OptionStrategyMatcher(OptionStrategyMatcherOptions options)
        {
            Options = options;
        }

        // TODO : Implement matching multiple permutations and using the objective function to select the best solution

        /// <summary>
        /// Using the definitions provided in <see cref="Options"/>, attempts to match all <paramref name="positions"/>.
        /// The resulting <see cref="OptionStrategyMatch"/> presents a single, valid solution for matching as many positions
        /// as possible.
        /// </summary>
        public OptionStrategyMatch MatchOnce(OptionPositionCollection positions)
        {
            // these definitions are enumerated according to the configured IOptionStrategyDefinitionEnumerator

            var strategies = new List<OptionStrategy>();
            foreach (var definition in Options.Definitions)
            {
                // simplest implementation here is to match one at a time, updating positions in between
                // a better implementation would be to evaluate all possible matches and make decisions
                // prioritizing positions that would require more margin if not matched

                OptionStrategyDefinitionMatch match;
                while (definition.TryMatchOnce(Options, positions, out match))
                {
                    positions = match.RemoveFrom(positions);
                    strategies.Add(match.CreateStrategy());
                }
            }

            return new OptionStrategyMatch(strategies);
        }
    }
}

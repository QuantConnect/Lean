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
    /// Provides a default implementation of <see cref="IOptionStrategyDefinitionEnumerator"/> that enumerates
    /// definitions according to the order that they were provided to <see cref="OptionStrategyMatcherOptions"/>
    /// </summary>
    public class IdentityOptionStrategyDefinitionEnumerator : IOptionStrategyDefinitionEnumerator
    {
        /// <summary>
        /// Enumerates the <paramref name="definitions"/> in the same order as provided.
        /// </summary>
        public IEnumerable<OptionStrategyDefinition> Enumerate(IReadOnlyList<OptionStrategyDefinition> definitions)
        {
            return definitions;
        }
    }
}
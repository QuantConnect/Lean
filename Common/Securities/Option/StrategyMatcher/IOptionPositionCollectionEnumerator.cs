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
    /// Enumerates an <see cref="OptionPositionCollection"/>. The intent is to evaluate positions that
    /// may be more important sooner. Positions appearing earlier in the enumeration are evaluated before
    /// positions showing later. This effectively prioritizes individual positions. This should not be
    /// used filter filtering, but it could also be used to split a position, for example a position with
    /// 10 could be changed to two 5s and they don't need to be enumerated back to-back either. In this
    /// way you could prioritize the first 5 and then delay matching of the final 5.
    /// </summary>
    public interface IOptionPositionCollectionEnumerator
    {
        /// <summary>
        /// Enumerates the provided <paramref name="positions"/>. Positions enumerated first are more
        /// likely to be matched than those appearing later in the enumeration.
        /// </summary>
        IEnumerable<OptionPosition> Enumerate(OptionPositionCollection positions);
    }
}
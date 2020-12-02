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

namespace QuantConnect.Securities.Option.StrategyMatcher
{
    /// <summary>
    /// Evaluates the provided match to assign an objective score. Higher scores are better.
    /// </summary>
    public interface IOptionStrategyMatchObjectiveFunction
    {
        /// <summary>
        /// Evaluates the objective function for the provided match solution. Solution with the highest score will be selected
        /// as the solution. NOTE: This part of the match has not been implemented as of 2020-11-06 as it's only evaluating the
        /// first solution match (MatchOnce).
        /// </summary>
        decimal ComputeScore(OptionPositionCollection input, OptionStrategyMatch match, OptionPositionCollection unmatched);
    }
}
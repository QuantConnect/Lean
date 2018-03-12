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

namespace QuantConnect.Algorithm.Framework.Alphas.Analysis
{
    /// <summary>
    /// Defines a function used to determine how correct a particular insight is.
    /// The result of calling <see cref="Evaluate"/> is expected to be within the range [0, 1]
    /// where 0 is completely wrong and 1 is completely right
    /// </summary>
    public interface IInsightScoreFunction
    {
        /// <summary>
        /// Evaluates the score of the insight within the context
        /// </summary>
        /// <param name="context">The insight's analysis context</param>
        /// <param name="scoreType">The score type to be evaluated</param>
        /// <returns>The insight's current score</returns>
        double Evaluate(InsightAnalysisContext context, InsightScoreType scoreType);
    }
}
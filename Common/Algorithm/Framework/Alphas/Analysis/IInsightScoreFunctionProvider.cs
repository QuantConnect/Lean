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
    /// Retrieves the registered scoring function for the specified insight/score type
    /// </summary>
    public interface IInsightScoreFunctionProvider
    {
        /// <summary>
        /// Gets the insight scoring function for the specified insight type and score type
        /// </summary>
        /// <param name="insightType">The insight's type</param>
        /// <param name="scoreType">The scoring type</param>
        /// <returns>A function to be used to compute insight scores</returns>
        IInsightScoreFunction GetScoreFunction(InsightType insightType, InsightScoreType scoreType);
    }
}
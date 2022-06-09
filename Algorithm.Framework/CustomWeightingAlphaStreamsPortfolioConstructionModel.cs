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

using System.Linq;
using QuantConnect.Logging;
using System.Collections.Generic;
using QuantConnect.Algorithm.Framework.Portfolio;

namespace QuantConnect.Algorithm.Framework
{
    /// <summary>
    /// Custom weighting alpha streams portfolio construction model that will generate aggregated security targets taking into account all the alphas positions
    /// and a custom weighting factor for each alpha, which is also factored by the relation of the alphas portfolio value and the current algorithms portfolio value
    /// </summary>
    public class CustomWeightingAlphaStreamsPortfolioConstructionModel : EqualWeightingAlphaStreamsPortfolioConstructionModel
    {
        private Dictionary<string, decimal> _alphaWeights;

        /// <summary>
        /// Specify a custom set of alpha portfolio weights to use
        /// </summary>
        /// <param name="alphaWeights">The alpha portfolio weights</param>
        public void SetAlphaWeights(Dictionary<string, decimal> alphaWeights)
        {
            Log.Trace($"CustomWeightingAlphaStreamsPortfolioConstructionModel.SetAlphaWeights(): new weights: [{string.Join(",", alphaWeights.Select(pair => $"{pair.Key}:{pair.Value}"))}]");
            _alphaWeights = alphaWeights;
        }

        /// <summary>
        /// Get's the weight for an alpha
        /// </summary>
        /// <param name="alphaId">The algorithm instance that experienced the change in securities</param>
        /// <returns>The alphas weight</returns>
        public override decimal GetAlphaWeight(string alphaId)
        {
            return !_alphaWeights.TryGetValue(alphaId, out var alphaWeight) ? 0 : alphaWeight;
        }
    }
}

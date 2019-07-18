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
using System.Linq;
using QuantConnect.Algorithm.Framework.Alphas;

namespace QuantConnect.Algorithm.Framework.Portfolio
{
    /// <summary>
    /// Provides an implementation of <see cref="IPortfolioConstructionModel"/> that generates percent targets based on the
    /// <see cref="Insight.Weight"/>. The target percent holdings of each Symbol is given by the <see cref="Insight.Weight"/>
    /// from the last active <see cref="Insight"/> for that symbol.
    /// For insights of direction <see cref="InsightDirection.Up"/>, long targets are returned and for insights of direction
    /// <see cref="InsightDirection.Down"/>, short targets are returned.
    /// If the sum of all the last active <see cref="Insight"/> per symbol is bigger than 1, it will factor down each target
    /// percent holdings proportionally so the sum is 1.
    /// It will ignore <see cref="Insight"/> that have no <see cref="Insight.Weight"/> value.
    /// </summary>
    public class InsightWeightingPortfolioConstructionModel : EqualWeightingPortfolioConstructionModel
    {
        /// <summary>
        /// Initialize a new instance of <see cref="InsightWeightingPortfolioConstructionModel"/>
        /// </summary>
        /// <param name="resolution">Rebalancing frequency</param>
        public InsightWeightingPortfolioConstructionModel(Resolution resolution = Resolution.Daily)
            : base(resolution)
        {
        }

        /// <summary>
        /// Method that will determine if the portfolio construction model should create a
        /// target for this insight
        /// </summary>
        /// <param name="insight">The insight to create a target for</param>
        /// <returns>True if the portfolio should create a target for the insight</returns>
        public override bool ShouldCreateTargetForInsight(Insight insight)
        {
            return insight.Weight.HasValue;
        }

        /// <summary>
        /// Will determine the target percent for each insight
        /// </summary>
        /// <param name="activeInsights">The active insights to generate a target for</param>
        /// <returns>A target percent for each insight</returns>
        public override Dictionary<Insight, double> DetermineTargetPercent(ICollection<Insight> activeInsights)
        {
            var result = new Dictionary<Insight, double>();
            // We will adjust weights proportionally in case the sum is > 1 so it sums to 1.
            var weightSums = activeInsights.Sum(insight => insight.Weight.Value);
            var weightFactor = 1.0;
            if (weightSums > 1)
            {
                weightFactor = 1 / weightSums;
            }
            foreach (var insight in activeInsights)
            {
                result[insight] = (int)insight.Direction * insight.Weight.Value * weightFactor;
            }
            return result;
        }
    }
}

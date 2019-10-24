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
using QuantConnect.Algorithm.Framework.Alphas;

namespace QuantConnect.Algorithm.Framework.Portfolio
{
    /// <summary>
    /// Provides an implementation of <see cref="IPortfolioConstructionModel"/> that generates percent targets based on the
    /// <see cref="Insight.Confidence"/>. The target percent holdings of each Symbol is given by the <see cref="Insight.Confidence"/>
    /// from the last active <see cref="Insight"/> for that symbol.
    /// For insights of direction <see cref="InsightDirection.Up"/>, long targets are returned and for insights of direction
    /// <see cref="InsightDirection.Down"/>, short targets are returned.
    /// If the sum of all the last active <see cref="Insight"/> per symbol is bigger than 1, it will factor down each target
    /// percent holdings proportionally so the sum is 1.
    /// It will ignore <see cref="Insight"/> that have no <see cref="Insight.Confidence"/> value.
    /// </summary>
    public class ConfidenceWeightedPortfolioConstructionModel : InsightWeightingPortfolioConstructionModel
    {
        /// <summary>
        /// Initialize a new instance of <see cref="InsightWeightingPortfolioConstructionModel"/>
        /// </summary>
        /// <param name="rebalancingFunc">For a given algorithm UTC DateTime returns the next expected rebalance time</param>
        public ConfidenceWeightedPortfolioConstructionModel(Func<DateTime, DateTime> rebalancingFunc)
            : base(rebalancingFunc)
        {
        }

        /// <summary>
        /// Initialize a new instance of <see cref="InsightWeightingPortfolioConstructionModel"/>
        /// </summary>
        /// <param name="timeSpan">Rebalancing frequency</param>
        public ConfidenceWeightedPortfolioConstructionModel(TimeSpan timeSpan)
            : base(timeSpan)
        {
        }

        /// <summary>
        /// Initialize a new instance of <see cref="ConfidenceWeightedPortfolioConstructionModel"/>
        /// </summary>
        /// <param name="resolution">Rebalancing frequency</param>
        public ConfidenceWeightedPortfolioConstructionModel(Resolution resolution = Resolution.Daily)
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
            return insight.Confidence.HasValue;
        }

        /// <summary>
        /// Method that will determine which member will be used to compute the weights and gets its value
        /// </summary>
        /// <param name="insight">The insight to create a target for</param>
        /// <returns>The value of the selected insight member</returns>
        protected override double GetValue(Insight insight) => insight.Confidence ?? 0;
    }
}

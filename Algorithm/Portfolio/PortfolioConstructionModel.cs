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
using QuantConnect.Algorithm.Framework.Alphas;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.Framework.Portfolio
{
    /// <summary>
    /// Provides a base class for portfolio construction models
    /// </summary>
    public class PortfolioConstructionModel : IPortfolioConstructionModel
    {
        /// <summary>
        /// Create portfolio targets from the specified insights
        /// </summary>
        /// <param name="algorithm">The algorithm instance</param>
        /// <param name="insights">The insights to create portfolio targets from</param>
        /// <returns>An enumerable of portfolio targets to be sent to the execution model</returns>
        public virtual IEnumerable<IPortfolioTarget> CreateTargets(QCAlgorithm algorithm, Insight[] insights)
        {
            throw new System.NotImplementedException("Types deriving from 'PortfolioConstructionModel' must implement the 'IEnumerable<IPortfolioTarget> CreateTargets(QCAlgorithm, Insight[]) method.");
        }

        /// <summary>
        /// Event fired each time the we add/remove securities from the data feed
        /// </summary>
        /// <param name="algorithm">The algorithm instance that experienced the change in securities</param>
        /// <param name="changes">The security additions and removals from the algorithm</param>
        public virtual void OnSecuritiesChanged(QCAlgorithm algorithm, SecurityChanges changes)
        {
        }

        /// <summary>
        /// Helper class that can be used by the different <see cref="IPortfolioConstructionModel"/>
        /// implementations to filter <see cref="Insight"/> instances with an invalid
        /// <see cref="Insight.Magnitude"/> value based on the <see cref="IAlgorithmSettings"/>
        /// </summary>
        /// <param name="algorithm">The algorithm instance</param>
        /// <param name="insights">The insight collection to filter</param>
        /// <returns>Returns a new array of insights removing invalid ones</returns>
        public static Insight[] FilterInvalidInsightMagnitude(QCAlgorithm algorithm, Insight[] insights)
        {
            var result = insights.Where(insight =>
            {
                if (!insight.Magnitude.HasValue || insight.Magnitude == 0)
                {
                    return true;
                }

                var absoluteMagnitude = Math.Abs(insight.Magnitude.Value);
                if (absoluteMagnitude > (double)algorithm.Settings.MaxAbsolutePortfolioTargetPercentage
                    || absoluteMagnitude < (double)algorithm.Settings.MinAbsolutePortfolioTargetPercentage)
                {
                    algorithm.Error("PortfolioConstructionModel.FilterInvalidInsightMagnitude():" +
                        $"The insight target Magnitude: {insight.Magnitude}, will not comply with the current " +
                        $"'Algorithm.Settings' 'MaxAbsolutePortfolioTargetPercentage': {algorithm.Settings.MaxAbsolutePortfolioTargetPercentage}" +
                        $" or 'MinAbsolutePortfolioTargetPercentage': {algorithm.Settings.MinAbsolutePortfolioTargetPercentage}. Skipping insight."
                    );
                    return false;
                }

                return true;
            });
            return result.ToArray();
        }
    }
}

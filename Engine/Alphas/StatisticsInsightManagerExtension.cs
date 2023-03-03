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
 *
*/

using System;
using QuantConnect.Interfaces;
using QuantConnect.Algorithm.Framework.Alphas;
using QuantConnect.Algorithm.Framework.Alphas.Analysis;

namespace QuantConnect.Lean.Engine.Alphas
{
    /// <summary>
    /// Manages alpha statistics responsbilities
    /// </summary>
    public class StatisticsInsightManagerExtension : IInsightManagerExtension
    {
        /// <summary>
        /// Gets the current statistics. The values are current as of the time specified
        /// in <see cref="AlphaRuntimeStatistics.MeanPopulationScore"/> and <see cref="AlphaRuntimeStatistics.RollingAveragedPopulationScore"/>
        /// </summary>
        public AlphaRuntimeStatistics Statistics { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="StatisticsInsightManagerExtension"/> class
        /// </summary>
        public StatisticsInsightManagerExtension()
        {
            Statistics = new AlphaRuntimeStatistics();
        }

        /// <summary>
        /// Handles the <see cref="IAlgorithm.InsightsGenerated"/> event
        /// Increments total, long and short counters. Updates long/short ratio
        /// </summary>
        /// <param name="context">The newly generated insight context</param>
        public void OnInsightGenerated(InsightAnalysisContext context)
        {
            // incremement total insight counter
            Statistics.TotalInsightsGenerated++;

            // update long/short ratio statistics
            if (context.Insight.Direction == InsightDirection.Up)
            {
                Statistics.LongCount++;
            }
            else if (context.Insight.Direction == InsightDirection.Down)
            {
                Statistics.ShortCount++;
            }
        }

        /// <summary>
        /// Computes an estimated value for the insight. This is intended to be invoked at the end of the
        /// insight period, i.e, when now == insight.GeneratedTimeUtc + insight.Period;
        /// </summary>
        /// <param name="context">Context whose insight has just closed</param>
        public void OnInsightClosed(InsightAnalysisContext context)
        {
            // increment closed insight counter
            Statistics.TotalInsightsClosed += 1;
        }

        /// <summary>
        /// Updates the specified statistics with the new scores
        /// </summary>
        /// <param name="context">Context whose insight has just completed analysis</param>
        public void OnInsightAnalysisCompleted(InsightAnalysisContext context)
        {
            // increment analysis completed counter
            Statistics.TotalInsightsAnalysisCompleted += 1;
        }

        /// <summary>
        /// Invokes the manager at the end of the time step.
        /// </summary>
        /// <param name="frontierTimeUtc">The current frontier time utc</param>
        public void Step(DateTime frontierTimeUtc)
        {
        }

        /// <summary>
        /// Allows the extension to initialize itself over the expected range
        /// </summary>
        /// <param name="algorithmStartDate">The start date of the algorithm</param>
        /// <param name="algorithmEndDate">The end date of the algorithm</param>
        /// <param name="algorithmUtcTime">The algorithm's current utc time</param>
        public void InitializeForRange(DateTime algorithmStartDate, DateTime algorithmEndDate, DateTime algorithmUtcTime)
        {
        }
    }
}

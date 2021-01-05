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
using QuantConnect.Algorithm.Framework.Alphas.Analysis;

namespace QuantConnect.Algorithm.Framework.Alphas
{
    /// <summary>
    /// Abstraction point to handle the various concerns from a common api.
    /// At the time of writing, these concerns are charting, scoring, perisistence and messaging.
    /// </summary>
    public interface IInsightManagerExtension
    {
        /// <summary>
        /// Invokes the manager at the end of the time step.
        /// </summary>
        /// <param name="frontierTimeUtc">The current frontier time utc</param>
        void Step(DateTime frontierTimeUtc);

        /// <summary>
        /// Allows the extension to initialize itself over the expected range
        /// </summary>
        /// <param name="algorithmStartDate">The start date of the algorithm</param>
        /// <param name="algorithmEndDate">The end date of the algorithm</param>
        /// <param name="algorithmUtcTime">The algorithm's current utc time</param>
        void InitializeForRange(DateTime algorithmStartDate, DateTime algorithmEndDate, DateTime algorithmUtcTime);

        /// <summary>
        /// Invoked when the insight manager first received a generated insight from the algorithm
        /// </summary>
        /// <param name="context">Context whose insight has just generated</param>
        void OnInsightGenerated(InsightAnalysisContext context);

        /// <summary>
        /// Invoked when the insight manager detects that an insight has closed (frontier has passed insight period)
        /// </summary>
        /// <param name="context">Context whose insight has just closed</param>
        void OnInsightClosed(InsightAnalysisContext context);

        /// <summary>
        /// Invoked when the insight manager has completed analysis on an insight
        /// </summary>
        /// <param name="context">Context whose insight has just completed analysis</param>
        void OnInsightAnalysisCompleted(InsightAnalysisContext context);
    }
}
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
    /// Updates alpha runtime statistics at various points in the alpha's lifecycle
    /// 1. OnAlphaReceived          -> handles new alpha
    /// 2. OnAlphaPeriodClosed      -> handles alpha at GeneratedTimeUtc + Period
    /// 3. OnAlphaAnalysisCompleted -> handles alpha after scoring is finlized
    /// </summary>
    public interface IAlphaRuntimeStatisticsUpdater
    {
        /// <summary>
        /// Updates statistics when a new alpha signal is received by the alpha manager
        /// </summary>
        /// <param name="statistics">Statistics to be updated</param>
        /// <param name="context">Context whose alpha was just generated</param>
        void OnAlphaReceived(AlphaRuntimeStatistics statistics, AlphaAnalysisContext context);

        /// <summary>
        /// Updates statistics when the alpha's period as closed.
        /// This is when we can assign an estimated value to the alpha
        /// </summary>
        /// <param name="statistics">Statistics to be updated</param>
        /// <param name="context">Context whose alpha has just closed</param>
        void OnAlphaClosed(AlphaRuntimeStatistics statistics, AlphaAnalysisContext context);

        /// <summary>
        /// Updates statistics when the alphas's analysis period is closed.
        /// This is when alpha scoring has been finalizd.
        /// </summary>
        /// <param name="statistics">Statistics to be updated</param>
        /// <param name="context">Context whose alpha has just completed analysis</param>
        void OnAlphaAnalysisCompleted(AlphaRuntimeStatistics statistics, AlphaAnalysisContext context);
    }
}
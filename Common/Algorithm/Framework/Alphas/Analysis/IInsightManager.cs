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

namespace QuantConnect.Algorithm.Framework.Alphas.Analysis
{
    /// <summary>
    /// Encapsulates the storage and on-line scoring of insights.
    /// </summary>b
    public interface IInsightManager
    {
        /// <summary>
        /// Gets the current number of insights that have been processed
        /// </summary>
        int InsightCount { get; }

        /// <summary>
        /// Returns all non expired insights
        /// </summary>
        List<Insight> GetOpenInsights(Func<Insight, bool> filter = null);

        /// <summary>
        /// Returns all expired insights
        /// </summary>
        List<Insight> GetInsights(Func<Insight, bool> filter = null);

        /// <summary>
        /// Removes insights from the manager
        /// </summary>
        /// <param name="insightsToRemove">The insights to be removed</param>
        void RemoveInsights(IEnumerable<Insight> insightsToRemove);
    }
}

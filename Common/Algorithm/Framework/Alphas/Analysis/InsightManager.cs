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
using System.Linq;
using System.Threading;
using QuantConnect.Util;
using System.Collections.Generic;

namespace QuantConnect.Algorithm.Framework.Alphas.Analysis
{
    /// <summary>
    /// Encapsulates the storage of insights.
    /// </summary>
    public class InsightManager : IInsightManager
    {
        private int _insightCount;
        private readonly object _lock = new ();

        /// <summary>
        /// This dictionary holds all insights.
        /// </summary>
        private readonly Dictionary<Guid, Insight> _completeInsights = new();

        /// <summary>
        /// This dictionary holds all open insights.
        /// </summary>
        private readonly Dictionary<Guid, Insight> _openInsights = new();

        /// <summary>
        /// Gets the current number of insights that have been processed
        /// </summary>
        public int InsightCount => _insightCount;

        /// <summary>
        /// Returns all non expired insights
        /// </summary>
        public List<Insight> GetOpenInsights(Func<Insight, bool> filter = null)
        {
            lock (_lock)
            {
                var result = _openInsights.Select(kvp => kvp.Value);
                if (filter != null)
                {
                    result = result.Where(filter);
                }
                return result.ToList();
            }
        }

        /// <summary>
        /// Returns all expired insights
        /// </summary>
        public virtual List<Insight> GetInsights(Func<Insight, bool> filter = null)
        {
            lock (_lock)
            {
                var result = _completeInsights.Select(kvp => kvp.Value);
                if (filter != null)
                {
                    result = result.Where(filter);
                }
                return result.ToList();
            }
        }

        /// <summary>
        /// Adds a collection of insights
        /// </summary>
        /// <param name="newInsights">The insight instances to add</param>
        public void AddInsights(IEnumerable<Insight> newInsights)
        {
            lock (_lock)
            {
                foreach (var insight in newInsights)
                {
                    Interlocked.Increment(ref _insightCount);

                    // we suppose they are not expired
                    _openInsights[insight.Id] = insight;
                    _completeInsights[insight.Id] = insight;
                }
            }
        }

        /// <summary>
        /// Process a new time step handling expired insights
        /// </summary>
        /// <param name="utcNow">The current utc time</param>
        public void Step(DateTime utcNow)
        {
            List<Insight> _toRemove = null;
            lock (_lock)
            {
                foreach (var insight in _openInsights.Values)
                {
                    if (insight.IsExpired(utcNow))
                    {
                        insight.Score.Finalize(utcNow);

                        _toRemove ??= new();
                        // keep track
                        _toRemove.Add(insight);
                    }
                }

                if(_toRemove != null )
                {
                    // remove from open
                    foreach (var insight in _toRemove)
                    {
                        _openInsights.Remove(insight.Id);
                    }
                }
            }
        }

        /// <summary>
        /// Removes insights from the manager
        /// </summary>
        /// <param name="insightsToRemove">The insights to be removed</param>
        public void RemoveInsights(IEnumerable<Insight> insightsToRemove)
        {
            lock (_lock)
            {
                foreach (var insight in insightsToRemove)
                {
                    _completeInsights.Remove(insight.Id);
                }
            }
        }
    }
}

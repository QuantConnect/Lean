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

using System.Collections.Generic;

namespace QuantConnect.Optimizer
{
    /// <summary>
    /// One k-means cluster of trials in standardized parameter space. Centroid coordinates
    /// are reported in the original parameter units so they are directly comparable to
    /// trial parameter values. Clusters are ordered by <see cref="SharpeMean"/> descending.
    /// </summary>
    public class Cluster
    {
        /// <summary>
        /// Cluster index (0 = highest mean Sharpe).
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// Cluster centroid in original parameter units (parameter name -> value).
        /// </summary>
        public IReadOnlyDictionary<string, double> Centroid { get; set; }

        /// <summary>
        /// Number of trials assigned to this cluster.
        /// </summary>
        public int MemberCount { get; set; }

        /// <summary>
        /// Mean Sharpe ratio across the cluster's members.
        /// </summary>
        public double SharpeMean { get; set; }

        /// <summary>
        /// Sample standard deviation of Sharpe ratios within this cluster.
        /// </summary>
        public double SharpeStdDev { get; set; }

        /// <summary>
        /// Minimum Sharpe ratio observed within this cluster.
        /// </summary>
        public double SharpeMin { get; set; }

        /// <summary>
        /// Maximum Sharpe ratio observed within this cluster.
        /// </summary>
        public double SharpeMax { get; set; }
    }
}

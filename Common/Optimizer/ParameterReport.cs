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

using Newtonsoft.Json;
using System.Collections.Generic;

namespace QuantConnect.Optimizer
{
    /// <summary>
    /// Sensitivity report for a single optimized parameter, computed from one-dimensional
    /// cross-sections of the parameter space (slices) where every other parameter is held
    /// constant.
    /// </summary>
    public class ParameterReport
    {
        /// <summary>
        /// Parameter name as defined in the optimization configuration.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Lower bound of the parameter sweep, taken from the optimization configuration.
        /// </summary>
        public double SearchedMin { get; set; }

        /// <summary>
        /// Upper bound of the parameter sweep, taken from the optimization configuration.
        /// </summary>
        public double SearchedMax { get; set; }

        /// <summary>
        /// Sweep step size from the optimization configuration, or null if none was provided.
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public double? Step { get; set; }

        /// <summary>
        /// Number of distinct values this parameter took across all completed trials.
        /// </summary>
        public int DistinctValueCount { get; set; }

        /// <summary>
        /// Average of (max Sharpe - min Sharpe) across every 1-D slice of this parameter.
        /// </summary>
        public double MeanWithinSliceSharpeRange { get; set; }

        /// <summary>
        /// Maximum of (max Sharpe - min Sharpe) across every 1-D slice of this parameter.
        /// </summary>
        public double MaxWithinSliceSharpeRange { get; set; }

        /// <summary>
        /// Worst-case Sharpe change between two adjacent grid values of this parameter,
        /// taken across all slices and scaled by <see cref="Step"/>.
        /// </summary>
        public double MaxAbsDerivativePerStep { get; set; }

        /// <summary>
        /// This parameter's value at the best trial.
        /// </summary>
        public double BestValue { get; set; }

        /// <summary>
        /// True when <see cref="BestValue"/> lies within half a step of the searched min or max,
        /// signalling that the optimum may lie outside the swept range.
        /// </summary>
        public bool BestAtSearchedEdge { get; set; }

        /// <summary>
        /// One-dimensional slices of the parameter space used for the sensitivity analysis.
        /// Each slice fixes every other parameter and varies only this one.
        /// </summary>
        public IReadOnlyList<SliceFit> Slices { get; set; }
    }
}

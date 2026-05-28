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
    /// Sensitivity report for a single optimized parameter.
    /// </summary>
    public class ParameterReport
    {
        /// <summary>
        /// Parameter name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Lower bound of the parameter sweep.
        /// </summary>
        public decimal SearchedMin { get; set; }

        /// <summary>
        /// Upper bound of the parameter sweep.
        /// </summary>
        public decimal SearchedMax { get; set; }

        /// <summary>
        /// Sweep step size; null when not provided in the optimization configuration.
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public decimal? Step { get; set; }

        /// <summary>
        /// Mean Sharpe range (max - min) across every 1-D slice.
        /// </summary>
        public decimal MeanWithinSliceSharpeRange { get; set; }

        /// <summary>
        /// Maximum Sharpe range (max - min) across every 1-D slice.
        /// </summary>
        public decimal MaxWithinSliceSharpeRange { get; set; }

        /// <summary>
        /// Worst-case Sharpe change between two adjacent grid values, scaled by <see cref="Step"/>.
        /// </summary>
        public decimal MaxAbsDerivativePerStep { get; set; }

        /// <summary>
        /// This parameter's value at the best backtest.
        /// </summary>
        public decimal BestValue { get; set; }

        /// <summary>
        /// True when <see cref="BestValue"/> lies within half a step of <see cref="SearchedMin"/> or <see cref="SearchedMax"/>.
        /// </summary>
        public bool BestAtSearchedEdge { get; set; }

        /// <summary>
        /// One-dimensional slices used for the sensitivity analysis.
        /// </summary>
        public IReadOnlyList<SliceFit> Slices { get; set; }
    }
}

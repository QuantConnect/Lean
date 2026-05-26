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
    /// One-dimensional cross-section of the parameter space: a single parameter varies while
    /// every other parameter is held constant at the values in <see cref="FixedParameters"/>.
    /// The piecewise linear interpolant (<see cref="Segments"/>) passes through every measured
    /// (ParameterValue, Sharpe) point exactly.
    /// </summary>
    public class SliceFit
    {
        /// <summary>
        /// Values of the other parameters that are held constant for this slice (name -> value).
        /// Empty for single-parameter optimizations.
        /// </summary>
        public IReadOnlyDictionary<string, double> FixedParameters { get; set; }

        /// <summary>
        /// Measured grid values of the slicing parameter, sorted ascending.
        /// </summary>
        public IReadOnlyList<double> ParameterValues { get; set; }

        /// <summary>
        /// Sharpe ratio at each entry in <see cref="ParameterValues"/> (same index).
        /// </summary>
        public IReadOnlyList<double> SharpeValues { get; set; }

        /// <summary>
        /// max(SharpeValues) - min(SharpeValues) across this slice. Zero for single-point slices.
        /// </summary>
        public double SharpeRange { get; set; }

        /// <summary>
        /// Maximum |slope| across this slice's linear segments. Equals
        /// max(|y[i+1] - y[i]| / (x[i+1] - x[i])).
        /// </summary>
        public double MaxAbsDerivative { get; set; }

        /// <summary>
        /// True for exactly one slice per parameter: the slice whose fixed parameters match
        /// the values at the best trial.
        /// </summary>
        public bool IsPrimary { get; set; }

        /// <summary>
        /// Piecewise linear pieces of the fit. Length = len(ParameterValues) - 1
        /// (empty when there is only one point).
        /// </summary>
        public IReadOnlyList<LinearSegment> Segments { get; set; }
    }
}

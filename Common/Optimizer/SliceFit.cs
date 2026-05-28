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
    /// One-dimensional cross-section of the parameter space: one parameter varies while every other is held constant.
    /// </summary>
    public class SliceFit
    {
        /// <summary>
        /// Values of the other parameters held constant for this slice.
        /// </summary>
        public IReadOnlyDictionary<string, decimal> FixedParameters { get; set; }

        /// <summary>
        /// Max Sharpe minus min Sharpe across this slice.
        /// </summary>
        public decimal SharpeRange { get; set; }

        /// <summary>
        /// Maximum absolute slope across this slice's linear segments.
        /// </summary>
        public decimal MaxAbsDerivative { get; set; }

        /// <summary>
        /// Piecewise linear pieces of the fit; one per adjacent pair of grid points.
        /// </summary>
        public IReadOnlyList<LinearSegment> Segments { get; set; }
    }
}

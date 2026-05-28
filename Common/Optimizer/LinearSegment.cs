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

namespace QuantConnect.Optimizer
{
    /// <summary>
    /// One linear piece of a piecewise interpolant on [<see cref="XLo"/>, <see cref="XHi"/>], evaluated as y(x) = A + B * (x - XLo).
    /// </summary>
    public class LinearSegment
    {
        /// <summary>
        /// Lower bound of this segment.
        /// </summary>
        public decimal XLo { get; set; }

        /// <summary>
        /// Upper bound of this segment.
        /// </summary>
        public decimal XHi { get; set; }

        /// <summary>
        /// Sharpe ratio at <see cref="XLo"/>.
        /// </summary>
        public decimal A { get; set; }

        /// <summary>
        /// Slope through the segment.
        /// </summary>
        public decimal B { get; set; }
    }
}

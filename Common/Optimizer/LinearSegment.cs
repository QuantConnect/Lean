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
    /// One straight-line piece of a piecewise linear interpolant covering [XLo, XHi].
    /// Evaluates as y(x) = A + B * (x - XLo). Passes exactly through (XLo, Sharpe at XLo)
    /// and (XHi, Sharpe at XHi).
    /// </summary>
    public class LinearSegment
    {
        /// <summary>
        /// Lower bound of this segment (inclusive).
        /// </summary>
        public double XLo { get; set; }

        /// <summary>
        /// Upper bound of this segment.
        /// </summary>
        public double XHi { get; set; }

        /// <summary>
        /// Sharpe ratio at XLo (the segment's left endpoint).
        /// </summary>
        public double A { get; set; }

        /// <summary>
        /// Constant slope through the segment: (Sharpe at XHi - Sharpe at XLo) / (XHi - XLo).
        /// </summary>
        public double B { get; set; }
    }
}

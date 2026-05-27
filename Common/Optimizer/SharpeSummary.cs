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
    /// Sharpe ratio statistics across all used backtests in an optimization.
    /// </summary>
    public class SharpeSummary
    {
        /// <summary>
        /// Arithmetic mean of Sharpe ratios.
        /// </summary>
        public decimal Mean { get; set; }

        /// <summary>
        /// Sample standard deviation of Sharpe ratios.
        /// </summary>
        public decimal StdDev { get; set; }

        /// <summary>
        /// Minimum Sharpe ratio observed.
        /// </summary>
        public decimal Min { get; set; }

        /// <summary>
        /// Maximum Sharpe ratio observed.
        /// </summary>
        public decimal Max { get; set; }

        /// <summary>
        /// Median Sharpe ratio.
        /// </summary>
        public decimal Median { get; set; }
    }
}

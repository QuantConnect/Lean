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
    /// Univariate statistics of the Sharpe ratio across all used trials in an optimization.
    /// </summary>
    public class SharpeSummary
    {
        /// <summary>
        /// Arithmetic mean of Sharpe ratios across all used trials.
        /// </summary>
        public double Mean { get; set; }

        /// <summary>
        /// Sample standard deviation of Sharpe ratios across all used trials.
        /// </summary>
        public double StdDev { get; set; }

        /// <summary>
        /// Minimum Sharpe ratio observed.
        /// </summary>
        public double Min { get; set; }

        /// <summary>
        /// Maximum Sharpe ratio observed.
        /// </summary>
        public double Max { get; set; }

        /// <summary>
        /// Median Sharpe ratio across all used trials.
        /// </summary>
        public double Median { get; set; }
    }
}

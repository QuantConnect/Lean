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

namespace QuantConnect.Statistics
{
    /// <summary>
    /// Represents the result of a drawdown analysis, including the maximum drawdown percentage
    /// and the maximum recovery time in days.
    /// </summary>
    public class DrawdownMetrics
    {
        /// <summary>
        /// Gets the maximum drawdown as a positive percentage.
        /// </summary>
        public decimal Drawdown { get; }

        /// <summary>
        /// Gets the maximum recovery time in days from peak to full recovery.
        /// </summary>
        public int DrawdownRecovery { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DrawdownMetrics"/> class
        /// with the specified maximum drawdown and recovery time.
        /// </summary>
        /// <param name="drawdown">The maximum drawdown as a positive percentage.</param>
        /// <param name="recoveryTime">The maximum number of days it took to recover from a drawdown.</param>
        public DrawdownMetrics(decimal drawdown, int recoveryTime)
        {
            Drawdown = drawdown;
            DrawdownRecovery = recoveryTime;
        }
    }
}
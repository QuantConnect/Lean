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
*/

using System;

namespace QuantConnect.Report
{
    /// <summary>
    /// Represents a period of time where the drawdown ranks amongst the top N drawdowns.
    /// </summary>
    public class DrawdownPeriod
    {
        /// <summary>
        /// Start of the drawdown period
        /// </summary>
        public DateTime Start { get; private set; }

        /// <summary>
        /// End of the drawdown period
        /// </summary>
        public DateTime End { get; private set; }

        /// <summary>
        /// Loss in percent from peak to trough
        /// </summary>
        public double PeakToTrough { get; private set; }

        /// <summary>
        /// Loss in percent from peak to trough - Alias for <see cref="PeakToTrough"/>
        /// </summary>
        public double Drawdown => PeakToTrough;

        /// <summary>
        /// Creates an instance with the given start, end, and drawdown
        /// </summary>
        /// <param name="start">Start of the drawdown period</param>
        /// <param name="end">End of the drawdown period</param>
        /// <param name="drawdown">Max drawdown of the period</param>
        public DrawdownPeriod(DateTime start, DateTime end, double drawdown)
        {
            Start = start;
            End = end;
            PeakToTrough = drawdown;
        }
    }
}

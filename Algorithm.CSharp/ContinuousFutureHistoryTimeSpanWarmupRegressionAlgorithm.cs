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

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Continuous Futures History Regression algorithm. Asserting and showcasing the behavior of adding a continuous future
    /// </summary>
    public class ContinuousFutureHistoryTimeSpanWarmupRegressionAlgorithm : ContinuousFutureHistoryRegressionAlgorithm
    {
        public override void Initialize()
        {
            base.Initialize();
            // We want to match the start time of the base algorithm. ES futures data time zone is UTC, algorithm time zone is new york (default).
            // Base algorithm warmup is 1 bar of daily resolution starts at 8 PM new york time of T-1. So to match the same start time
            // we go back a 1 day + 4 hours. This is calculated by 'Time.GetStartTimeForTradeBars'
            SetWarmup(TimeSpan.FromHours(24 + 4));
        }

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public override long DataPoints => 9079;
    }
}

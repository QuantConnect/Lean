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

namespace QuantConnect.Algorithm.CSharp
{
    public class WarmupDataTypesBarCountWarmupRegressionAlgorithm : WarmupDataTypesRegressionAlgorithm
    {
        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            base.Initialize();

            // We want to match the start time of the base algorithm: Base algorithm warmup is 24 bars of hour resolution.
            // So to match the same start time we go back 5 days + a few hours, we need to account for weekends. This is calculated by 'Time.GetStartTimeForTradeBars'
            // Each day has 7 hour bars => 3 complete days 21 hours + 2 weekend days + 3 hours of the previous day (24 PM - 11 hours = 13 PM - 13/14/15 hour bars)
            SetWarmUp(24 * 5 + 11);
        }

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public override long DataPoints => 5299;
    }
}

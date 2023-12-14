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

using System;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm asserting the behavior of option warmup
    /// </summary>
    public class WarmupOptionResolutionRegressionAlgorithm : WarmupOptionRegressionAlgorithm
    {
        public override void Initialize()
        {
            base.Initialize();

            SetWarmUp(1, Resolution.Daily);
        }

        public override void OnEndOfAlgorithm()
        {
            var start = new DateTime(2015, 12, 24, 0, 0, 0);
            var end = new DateTime(2015, 12, 24, 0, 0, 0);
            var count = 0;
            do
            {
                if (OptionWarmupTimes[count] != start)
                {
                    throw new Exception($"Unexpected time {OptionWarmupTimes[count]} expected {start}");
                }
                count++;
                start = start.AddDays(1);
            }
            while (start < end);
        }

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public override long DataPoints => 542803;
    }
}

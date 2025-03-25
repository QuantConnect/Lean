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
    /// Tests indicator behavior when "DailyPreciseEndTime" is disabled,  
    /// ensuring updates occur at midnight and values remain consistent.  
    /// </summary>
    public class DailyResolutionVsTimeSpanNoPreciseEndRegressionAlgorithm : DailyResolutionVsTimeSpanRegressionAlgorithm
    {
        // Disables precise end time, considering the full day instead.
        protected override bool DailyPreciseEndTime => false;

        protected override void SetupFirstIndicatorUpdatedHandler()
        {
            RelativeStrengthIndex1.Updated += (sender, data) =>
            {
                var updatedTime = Time;

                // RSI1 should update at midnight when precise end time is disabled
                if (updatedTime.TimeOfDay != new TimeSpan(0, 0, 0))
                {
                    throw new RegressionTestException($"{RelativeStrengthIndex1.Name} must have updated at midnight, but it was updated at {updatedTime}");
                }

                // Since RSI1 updates before RSI2, it should have exactly one extra sample
                if (RelativeStrengthIndex1.Samples - 1 != RelativeStrengthIndex2.Samples)
                {
                    throw new RegressionTestException("RSI1 must have 1 extra sample");
                }

                // RSI1's previous value should match RSI2's current value, ensuring consistency
                if (RelativeStrengthIndex1.Previous.Value != RelativeStrengthIndex2.Current.Value)
                {
                    throw new RegressionTestException("RSI1 and RSI2 must have same value");
                }
            };
        }

        public override void OnEndOfAlgorithm()
        {
            if (RelativeStrengthIndex1.Current.Value != RelativeStrengthIndex2.Current.Value)
            {
                throw new RegressionTestException("RSI1 and RSI2 must have same value");
            }
            if (RelativeStrengthIndex1.Samples <= 20 || RelativeStrengthIndex2.Samples != RelativeStrengthIndex1.Samples)
            {
                throw new RegressionTestException("The number of samples must be the same and greater than 20");
            }
        }
    }
}

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

namespace QuantConnect.Indicators
{
    /// <summary>
    /// This indicator computes the n-period Ease of Movement Value using the following:
    /// MID = (high_1 + low_1)/2 - (high_0 + low_0)/2 
    /// RATIO = (currentVolume/10000) / (high_1 - low_1)
    /// EMV = MID/ratio
    /// </summary>
    public class EaseOfMovement : WindowIndicator<IndicatorDataPoint>, IIndicatorWarmUpPeriodProvider
    {
        /// <summary>
        /// Required period for indicator to be fully initialized
        /// </summary>
        public int WarmUpPeriod => Period;

        /// <summary>
        /// Initializeds a new instance of the <see cref="EaseOfMovement"/> class using the specufued period
        /// </summary>
        /// <param name="period">The period over which to perform to computation</param>
        public EaseOfMovement(int period)
            : base($"EMV({period})", period)
        {
        }

        /// <summary>
        /// Creates a new EaseOfMovement indicator with the specified period
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="period">The period over which to perform to computation</param>
        public EaseOfMovement(string name, int period)
            : base(name, period)
        {
        }


        /// <summary>
        /// Computes the next value for this indicator from the given state.
        /// </summary>
        /// <param name="window">The window of data held in this indicator</param>
        /// <param name="input">The input value to this indicator on this time step</param>
        /// <returns>A a value for this indicator</returns>
        protected override decimal ComputeNextValue(IReadOnlyWindow<IndicatorDataPoint> window, IndicatorDataPoint input)
        {
            
            var previous = window.Samples <= window.Size ? window[window.Count - 1] : window.MostRecentlyRemoved;

            /// if we're not ready just grab the first input point in the window
            if (previous == 0)
            {
                return 0;
            }

            var mid = ((input.High + input.Low)/2) - ((previous.High + previous.Low) / 2);
            var ratio = (input.Volume/10000) / (input.High - input.Low);
            var emvValue = mid / ratio;

            return emvValue;
        }
    }
}
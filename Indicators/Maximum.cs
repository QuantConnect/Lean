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

using System.Linq;

namespace QuantConnect.Indicators
{
    /// <summary>
    /// Represents an indicator capable of tracking the maximum value and how many periods ago it occurred
    /// </summary>
    public class Maximum : WindowIndicator<IndicatorDataPoint>, IIndicatorWarmUpPeriodProvider
    {
        /// <summary>
        /// The number of periods since the maximum value was encountered
        /// </summary>
        public int PeriodsSinceMaximum { get; private set; }

        /// <summary>
        /// Gets a flag indicating when this indicator is ready and fully initialized
        /// </summary>
        public override bool IsReady => Samples >= Period;

        /// <summary>
        /// Required period, in data points, for the indicator to be ready and fully initialized.
        /// </summary>
        public int WarmUpPeriod => Period;

        /// <summary>
        /// Creates a new Maximum indicator with the specified period
        /// </summary>
        /// <param name="period">The period over which to look back</param>
        public Maximum(int period)
            : base($"MAX({period})", period)
        {
        }

        /// <summary>
        /// Creates a new Maximum indicator with the specified period
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="period">The period over which to look back</param>
        public Maximum(string name, int period)
            : base(name, period)
        {
        }

        /// <inheritdoc />
        protected override decimal ComputeNextValue(IReadOnlyWindow<IndicatorDataPoint> window, IndicatorDataPoint input)
        {
            if (Samples == 1 || input.Value >= Current.Value)
            {
                // our first sample or if we're bigger than our previous indicator value
                // reset the periods since maximum (it's this period) and return the value
                PeriodsSinceMaximum = 0;
                return input.Value;
            }

            if (PeriodsSinceMaximum >= Period - 1)
            {
                // at this point we need to find a new maximum
                // the window enumerates from most recent to oldest
                // so let's scour the window for the max and it's index

                // this could be done more efficiently if we were to intelligently keep track of the 'next'
                // maximum, so when one falls off, we have the other... but then we would also need the 'next, next' 
                // maximum, so on and so forth, for now this works.

                var maximum = window.Select((v, i) => new
                {
                    Value = v,
                    Index = i
                }).OrderByDescending(x => x.Value.Value).First();

                PeriodsSinceMaximum = maximum.Index;
                return maximum.Value;
            }

            // if we made it here then we didn't see a new maximum and we haven't reached our period limit,
            // so just increment our periods since maximum and return the same value as we had before
            PeriodsSinceMaximum++;
            return Current;
        }

        /// <summary>
        /// Resets this indicator to its initial state
        /// </summary>
        public override void Reset()
        {
            PeriodsSinceMaximum = 0;
            base.Reset();
        }
    }
}
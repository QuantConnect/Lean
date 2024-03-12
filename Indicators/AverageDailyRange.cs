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
using QuantConnect.Data.Market;

namespace QuantConnect.Indicators
{
    /// <summary>
    /// The AverageDailyRange indicator calculates the average price range of a security over a specific period.
    /// </summary>
    public class AverageDailyRange : BarIndicator, IIndicatorWarmUpPeriodProvider
    {
        private readonly int _period;
        private readonly RollingWindow<decimal> _dailyRanges;

        /// <summary>
        /// Gets a flag indicating when this indicator is ready and fully initialized
        /// </summary>
        public override bool IsReady => _dailyRanges.IsReady;

        /// <summary>
        /// Gets the required warm-up period in data points for the indicator to be ready and fully initialized.
        /// </summary>
        public int WarmUpPeriod => _period;

        /// <summary>
        /// Creates a new instance of the AverageDailyRange indicator with the specified period.
        /// </summary>
        /// <param name="name">The name of the indicator</param>
        /// <param name="period">The period over which to calculate the average daily range</param>
        public AverageDailyRange(string name, int period)
            : base(name)
        {
            _period = period;
            _dailyRanges = new RollingWindow<decimal>(period);

            // Warm up the indicator by populating the rolling window with zeros
            for (int i = 0; i < period; i++)
            {
                _dailyRanges.Add(0m);
            }
        }

        /// <summary>
        /// Creates a new instance of the AverageDailyRange indicator with the specified period.
        /// </summary>
        /// <param name="period">The period over which to calculate the average daily range</param>
        public AverageDailyRange(int period)
            : this($"ADR({period})", period)
        {
        }

        /// <summary>
        /// Computes the next value of the AverageDailyRange indicator.
        /// </summary>
        /// <param name="input">The input bar</param>
        /// <returns>The next value of the indicator</returns>
        protected override decimal ComputeNextValue(IBaseDataBar input)
{
    // Calculate the daily range for the current bar
    decimal dailyRange = input.High - input.Low;

    // Add the daily range to the rolling window
    _dailyRanges.Add(dailyRange);

    // Compute the sum of the daily ranges in the rolling window
    decimal sum = 0m;
    foreach (var range in _dailyRanges)
    {
        sum += range;
    }

    // Compute the average daily range over the specified period
    decimal averageDailyRange = sum / _dailyRanges.Count;

    return averageDailyRange;
}

public override void Reset()
{
    // Reset the rolling window and base class
    _dailyRanges.Reset();
    base.Reset();
}

    }
}

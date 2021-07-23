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
using System.Linq;
using System.Collections.Generic;
using QuantConnect.Data.Market;

namespace QuantConnect.Indicators
{
    /// <summary>
    /// The Relative Daily Volume indicator is an indicator that compares current 
    /// cumulative volume to the cumulative volume for a given 
    /// time of day, measured as a ratio.
    /// 
    /// Current volume from open to current time of day / Average over the past x days from open to current time of day
    /// </summary>
    public class RelativeDailyVolume : TradeBarIndicator, IIndicatorWarmUpPeriodProvider
    {
        private readonly List<decimal> _currentVolume;
        private readonly RollingWindow<DateTime> _rollingTime;
        private readonly RollingWindow<decimal> _rollingData;
        private int _previousDay;
        private bool _readyHandler;

        /// <summary>
        /// Gets a flag indicating when the indicator is ready and fully initialized
        /// </summary>
        public override bool IsReady => Samples >= WarmUpPeriod;

        /// <summary>
        /// Required period, in data points, for the indicator to be ready and fully initialized.
        /// </summary>
        public int WarmUpPeriod { get; }

        /// <summary>
        /// Initializes a new instance of the RelativeDailyVolume class using the specified period
        /// </summary>
        /// <param name="period">The period over which to perform the computation</param>
        /// /// <param name="resolution">The resolution over which to perform to computation</param>
        public RelativeDailyVolume(int period = 2, Resolution resolution = Resolution.Daily)
            : this($"RDV({period})", period, resolution)
        {
        }

        /// <summary>
        /// Returns a bool if the indicator period and resolution amount is more than one day of data
        /// </summary>
        /// <param name="period">The period over which to perform to computation</param>
        /// /// <param name="resolution">The resolution over which to perform the computation</param>
        public void CheckLength(Resolution resolution, int period)
        {
            if (resolution == Resolution.Daily && period < 1)
            {
                throw new ArgumentException("Indicator needs at least one day of data. Please increase period.", nameof(WarmUpPeriod));
            }
            else if (resolution == Resolution.Hour && period < 7)
            {
                throw new ArgumentException("Indicator needs at least one day of data. Please increase period.", nameof(WarmUpPeriod));
            }
            else if (resolution == Resolution.Minute && period < 390)
            {
                throw new ArgumentException("Indicator needs at least one day of data. Please increase period.", nameof(WarmUpPeriod));
            }
            else if (resolution == Resolution.Second && period < 23400)
            {
                throw new ArgumentException("Indicator needs at least one day of data. Please increase period.", nameof(WarmUpPeriod));
            }
            else if (resolution == Resolution.Tick)
            {
                throw new ArgumentException("Please choose a greater resolution", nameof(resolution));
            }
        }

        /// <summary>
        /// Returns the number of values to constitue a trading day based on the indicator resolution
        /// </summary>
        /// <param name="period">The period over which to perform to computation</param>
        /// /// <param name="resolution">The resolution over which to perform the computation</param>
        public int ExcessValues(Resolution resolution, int period)
        {
            if (resolution == Resolution.Daily)
            {
                return 1;
            }
            else if (resolution == Resolution.Hour)
            {
                return 7;
            }
            else if (resolution == Resolution.Minute)
            {
                return 390;
            }
            else if (resolution == Resolution.Second)
            {
                return 23400;
            }
            return period;
        }

        /// <summary>
        /// Creates a new RelativeDailyVolume indicator with the specified period
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="period">The period of this indicator</param>
        /// <param name="resolution">The resolution of this indicator</param>
        public RelativeDailyVolume(string name, int period, Resolution resolution)
            : base(name)
        {
            CheckLength(resolution, period);
            _previousDay = -1; /// No calendar day can be -1, thus default is not a calendar day
            _readyHandler = false;
            WarmUpPeriod = period;
            _currentVolume = new List<decimal>();
            _rollingTime = new RollingWindow<DateTime>(period + ExcessValues(resolution, period)); /// Adds an extra "day" to the rolling window
            _rollingData = new RollingWindow<decimal>(period + ExcessValues(resolution, period));
        }

        /// <summary>
        /// Computes the next value for this indicator from the given state.
        /// </summary>
        /// <param name="input">The input value to this indicator on this time step</param>
        /// <returns>A a value for this indicator</returns>
        protected override decimal ComputeNextValue(TradeBar input)
        {
            if (input.Time.Day != _previousDay)
            {
                _currentVolume.Clear();
                _previousDay = input.Time.Day;
            }

            _currentVolume.Add(input.Volume);
            _rollingTime.Add(input.Time);
            _rollingData.Add(input.Volume);

            if (!IsReady)
            {
                return 0;
            }
            else if (IsReady && _readyHandler == false)
            {
                _readyHandler = true;
                return 0; /// Since this is a "daily" tracked indicator, the value before the current value must always be 0.
            }

            var todaysTotal = _currentVolume.Sum();
            var timeList = _rollingTime.ToList();
            var dataList = _rollingData.ToList();
            timeList = timeList.Skip(_currentVolume.Count).Take(WarmUpPeriod).ToList(); /// Ignores "todays" volume and volume outside the periods' range
            dataList = dataList.Skip(_currentVolume.Count).Take(WarmUpPeriod).ToList();
            var currentDay = -1;
            var cummulateValue = 0;
            var relativeValues = new List<decimal>();

            foreach (var pair in timeList)
            {
                if (currentDay != pair.Day)
                {
                    if (cummulateValue != 0)
                    {
                        relativeValues.Add(cummulateValue);
                    }
                    cummulateValue = 0;
                }

                var dayDelta = input.Time - pair;
                if ((dayDelta.Days >= 1) && (pair.Hour <= input.Time.Hour) && (pair.Minute <= input.Time.Minute) && (input.Time.Second <= pair.Second))
                {
                    cummulateValue += (int)dataList[timeList.IndexOf(pair)];
                }
                currentDay = pair.Day;
            }

            if (cummulateValue != 0)
            {
                relativeValues.Add(cummulateValue);
            }

            var relativeAverage = relativeValues.Average();
            var relativeDailyVolume = todaysTotal / relativeAverage;

            return relativeDailyVolume;
        }

        /// <summary>
        /// Resets this indicator to its initial state
        /// </summary>
        public override void Reset()
        {
            _currentVolume.Clear();
            _rollingTime.Reset();
            _rollingData.Reset();
            _previousDay = -1;
            _readyHandler = false;
            base.Reset();
        }
    }
}

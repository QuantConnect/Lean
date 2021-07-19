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
    /// time of day over an x day period, measured as a ratio.
    /// 
    /// Current volume from open to current time of day / Average over the past x days from open to current time of day
    /// </summary>
    public class RelativeDailyVolume : TradeBarIndicator, IIndicatorWarmUpPeriodProvider
    {
        private readonly List<decimal> _currentVolume = new List<decimal>();
        private readonly Dictionary<DateTime, decimal> _historicalVolumes = new Dictionary<DateTime, decimal>();
        private readonly RollingWindow<DateTime> _rollingTime;
        private readonly RollingWindow<decimal> _rollingData;
        private int _previousDay;

        /// <summary>
        /// Gets a flag indicating when the indicator is ready and fully initialized
        /// </summary>
        public override bool IsReady => Samples >= WarmUpPeriod;

        /// <summary>
        /// Required period, in data points, for the indicator to be ready and fully initialized.
        /// </summary>
        public int WarmUpPeriod { get; }

        /// <summary>
        /// Initializeds a new instance of the RelativeVolume class using the specified period
        /// </summary>
        /// <param name="period">The period over which to perform to computation</param>
        public RelativeDailyVolume(int period = 14)
            : this($"RDV({period})", period)
        {
        }
        /// <summary>
        /// Creates a new RelativeVolume indicator with the specified period
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="period">The period of this indicator</param>
        public RelativeDailyVolume(string name, int period)
            : base(name)
        {
            _previousDay = -1;
            WarmUpPeriod = period;
            _rollingTime = new RollingWindow<DateTime>(period * 2);
            _rollingData = new RollingWindow<decimal>(period * 2);
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
            _historicalVolumes.Add(input.Time, input.Volume);
            _rollingTime.Add(input.Time);
            _rollingData.Add(input.Volume);

            if (!IsReady) { return 0; }

            var todaysTotal = _currentVolume.Sum();
            var timeList = _rollingTime.ToList();
            var dataList = _rollingData.ToList();
            int listCount = timeList.Count;
            var currentDay = -1;
            int listDifference = (listCount - WarmUpPeriod) - 1;
            decimal cummulateValue = 0;
            var relativeValues = new List<decimal>();

            if (listDifference != -1) { timeList.Skip(listDifference); }
            foreach (var pair in timeList)
            {
                if (currentDay != pair.Day)
                {
                    if (cummulateValue != 0) { relativeValues.Add(cummulateValue); }
                    cummulateValue = 0;
                }

                var dayDelta = input.Time - pair;
                if ((dayDelta.Days >= 1) && (pair.Hour <= input.Time.Hour) && (pair.Minute <= input.Time.Minute) && (input.Time.Second <= pair.Second))
                {
                    cummulateValue += dataList[timeList.IndexOf(pair)];
                }
                currentDay = pair.Day;
            }

            if (cummulateValue != 0) { relativeValues.Add(cummulateValue); }

            if (relativeValues.Count == 0)
            {
                throw new ArgumentException("Need atleast one day of data", nameof(WarmUpPeriod));
            }

            var relativeAverage = relativeValues.Average();
            var relativeVolume = todaysTotal / relativeAverage;

            return relativeVolume;
        }

        /// <summary>
        /// Resets this indicator to its initial state
        /// </summary>
        public override void Reset()
        {
            _currentVolume.Clear();
            _historicalVolumes.Clear();
            _rollingTime.Reset();
            _rollingData.Reset();
            _previousDay = -1;
            base.Reset();
        }
    }
}

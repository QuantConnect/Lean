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
    /// The Relative Volume indicator is an indicator that compares current 
    /// cumulative volume to the cumulative volume for a given 
    /// time of day, measured as a ratio.
    /// 
    /// Current volume from open to current time of day / Average over the past x days from open to current time of day
    /// </summary>
    public class RelativeVolume : TradeBarIndicator, IIndicatorWarmUpPeriodProvider
    {
        private readonly List<decimal> CurrentVolume = new List<decimal>();
        private readonly Dictionary<DateTime, decimal> HistoricalVolumes = new Dictionary<DateTime, decimal>();
        private DateTime _previousDay;

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
        public RelativeVolume(int period = 14)
            : this($"RVOL({period})", period)
        {
        }
        /// <summary>
        /// Creates a new RelativeVolume indicator with the specified period
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="period">The period of this indicator</param>
        public RelativeVolume(string name, int period)
            : base(name)
        {
            _previousDay = default(DateTime);
            WarmUpPeriod = period;
        }

        /// <summary>
        /// Computes the next value for this indicator from the given state.
        /// </summary>
        /// <param name="input">The input value to this indicator on this time step</param>
        /// <returns>A a value for this indicator</returns>
        protected override decimal ComputeNextValue(TradeBar input)
        {
            if (input.Time.Day != _previousDay.Day)
            {

                CurrentVolume.Clear();
                _previousDay = input.Time;

                List<DateTime> allDates = new List<DateTime>();
                allDates = HistoricalVolumes.Keys.ToList();
                allDates.Sort((ps1, ps2) => DateTime.Compare(ps1, ps2));

                if (IsReady && allDates.Count != WarmUpPeriod)
                {
                    var index = 0;
                    var delta = allDates.Count - WarmUpPeriod;
                    foreach (DateTime time in allDates)
                    {
                        if (index < delta)
                        {
                            HistoricalVolumes.Remove(time);
                        }
                        index += 1;
                    }
                }
            }

            if (!IsReady)
            {
                CurrentVolume.Add(input.Volume);
                HistoricalVolumes.Add(input.Time, input.Volume);
                return 0;
            }

            CurrentVolume.Add(input.Volume);
            HistoricalVolumes.Add(input.Time, input.Volume);

            var todaysTotal = CurrentVolume.Sum();
            List<decimal> relativeValues = new List<decimal>();
            var curDay = -1;
            decimal curValue = 0;
            foreach (KeyValuePair<DateTime, decimal> pair in HistoricalVolumes)
            {
                if (curDay != pair.Key.Day)
                {
                    if (curValue != 0)
                    {
                        relativeValues.Add(curValue);
                    }
                    curValue = 0;
                }

                var dayDelta = input.Time - pair.Key;
                if ((dayDelta.Days >= 1) && (pair.Key.Hour <= input.Time.Hour) && (pair.Key.Minute <= input.Time.Minute) && (input.Time.Second <= pair.Key.Second))
                {
                    curValue += pair.Value;
                }
                curDay = pair.Key.Day;
            }

            if (curValue != 0) { relativeValues.Add(curValue); }

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
            CurrentVolume.Clear();
            HistoricalVolumes.Clear();
            _previousDay = default(DateTime);
            base.Reset();
        }
    }
}

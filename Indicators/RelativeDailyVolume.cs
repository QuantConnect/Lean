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
        private readonly Dictionary<DateTime, SimpleMovingAverage> _relativeData;
        private readonly Dictionary<DateTime, decimal> _currentData;
        private int _previousDay;
        private int _days;

        /// <summary>
        /// Gets a flag indicating when the indicator is ready and fully initialized
        /// </summary>
        public override bool IsReady => _days == WarmUpPeriod;

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
            : this($"RDV({period})", period)
        {
        }

        /// <summary>
        /// Creates a new RelativeDailyVolume indicator with the specified period
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="period">The period of this indicator</param>
        public RelativeDailyVolume(string name, int period)
            : base(name)
        {
            _relativeData = new Dictionary<DateTime, SimpleMovingAverage>();
            _currentData = new Dictionary<DateTime, decimal>();
            WarmUpPeriod = period;
            _previousDay = -1; /// No calendar day can be -1, thus default is not a calendar day
            _days = -1; /// Will increment by one after first TradeBar, then will increment by one every new day
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
                var cummulativeVolume = 0;
                foreach (var pair in _currentData)
                {
                    /// Arbitrary year, month, day
                    DateTime timeBar = new DateTime(1, 1, 1, pair.Key.Hour, pair.Key.Minute, pair.Key.Second);
                    if (!_relativeData.ContainsKey(timeBar))
                    {
                        _relativeData[timeBar] = new SimpleMovingAverage(WarmUpPeriod);
                    }
                    cummulativeVolume += (int)pair.Value;
                    _relativeData[timeBar].Update(pair.Key, cummulativeVolume);
                }
                _currentData.Clear();
                _previousDay = input.Time.Day;
                _days += 1;
            }

            if (!_currentData.ContainsKey(input.Time))
            {
                _currentData[input.Time] = input.Volume;
            }

            if (!IsReady)
            {
                return 0;
            }

            /// Arbitrary year, month, day
            DateTime currentTimeBar = new DateTime(1, 1, 1, input.Time.Hour, input.Time.Minute, input.Time.Second);
            var denominator = 0.0m;
            if (!_relativeData.ContainsKey(currentTimeBar))
            {
                /// If there is no historical data for the current time, get most recent historical data
                /// This may come into play for crypto assets or a circuit breaker event
                var relativeDataKeys = _relativeData.Keys.ToList();
                relativeDataKeys.Sort((x, y) => DateTime.Compare(x, y));
                for (int i = 1; i < relativeDataKeys.Count; i++)
                {
                    if (relativeDataKeys[i] > currentTimeBar)
                    {
                        denominator = _relativeData[relativeDataKeys[i - 1]].Current.Value;
                    }
                }
            }
            else
            {
                denominator = _relativeData[currentTimeBar].Current.Value;
            }

            var relativeDailyVolume = _currentData.Values.Sum() / denominator;
            return relativeDailyVolume;
        }

        /// <summary>
        /// Resets this indicator to its initial state
        /// </summary>
        public override void Reset()
        {
            _relativeData.Clear();
            _currentData.Clear();
            _previousDay = -1;
            _days = -1;
            base.Reset();
        }
    }
}

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
    /// The Tom DeMark (TD) Sequential Candlestick Indicator.
    /// TD Sequential is designed to identify points of trend exhaustion and potential price reversal.
    /// It consists of two primary phases:
    /// 1. TD Setup Phase:
    ///    - Buy Setup: 9 consecutive bars where Close &lt; Close of bar 4 bars ago.
    ///    - Sell Setup: 9 consecutive bars where Close &gt; Close of bar 4 bars ago.
    /// 2. TD Countdown Phase:
    ///    - Triggers post-Setup completion, counting up to 13 bars where Close &gt;= High of 2 bars prior (Sell Countdown)
    ///      or Close &lt;= Low of 2 bars prior (Buy Countdown).
    /// </summary>
    public class TDSequential : BarIndicator, IIndicatorWarmUpPeriodProvider
    {
        private readonly RollingWindow<IBaseDataBar> _window;
        private int _setupCount;
        private int _setupDirection; // -1 for Buy Setup, +1 for Sell Setup, 0 for None
        private int _countdownCount;
        private int _countdownDirection; // -1 for Buy Countdown, +1 for Sell Countdown, 0 for None
        private bool _inCountdown;

        /// <summary>
        /// Gets the current Setup phase value (+1 to +9 for Sell Setup, -1 to -9 for Buy Setup, 0 otherwise).
        /// </summary>
        public IndicatorDataPoint SetupValue { get; private set; }

        /// <summary>
        /// Gets the current Countdown phase value (+1 to +13 for Sell Countdown, -1 to -13 for Buy Countdown, 0 otherwise).
        /// </summary>
        public IndicatorDataPoint CountdownValue { get; private set; }

        /// <summary>
        /// Gets a flag indicating when this indicator is ready and fully initialized.
        /// </summary>
        public override bool IsReady => _window.IsReady;

        /// <summary>
        /// Required period, in data points, for the indicator to be ready and fully initialized.
        /// </summary>
        public int WarmUpPeriod => 5;

        /// <summary>
        /// Initializes a new instance of the <see cref="TDSequential"/> class using the default name.
        /// </summary>
        public TDSequential()
            : this("TDSequential")
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TDSequential"/> class using the specified name.
        /// </summary>
        /// <param name="name">The name of this indicator.</param>
        public TDSequential(string name)
            : base(name)
        {
            _window = new RollingWindow<IBaseDataBar>(5);
            SetupValue = new IndicatorDataPoint(DateTime.MinValue, 0m);
            CountdownValue = new IndicatorDataPoint(DateTime.MinValue, 0m);
        }

        /// <summary>
        /// Computes the next value of this indicator from the given state.
        /// </summary>
        /// <param name="input">The input bar given to the indicator.</param>
        /// <returns>A new value for this indicator representing current countdown or setup value, or 0 if neither is active.</returns>
        protected override decimal ComputeNextValue(IBaseDataBar input)
        {
            if (input == null)
            {
                return 0m;
            }

            _window.Add(input);

            if (!_window.IsReady)
            {
                SetupValue = new IndicatorDataPoint(input.EndTime, 0m);
                CountdownValue = new IndicatorDataPoint(input.EndTime, 0m);
                return 0m;
            }

            var bar4Ago = _window[4];
            var bar2Ago = _window[2];

            if (_inCountdown)
            {
                // Active Countdown phase post-Setup completion
                SetupValue = new IndicatorDataPoint(input.EndTime, 0m);

                var qualifies = false;
                if (_countdownDirection == -1)
                {
                    // Buy Countdown: Close <= Low of 2 bars prior
                    if (input.Close <= bar2Ago.Low)
                    {
                        qualifies = true;
                    }
                }
                else if (_countdownDirection == 1)
                {
                    // Sell Countdown: Close >= High of 2 bars prior
                    if (input.Close >= bar2Ago.High)
                    {
                        qualifies = true;
                    }
                }

                if (qualifies)
                {
                    _countdownCount++;
                }

                var countdownVal = _countdownDirection * _countdownCount;
                CountdownValue = new IndicatorDataPoint(input.EndTime, countdownVal);

                if (_countdownCount >= 13)
                {
                    // Countdown completed after reaching 13 qualifying bars
                    _inCountdown = false;
                    _countdownDirection = 0;
                    _countdownCount = 0;
                    _setupCount = 0;
                    _setupDirection = 0;
                }

                return countdownVal;
            }

            // Setup Phase
            CountdownValue = new IndicatorDataPoint(input.EndTime, 0m);

            if (_setupDirection == -1)
            {
                // Existing Buy Setup
                if (input.Close < bar4Ago.Close)
                {
                    _setupCount++;
                }
                else
                {
                    // Sequence condition broke before reaching 9; reset setup count
                    _setupCount = 0;
                    _setupDirection = 0;
                }
            }
            else if (_setupDirection == 1)
            {
                // Existing Sell Setup
                if (input.Close > bar4Ago.Close)
                {
                    _setupCount++;
                }
                else
                {
                    // Sequence condition broke before reaching 9; reset setup count
                    _setupCount = 0;
                    _setupDirection = 0;
                }
            }
            else
            {
                // No active setup; check if Buy Setup or Sell Setup begins
                if (input.Close < bar4Ago.Close)
                {
                    _setupDirection = -1;
                    _setupCount = 1;
                }
                else if (input.Close > bar4Ago.Close)
                {
                    _setupDirection = 1;
                    _setupCount = 1;
                }
                else
                {
                    _setupDirection = 0;
                    _setupCount = 0;
                }
            }

            var setupVal = _setupDirection * _setupCount;
            SetupValue = new IndicatorDataPoint(input.EndTime, setupVal);

            if (_setupCount >= 9)
            {
                // 9-bar Setup completed; trigger Countdown phase for subsequent bars
                _inCountdown = true;
                _countdownDirection = _setupDirection;
                _countdownCount = 0;
                _setupCount = 0;
                _setupDirection = 0;
            }

            return setupVal;
        }

        /// <summary>
        /// Resets this indicator to its initial state.
        /// </summary>
        public override void Reset()
        {
            base.Reset();
            _window.Reset();
            _setupCount = 0;
            _setupDirection = 0;
            _countdownCount = 0;
            _countdownDirection = 0;
            _inCountdown = false;
            SetupValue = new IndicatorDataPoint(DateTime.MinValue, 0m);
            CountdownValue = new IndicatorDataPoint(DateTime.MinValue, 0m);
        }
    }
}


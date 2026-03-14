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

using System.Collections.Generic;
using QuantConnect.Data.Market;

namespace QuantConnect.Indicators
{
    /// <summary>
    /// TD Sequential is a momentum indicator developed by Tom DeMark that identifies potential
    /// trend exhaustion points. It consists of two phases:
    /// <list type="bullet">
    ///   <item><description>
    ///     <b>Setup</b>: A sequence of 9 consecutive closes each higher (sell setup) or lower
    ///     (buy setup) than the close 4 bars earlier. A completed setup counts are reported
    ///     as positive (sell) or negative (buy) integers from 1 to 9.
    ///   </description></item>
    ///   <item><description>
    ///     <b>Countdown</b>: After a completed 9-bar setup, countdown bars are accumulated until
    ///     13 qualifying bars are reached (each close &lt;= low 2 bars ago for buy countdown, or
    ///     each close >= high 2 bars ago for sell countdown). Countdown values are reported as
    ///     positive (sell) or negative (buy) integers from 1 to 13; a completed countdown of
    ///     +13 or -13 signals trend exhaustion.
    ///   </description></item>
    /// </list>
    /// Reference: https://oxfordstrat.com/trading-strategies/td-sequential-1/
    /// </summary>
    public class TDSequential : BarIndicator, IIndicatorWarmUpPeriodProvider
    {
        // Rolling window large enough for countdown look-back (we need close[4] for setup and high/low[2] for countdown)
        private readonly RollingWindow<IBaseDataBar> _window;

        // Setup state
        private int _setupCount;          // +1..+9 sell setup, -1..-9 buy setup, 0 = no active setup
        private bool _sellSetupComplete;
        private bool _buySetupComplete;

        // Countdown state
        private int _sellCountdown;       // 0..13
        private int _buyCountdown;        // 0..13

        /// <summary>
        /// Gets the current TD Setup value.
        /// Positive values (1–9) indicate a sell setup in progress; negative values (-1 to -9) a buy setup.
        /// A value of ±9 marks a completed setup.
        /// </summary>
        public IndicatorDataPoint SetupValue { get; }

        /// <summary>
        /// Gets the current TD Countdown value.
        /// Positive values (1–13) track a sell countdown; negative values (-1 to -13) a buy countdown.
        /// A value of ±13 signals potential trend exhaustion.
        /// </summary>
        public IndicatorDataPoint CountdownValue { get; }

        /// <summary>
        /// Gets a flag indicating when this indicator is ready and fully initialized.
        /// Requires at least 5 bars (4 look-back bars + current bar) for the setup phase.
        /// </summary>
        public override bool IsReady => _window.IsReady;

        /// <summary>
        /// Required period, in data points, for the indicator to be ready and fully initialized.
        /// </summary>
        public int WarmUpPeriod { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TDSequential"/> class with default name.
        /// </summary>
        public TDSequential()
            : this("TDS")
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TDSequential"/> class.
        /// </summary>
        /// <param name="name">The name of this indicator.</param>
        public TDSequential(string name)
            : base(name)
        {
            // We need current bar + 4 previous bars for setup comparison,
            // and current bar + 2 previous bars for countdown comparison.
            // Keep a window of 5 to satisfy both.
            WarmUpPeriod = 5;
            _window = new RollingWindow<IBaseDataBar>(WarmUpPeriod);

            SetupValue = new IndicatorDataPoint(name + "_Setup", 0m);
            CountdownValue = new IndicatorDataPoint(name + "_Countdown", 0m);
        }

        /// <summary>
        /// Computes the next value of this indicator from the given state.
        /// Returns the Setup bar count (positive = sell setup, negative = buy setup) while a setup
        /// is in progress, and 0 once a countdown is active (use <see cref="CountdownValue"/> for
        /// the countdown reading).
        /// </summary>
        /// <param name="input">The input bar.</param>
        /// <returns>The current setup count or 0 when no setup is active.</returns>
        protected override decimal ComputeNextValue(IBaseDataBar input)
        {
            _window.Add(input);

            if (!IsReady)
            {
                SetupValue.Value = 0m;
                CountdownValue.Value = 0m;
                return 0m;
            }

            // _window[0] = current, _window[1] = 1 bar ago, ..., _window[4] = 4 bars ago
            var currentClose = _window[0].Close;
            var close4Ago    = _window[4].Close;

            // ---- Setup Phase ----
            if (currentClose < close4Ago)
            {
                // Buy setup: each close < close 4 bars ago
                if (_setupCount <= 0)
                {
                    _setupCount--;
                }
                else
                {
                    // Interrupted sell setup; reset and start buy
                    _setupCount = -1;
                    _sellSetupComplete = false;
                }

                if (_setupCount == -9)
                {
                    _buySetupComplete = true;
                    _buyCountdown = 0;
                }
            }
            else if (currentClose > close4Ago)
            {
                // Sell setup: each close > close 4 bars ago
                if (_setupCount >= 0)
                {
                    _setupCount++;
                }
                else
                {
                    // Interrupted buy setup; reset and start sell
                    _setupCount = 1;
                    _buySetupComplete = false;
                }

                if (_setupCount == 9)
                {
                    _sellSetupComplete = true;
                    _sellCountdown = 0;
                }
            }
            else
            {
                // Close equals close-4; break the setup sequence
                _setupCount = 0;
            }

            // Clamp setup count to ±9
            if (_setupCount > 9)  _setupCount = 9;
            if (_setupCount < -9) _setupCount = -9;

            SetupValue.Value = _setupCount;

            // ---- Countdown Phase ----
            int countdownOutput = 0;

            if (_sellSetupComplete && _sellCountdown < 13)
            {
                // Sell countdown: close >= high of 2 bars ago
                if (_window[0].Close >= _window[2].High)
                {
                    _sellCountdown++;
                }
                countdownOutput = _sellCountdown;

                if (_sellCountdown == 13)
                {
                    _sellSetupComplete = false; // Reset after completed countdown
                }
            }
            else if (_buySetupComplete && _buyCountdown < 13)
            {
                // Buy countdown: close <= low of 2 bars ago
                if (_window[0].Close <= _window[2].Low)
                {
                    _buyCountdown++;
                }
                countdownOutput = -_buyCountdown;

                if (_buyCountdown == 13)
                {
                    _buySetupComplete = false; // Reset after completed countdown
                }
            }

            CountdownValue.Value = countdownOutput;
            return SetupValue.Value;
        }

        /// <summary>
        /// Resets this indicator and all sub-indicators to their initial state.
        /// </summary>
        public override void Reset()
        {
            _window.Reset();
            _setupCount = 0;
            _sellSetupComplete = false;
            _buySetupComplete = false;
            _sellCountdown = 0;
            _buyCountdown = 0;
            SetupValue.Value = 0m;
            CountdownValue.Value = 0m;
            base.Reset();
        }
    }
}

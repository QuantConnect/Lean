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
using QuantConnect.Data;

namespace QuantConnect.Indicators
{
    /// <summary>
    /// The time-series information coefficient (IC) measures how well a signal times one asset: it is the Spearman
    /// correlation, over a window of periods, between the signal's value at the end of each period and the asset's
    /// return over the next period.
    ///
    /// The indicator owns its signal: each input updates the signal first, so the signal must not be updated elsewhere.
    /// The asset's price is the input's value.
    /// </summary>
    public class TimeSeriesInformationCoefficient : IndicatorBase<IBaseData>, IIndicatorWarmUpPeriodProvider
    {
        private readonly RollingWindow<double> _signalValues;
        private readonly RollingWindow<double> _nextReturns;
        private decimal _previousPrice;
        private decimal? _previousSignalValue;

        /// <summary>
        /// The signal whose information coefficient this indicator measures
        /// </summary>
        public IIndicator Signal { get; }

        /// <summary>
        /// Gets a flag indicating when this indicator is ready and fully initialized
        /// </summary>
        public override bool IsReady => _nextReturns.IsReady;

        /// <summary>
        /// Required period, in data points, for the indicator to be ready and fully initialized.
        /// </summary>
        public int WarmUpPeriod { get; }

        /// <summary>
        /// Creates a new time-series information coefficient indicator
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="signal">The signal whose information coefficient to measure. This indicator updates it</param>
        /// <param name="period">The number of signal values and next returns to correlate</param>
        public TimeSeriesInformationCoefficient(string name, IIndicator signal, int period)
            : base(name)
        {
            if (period < 3)
            {
                throw new ArgumentException($"Period parameter for TimeSeriesInformationCoefficient indicator must be greater than 2 but was {period}");
            }
            Signal = signal;
            _signalValues = new RollingWindow<double>(period);
            _nextReturns = new RollingWindow<double>(period);
            var signalWarmUpPeriod = (signal as IIndicatorWarmUpPeriodProvider)?.WarmUpPeriod ?? 1;
            WarmUpPeriod = signalWarmUpPeriod + period;
        }

        /// <summary>
        /// Creates a new time-series information coefficient indicator
        /// </summary>
        /// <param name="signal">The signal whose information coefficient to measure. This indicator updates it</param>
        /// <param name="period">The number of signal values and next returns to correlate</param>
        public TimeSeriesInformationCoefficient(IIndicator signal, int period)
            : this($"TSIC({signal.Name},{period})", signal, period)
        {
        }

        /// <summary>
        /// Updates the signal and, once a previous signal value exists, adds it with the return since then
        /// </summary>
        /// <param name="input">The input given to the indicator</param>
        /// <returns>The Spearman correlation between the signal values and their next returns</returns>
        protected override decimal ComputeNextValue(IBaseData input)
        {
            Signal.Update(input);
            var price = input.Value;

            if (_previousSignalValue.HasValue && _previousPrice != 0)
            {
                _signalValues.Add((double)_previousSignalValue.Value);
                _nextReturns.Add((double)(price / _previousPrice - 1));
            }

            _previousPrice = price;
            _previousSignalValue = Signal.IsReady ? Signal.Current.Value : null;

            if (!IsReady)
            {
                return 0m;
            }

            var correlation = MathNet.Numerics.Statistics.Correlation.Spearman(_signalValues, _nextReturns);
            return correlation.IsNaNOrZero() ? 0m : Extensions.SafeDecimalCast(correlation);
        }

        /// <summary>
        /// Resets this indicator and its signal to their initial state
        /// </summary>
        public override void Reset()
        {
            Signal.Reset();
            _signalValues.Reset();
            _nextReturns.Reset();
            _previousPrice = 0;
            _previousSignalValue = null;
            base.Reset();
        }
    }
}

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

namespace QuantConnect.Indicators
{
    /// <summary>
    /// This indicator creates the Schaff Trend Cycle
    /// </summary>
    public class SchaffTrendCycle : Indicator, IIndicatorWarmUpPeriodProvider
    {
        // MACD Variables
        private readonly MovingAverageConvergenceDivergence _MACD;
        private readonly IndicatorBase<IndicatorDataPoint> _maximum;
        private readonly IndicatorBase<IndicatorDataPoint> _minimum;

        // _K = %K FROM MACD; _D = %D FROM _K
        private readonly IndicatorBase<IndicatorDataPoint> _K;
        private readonly IndicatorBase<IndicatorDataPoint> _D;
        private readonly IndicatorBase<IndicatorDataPoint> _maximumD;
        private readonly IndicatorBase<IndicatorDataPoint> _minimumD;

        // PF = %K FROM %MACD_D; PFF = %D FROM PF
        private readonly IndicatorBase<IndicatorDataPoint> _PF;
        private readonly IndicatorBase<IndicatorDataPoint> _PFF;

        /// <summary>
        /// Gets a flag indicating when this indicator is ready and fully initialized
        /// </summary>
        public override bool IsReady => _MACD.IsReady;

        /// <summary>
        /// Required period, in data points, for the indicator to be ready and fully initialized.
        /// </summary>
        public int WarmUpPeriod { get; }

        /// <summary>
        /// Creates the name string and calls on the indicator constructor with given parameters 
        /// https://www.tradingpedia.com/forex-trading-indicators/schaff-trend-cycle
        /// </summary>
        /// <param name="fastPeriod">The fast moving average period</param>
        /// <param name="slowPeriod">The slow moving average period</param>
        /// <param name="cyclePeriod">The signal period</param>
        /// <param name="type">The type of moving averages to use</param>
        public SchaffTrendCycle(int cyclePeriod = 10, int fastPeriod = 23, int slowPeriod = 50, MovingAverageType type = MovingAverageType.Exponential)
            : this($"SchaffTrendCycle({cyclePeriod},{fastPeriod},{slowPeriod})", cyclePeriod, fastPeriod, slowPeriod, type)
        {
        }

        /// <summary>
        /// Creates a new schaff trend cycle with the specified parameters
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="fastPeriod">The fast moving average period</param>
        /// <param name="slowPeriod">The slow moving average period</param>
        /// <param name="cyclePeriod">The signal period</param>
        /// <param name="type">The type of moving averages to use</param>
        public SchaffTrendCycle(string name, int cyclePeriod, int fastPeriod, int slowPeriod, MovingAverageType type)
            : base(name)
        {
            //Create MACD indicator and track max and min.
            _MACD = new MovingAverageConvergenceDivergence(fastPeriod, slowPeriod, cyclePeriod, type);
            _maximum = _MACD.MAX(cyclePeriod, false);
            _minimum = _MACD.MIN(cyclePeriod, false);

            //Stochastics of MACD variables
            _K = new Identity(name + "_K");
            _D = type.AsIndicator(3).Of(_K, false);
            _maximumD = _D.MAX(cyclePeriod, false);
            _minimumD = _D.MIN(cyclePeriod, false);

            //Stochastics of MACD Stochastics variables; _PFF is STC
            _PF = new Identity(name + "_PF");
            _PFF = type.AsIndicator(3).Of(_PF, false);

            WarmUpPeriod = _MACD.WarmUpPeriod;
        }

        /// <summary>
        /// Computes the next value of this indicator from the given state
        /// </summary>
        /// <param name="input">The input given to the indicator</param>
        /// <returns>A new value for this indicator</returns>
        protected override decimal ComputeNextValue(IndicatorDataPoint input)
        {
            // Update internal indicator, automatically updates _maximum and _minimum
            _MACD.Update(input);

            // Update our Stochastics K, automatically updates our Stochastics D variable which is a smoothed version of K
            var MACD_K = new IndicatorDataPoint(input.EndTime, ComputeStoch(_MACD.Current.Value, _maximum.Current.Value, _minimum.Current.Value));
            _K.Update(MACD_K);

            // With our Stochastic D values calculate PF 
            var PF = new IndicatorDataPoint(input.EndTime, ComputeStoch(_D.Current.Value, _maximumD.Current.Value, _minimumD.Current.Value));
            _PF.Update(PF);

            return _PFF.Current.Value;
        }

        /// <summary>
        /// Computes the stochastics value for a series.
        /// </summary>
        /// <param name="value">The current value of the set</param>
        /// <param name="highest">The max value of the set within a given period</param>
        /// <param name="lowest">The min value of the set within a given period</param>
        /// <returns>Stochastics value </returns>
        private decimal ComputeStoch(decimal value, decimal highest, decimal lowest)
        {
            var numerator = value - lowest;
            var denominator = highest - lowest;

            return denominator > 0 ? (numerator / denominator) * 100 : decimal.Zero;
        }

        /// <summary>
        /// Resets this indicator to its initial state
        /// </summary>
        public override void Reset()
        {
            _MACD.Reset();
            _maximum.Reset();
            _minimum.Reset();
            _K.Reset();
            _D.Reset();
            _maximumD.Reset();
            _minimumD.Reset();
            _PF.Reset();
            _PFF.Reset();
            base.Reset();
        }
    }
}

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
        private readonly MovingAverageConvergenceDivergence _MACD;
        private readonly IndicatorBase<IndicatorDataPoint> _maximum;
        private readonly IndicatorBase<IndicatorDataPoint> _minimum;
        private readonly IndicatorBase<IndicatorDataPoint> _sumFastK;
        private readonly IndicatorBase<IndicatorDataPoint> _sumSlowK;

        /// <summary>
        /// Gets the value of the Slow Stochastics given Period K.
        /// </summary>
        public IndicatorBase<IndicatorDataPoint> StochK { get; }

        /// <summary>
        /// Gets the value of the Slow Stochastics given Period D.
        /// </summary>
        public IndicatorBase<IndicatorDataPoint> StochD { get; }

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
        /// <param name="dPeriod">The D period given to calculated the Slow %D</param>
        /// <param name="type">The type of moving averages to use</param>

        public SchaffTrendCycle(int fastPeriod = 23, int slowPeriod = 50, int cyclePeriod = 10, int dPeriod = 3, MovingAverageType type = MovingAverageType.Exponential)
            : this($"SchaffTrendCycle({fastPeriod},{slowPeriod},{cyclePeriod})", fastPeriod, slowPeriod, cyclePeriod, dPeriod, type)
        {
        }

        /// <summary>
        /// Creates a new schaff trend cycle with the specified parameters
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="fastPeriod">The fast moving average period</param>
        /// <param name="slowPeriod">The slow moving average period</param>
        /// <param name="cyclePeriod">The signal period</param>
        /// <param name="dPeriod">The D period given to calculated the Slow %D</param>
        /// <param name="type">The type of moving averages to use</param>
        public SchaffTrendCycle(string name, int fastPeriod, int slowPeriod, int cyclePeriod, int dPeriod, MovingAverageType type)
            : base(name)
        {
            //Create MACD indicator and build Stochastics that take MACD values as input.
            _MACD = new MovingAverageConvergenceDivergence(fastPeriod, slowPeriod, cyclePeriod, type);
            _maximum = _MACD.MAX(cyclePeriod,false);
            _minimum = _MACD.MIN(cyclePeriod,false);
            _sumFastK = new Sum(name + "_SumFastK", cyclePeriod).Of(_MACD, false);
            _sumSlowK = new Sum(name + "_SumD", dPeriod).Of(_MACD, false);


            StochK = new FunctionalIndicator<IndicatorDataPoint>(name + "_StochK",
                input => ComputeStochK(cyclePeriod, input),
                stochK => _maximum.IsReady,
                () => { }
            ).Of(_MACD, false);

            StochD = new FunctionalIndicator<IndicatorDataPoint>(
                name + "_StochD",
                input => ComputeStochD(cyclePeriod, dPeriod),
                stochD => _maximum.IsReady,
                () => { }
            ).Of(_MACD, false);

            WarmUpPeriod = cyclePeriod;
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

            var denominator = StochD - StochK;

            // if there's no range, just return constant zero
            if (denominator == 0m)
            {
                return 0m;
            }

            var numerator = _MACD - StochK;
            var STC = _maximum.Samples >= WarmUpPeriod ? numerator / denominator : decimal.Zero;
            return STC;
        }

        /// <summary>
        /// Computes the Slow Stochastic %K.
        /// </summary>
        /// <param name="period">The period.</param>
        /// <param name="input">The input.</param>
        /// <returns>The Slow Stochastics %K value.</returns>
        private decimal ComputeStochK(int period, IndicatorDataPoint input)
        {
            var stochK = _maximum.Samples >= (period) ? _sumFastK / period : decimal.Zero;
            _sumSlowK.Update(input.Time, stochK);
            return stochK;
        }

        /// <summary>
        /// Computes the Slow Stochastic %D.
        /// </summary>
        /// <param name="period">The period.</param>
        /// <param name="dPeriod">The period for StochD Calculation</param>
        /// <returns>The Slow Stochastics %D value.</returns>
        private decimal ComputeStochD(int period, int dPeriod)
        {
            var stochD = _maximum.Samples >= (period + dPeriod - 2) ? _sumSlowK / dPeriod : decimal.Zero;
            return stochD;
        }

        /// <summary>
        /// Resets this indicator to its initial state
        /// </summary>
        public override void Reset()
        {
            _MACD.Reset();
            _maximum.Reset();
            _minimum.Reset();
            base.Reset();
        }
    }
}

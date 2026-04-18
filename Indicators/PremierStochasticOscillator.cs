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
    /// Premier Stochastic Oscillator (PSO) Indicator implementation.
    /// This indicator combines a stochastic oscillator with exponential moving averages to provide
    /// a normalized output between -1 and 1, which can be useful for identifying trends and 
    /// potential reversal points in the market.
    /// </summary>
    public class PremierStochasticOscillator : BarIndicator, IIndicatorWarmUpPeriodProvider
    {
        /// <summary>
        /// Exponential Moving Averages (EMA) used in the calculation of the Premier Stochastic Oscillator (PSO).
        /// firstSmoothingEma performs the first smoothing of the Normalized Stochastic (0.1 * (Fast%K - 50)),
        /// and doubleSmoothingEma applies a second smoothing on the result of _ema1, resulting in the Double-Smoothed Normalized Stochastic
        /// </summary>
        private readonly ExponentialMovingAverage _firstSmoothingEma;
        private readonly ExponentialMovingAverage _doubleSmoothingEma;

        /// <summary>
        /// Stochastic oscillator used to calculate the K value.
        /// </summary>
        private readonly Stochastic _stochastic;

        /// <summary>
        /// The warm-up period necessary before the PSO indicator is considered ready.
        /// </summary>
        public int WarmUpPeriod { get; }

        /// <summary>
        /// Constructor for the Premier Stochastic Oscillator.
        /// Initializes the Stochastic and EMA indicators and calculates the warm-up period.
        /// </summary>
        /// <param name="name">The name of the indicator.</param>
        /// <param name="period">The period given to calculate FastK.</param>
        /// <param name="emaPeriod">The period for EMA calculations.</param>
        public PremierStochasticOscillator(string name, int period, int emaPeriod) : base(name)
        {
            _stochastic = new Stochastic(name, period, period, period);
            _firstSmoothingEma = new ExponentialMovingAverage(emaPeriod);
            _doubleSmoothingEma = _firstSmoothingEma.EMA(emaPeriod);
            WarmUpPeriod = period + 2 * (emaPeriod - 1);
        }

        /// <summary>
        /// Overloaded constructor to facilitate instantiation with a default name format.
        /// </summary>
        /// <param name="period">The period given to calculate FastK.</param>
        /// <param name="emaPeriod">The period for EMA calculations.</param>
        public PremierStochasticOscillator(int period, int emaPeriod)
            : this($"PSO({period},{emaPeriod})", period, emaPeriod)
        {
        }

        /// <summary>
        /// Gets a flag indicating when this indicator is ready and fully initialized
        /// </summary>
        public override bool IsReady => _doubleSmoothingEma.IsReady;

        /// <summary>
        /// Computes the Premier Stochastic Oscillator (PSO) based on the current input.
        /// This calculation involves updating the stochastic oscillator and the EMAs,
        /// followed by calculating the PSO using the formula:
        /// PSO = (exp(EMA2) - 1) / (exp(EMA2) + 1)
        /// </summary>
        /// <param name="input">The current input bar containing market data.</param>
        /// <returns>The computed value of the PSO.</returns>
        protected override decimal ComputeNextValue(IBaseDataBar input)
        {
            _stochastic.Update(input);
            if (!_stochastic.FastStoch.IsReady)
            {
                return decimal.Zero;
            }

            var k = _stochastic.FastStoch.Current.Value;
            var nsk = 0.1m * (k - 50);
            if (!_firstSmoothingEma.Update(new IndicatorDataPoint(input.EndTime, nsk)))
            {
                return decimal.Zero;
            }

            if (!_doubleSmoothingEma.IsReady)
            {
                return decimal.Zero;
            }
            var expss = (decimal)Math.Exp((double)_doubleSmoothingEma.Current.Value);
            return (expss - 1) / (expss + 1);
        }

        /// <summary>
        /// Resets this indicator to its initial state
        /// </summary>
        public override void Reset()
        {
            _stochastic.Reset();
            _firstSmoothingEma.Reset();
            _doubleSmoothingEma.Reset();
            base.Reset();
        }
    }
}

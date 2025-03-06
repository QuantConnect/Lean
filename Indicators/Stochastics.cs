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
using QuantConnect.Data.Market;

namespace QuantConnect.Indicators
{
    /// <summary>
    /// This indicator computes the Slow Stochastics %K and %D. The Fast Stochastics %K is is computed by 
    /// (Current Close Price - Lowest Price of given Period) / (Highest Price of given Period - Lowest Price of given Period)
    /// multiplied by 100. Once the Fast Stochastics %K is calculated the Slow Stochastic %K is calculated by the average/smoothed price of
    /// of the Fast %K with the given period. The Slow Stochastics %D is then derived from the Slow Stochastics %K with the given period.
    /// </summary>
    public class Stochastic : BarIndicator, IIndicatorWarmUpPeriodProvider
    {
        private readonly IndicatorBase<IndicatorDataPoint> _maximum;
        private readonly IndicatorBase<IndicatorDataPoint> _minimum;
        private readonly IndicatorBase<IndicatorDataPoint> _sumFastK;
        private readonly IndicatorBase<IndicatorDataPoint> _sumSlowK;

        /// <summary>
        /// Gets the value of the Fast Stochastics %K given Period.
        /// </summary>
        public IndicatorBase<IBaseDataBar> FastStoch { get; }

        /// <summary>
        /// Gets the value of the Slow Stochastics given Period K.
        /// </summary>
        public IndicatorBase<IBaseDataBar> StochK { get; }

        /// <summary>
        /// Gets the value of the Slow Stochastics given Period D.
        /// </summary>
        public IndicatorBase<IBaseDataBar> StochD { get; }

        /// <summary>
        /// Creates a new Stochastics Indicator from the specified periods.
        /// </summary>
        /// <param name="name">The name of this indicator.</param>
        /// <param name="period">The period given to calculate the Fast %K</param>
        /// <param name="kPeriod">The K period given to calculated the Slow %K</param>
        /// <param name="dPeriod">The D period given to calculated the Slow %D</param>
        public Stochastic(string name, int period, int kPeriod, int dPeriod)
            : base(name)
        {
            _maximum = new Maximum(name + "_Max", period);
            _minimum = new Minimum(name + "_Min", period);
            _sumFastK = new Sum(name + "_SumFastK", kPeriod);
            _sumSlowK = new Sum(name + "_SumD", dPeriod);

            FastStoch = new FunctionalIndicator<IBaseDataBar>(name + "_FastStoch",
                input => ComputeFastStoch(input),
                fastStoch => _maximum.IsReady
                );

            StochK = new FunctionalIndicator<IBaseDataBar>(name + "_StochK",
                input => ComputeStochK(kPeriod, input),
                stochK => _sumFastK.IsReady
            );

            StochD = new FunctionalIndicator<IBaseDataBar>(
                name + "_StochD",
                input => ComputeStochD(dPeriod),
                stochD => _sumSlowK.IsReady
            );

            // Subtracting 2 since the first value is calculated after 'period' bars, 
            // and each smoothing step adds (kPeriod - 1) and (dPeriod - 1) respectively.
            WarmUpPeriod = period + kPeriod + dPeriod - 2;
        }

        /// <summary>
        /// Creates a new <see cref="Stochastic"/> indicator from the specified inputs.
        /// </summary>
        /// <param name="period">The period given to calculate the Fast %K</param>
        /// <param name="kPeriod">The K period given to calculated the Slow %K</param>
        /// <param name="dPeriod">The D period given to calculated the Slow %D</param>
        public Stochastic(int period, int kPeriod, int dPeriod)
            : this($"STO({period},{kPeriod},{dPeriod})", period, kPeriod, dPeriod)
        {
        }

        /// <summary>
        /// Gets a flag indicating when this indicator is ready and fully initialized
        /// </summary>
        public override bool IsReady => FastStoch.IsReady && StochK.IsReady && StochD.IsReady;

        /// <summary>
        /// Required period, in data points, for the indicator to be ready and fully initialized.
        /// </summary>
        public int WarmUpPeriod { get; }

        /// <summary>
        /// Computes the next value of this indicator from the given state
        /// </summary>
        /// <param name="input">The input given to the indicator</param>
        protected override decimal ComputeNextValue(IBaseDataBar input)
        {
            _maximum.Update(input.Time, input.High);
            _minimum.Update(input.Time, input.Low);
            FastStoch.Update(input);
            StochK.Update(input);
            StochD.Update(input);

            return FastStoch.Current.Value;
        }

        /// <summary>
        /// Computes the Fast Stochastic %K.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <returns>The Fast Stochastics %K value.</returns>
        private decimal ComputeFastStoch(IBaseDataBar input)
        {
            var fastStoch = 0m;
            // It requires at least 'period' data points to compute Fast %K.
            if (_maximum.IsReady)
            {
                var denominator = _maximum.Current.Value - _minimum.Current.Value;

                // if there's no range, just return constant zero
                if (denominator == 0m)
                {
                    return 0m;
                }

                var numerator = input.Close - _minimum.Current.Value;
                fastStoch = numerator / denominator;
                _sumFastK.Update(input.Time, fastStoch);
            }
            return fastStoch * 100;
        }

        /// <summary>
        /// Computes the Slow Stochastic %K.
        /// </summary>
        /// <param name="constantK">The constant k.</param>
        /// <param name="input">The input.</param>
        /// <returns>The Slow Stochastics %K value.</returns>
        private decimal ComputeStochK(int constantK, IBaseData input)
        {
            var stochK = 0m;
            // It requires at least 'kPeriod' updates in _sumFastK for calculation.  
            if (_sumFastK.IsReady)
            {
                stochK = _sumFastK.Current.Value / constantK;
                _sumSlowK.Update(input.Time, stochK);
            }
            return stochK * 100;
        }

        /// <summary>
        /// Computes the Slow Stochastic %D.
        /// </summary>
        /// <param name="constantD">The constant d.</param>
        /// <returns>The Slow Stochastics %D value.</returns>
        private decimal ComputeStochD(int constantD)
        {
            var stochD = 0m;
            // It requires at least 'dPeriod' updates in _sumSlowK for calculation  
            if (_sumSlowK.IsReady)
            {
                stochD = _sumSlowK.Current.Value / constantD;
            }
            return stochD * 100;
        }
        /// <summary>
        /// Resets this indicator to its initial state
        /// </summary>
        public override void Reset()
        {
            FastStoch.Reset();
            StochK.Reset();
            StochD.Reset();
            _maximum.Reset();
            _minimum.Reset();
            _sumFastK.Reset();
            _sumSlowK.Reset();
            base.Reset();
        }
    }
}

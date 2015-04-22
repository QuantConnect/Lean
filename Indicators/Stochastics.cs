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

using QuantConnect.Data.Market;

namespace QuantConnect.Indicators
{
    /// <summary>
    /// This indicator computes the Slow Stochastics %K and %D. The Fast Stochastics %K is is computed by 
    /// (Current Close Price - Lowest Price of given Period) / (Highest Price of given Period - Lowest Price of given Period)
    /// multiplied by 100. Once the Fast Stochastics %K is calculated the Slow Stochastic %K is calculated by the average/smoothed price of
    /// of the Fast %K with the given period. The Slow Stochastics %D is then derived from the Slow Stochastics %K with the given period.
    /// </summary>
    public class Stochastic : TradeBarIndicator
    {
        /// <summary>
        /// Gets the value of the Fast Stochastics %K given Period.
        /// </summary>
        public IndicatorBase<TradeBar> FastStoch { get; private set; }

        /// <summary>
        /// Gets the value of the Slow Stochastics given Period K.
        /// </summary>
        public IndicatorBase<TradeBar> StochK { get; private set; }

        /// <summary>
        /// Gets the value of the Slow Stochastics given Period D.
        /// </summary>
        public IndicatorBase<TradeBar> StochD { get; private set; }

        /// <summary>
        /// Gets or sets the maximum of given period.
        /// </summary>
        private IndicatorBase<IndicatorDataPoint> Maximum { get; set; }

        /// <summary>
        /// Gets or sets the mininum of given period.
        /// </summary>
        private IndicatorBase<IndicatorDataPoint> Mininum { get; set; }

        /// <summary>
        /// Placeholder to calculate the sum of Fast %K.
        /// </summary>
        public IndicatorBase<IndicatorDataPoint> SumFastK { get; private set; }

        /// <summary>
        /// Placeholder to calculate the sum of Slow %K.
        /// </summary>
        public IndicatorBase<IndicatorDataPoint> SumSlowK { get; private set; }

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
            Maximum = new Maximum(name + "_Max", period);
            Mininum = new Minimum(name + "_Min", period);
            SumFastK = new Sum(name + "_SumFastK", kPeriod);
            SumSlowK = new Sum(name + "_SumD", dPeriod);

            FastStoch = new FunctionalIndicator<TradeBar>(name + "_FastStoch",
                input => ComputeFastStoch(period, input),
                fastStoch => Maximum.IsReady,
                () => Maximum.Reset()
                );

            StochK = new FunctionalIndicator<TradeBar>(name + "_StochK",
                input => ComputeStochK(period, kPeriod, input),
                stochK => Maximum.IsReady,
                () => Maximum.Reset()
                );

            StochD = new FunctionalIndicator<TradeBar>(name + "_StochD",
                input => ComputeStochD(period, kPeriod, dPeriod),
                stochD => Maximum.IsReady,
                () => Maximum.Reset()
                );
        }

        /// <summary>
        /// Gets a flag indicating when this indicator is ready and fully initialized
        /// </summary>
        public override bool IsReady
        {
            get { return FastStoch.IsReady && StochK.IsReady && StochD.IsReady; }
        }
        /// <summary>
        /// Computes the next value of this indicator from the given state
        /// </summary>
        /// <param name="input">The input given to the indicator</param>
        protected override decimal ComputeNextValue(TradeBar input)
        {
            Maximum.Update(input.Time, input.High);
            Mininum.Update(input.Time, input.Low);
            FastStoch.Update(input);
            StochK.Update(input);
            StochD.Update(input);
            return StochK * 100;
        }

        /// <summary>
        /// Computes the Fast Stochastic %K.
        /// </summary>
        /// <param name="period">The period.</param>
        /// <param name="input">The input.</param>
        /// <returns>The Fast Stochastics %K value.</returns>
        private decimal ComputeFastStoch(int period, TradeBar input)
        {
            var fastStoch = Maximum.Samples >= period ? (input.Close - Mininum) / (Maximum - Mininum) : new decimal(0.0);
            SumFastK.Update(input.Time, fastStoch);
            return fastStoch;
        }

        /// <summary>
        /// Computes the Slow Stochastic %K.
        /// </summary>
        /// <param name="period">The period.</param>
        /// <param name="constantK">The constant k.</param>
        /// <param name="input">The input.</param>
        /// <returns>The Slow Stochastics %K value.</returns>
        private decimal ComputeStochK(int period, int constantK, TradeBar input)
        {
            var stochK = Maximum.Samples >= (period + constantK - 1) ? SumFastK / constantK : new decimal(0.0);
            SumSlowK.Update(input.Time, stochK);
            return stochK * 100;
        }

        /// <summary>
        /// Computes the Slow Stochastic %D.
        /// </summary>
        /// <param name="period">The period.</param>
        /// <param name="constantK">The constant k.</param>
        /// <param name="constantD">The constant d.</param>
        /// <returns>The Slow Stochastics %D value.</returns>
        private decimal ComputeStochD(int period, int constantK, int constantD)
        {
            var stochD = Maximum.Samples >= (period + constantK + constantD - 2) ? SumSlowK / constantD : new decimal(0.0);
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
            SumFastK.Reset();
            SumSlowK.Reset();
            base.Reset();
        }
    }
}

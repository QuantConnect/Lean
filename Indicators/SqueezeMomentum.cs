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
    /// The SqueezeMomentum indicator calculates whether the market is in a "squeeze" condition,
    /// determined by comparing Bollinger Bands to Keltner Channels. When the Bollinger Bands are
    /// inside the Keltner Channels, the indicator returns 1 (squeeze on). Otherwise, it returns -1 (squeeze off).
    /// </summary>
    public class SqueezeMomentum : BarIndicator, IIndicatorWarmUpPeriodProvider
    {
        /// <summary>
        /// The Bollinger Bands indicator used to calculate the upper, lower, and middle bands.
        /// </summary>
        public BollingerBands BollingerBands { get; }

        /// <summary>
        /// The Keltner Channels indicator used to calculate the upper, lower, and middle channels.
        /// </summary>
        public KeltnerChannels KeltnerChannels { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SqueezeMomentum"/> class.
        /// </summary>
        /// <param name="name">The name of the indicator.</param>
        /// <param name="bollingerPeriod">The period used for the Bollinger Bands calculation.</param>
        /// <param name="bollingerMultiplier">The multiplier for the Bollinger Bands width.</param>
        /// <param name="keltnerPeriod">The period used for the Average True Range (ATR) calculation in Keltner Channels.</param>
        /// <param name="keltnerMultiplier">The multiplier applied to the ATR for calculating Keltner Channels.</param>
        public SqueezeMomentum(string name, int bollingerPeriod, decimal bollingerMultiplier, int keltnerPeriod, decimal keltnerMultiplier) : base(name)
        {
            BollingerBands = new BollingerBands(bollingerPeriod, bollingerMultiplier);
            KeltnerChannels = new KeltnerChannels(keltnerPeriod, keltnerMultiplier, MovingAverageType.Exponential);
            WarmUpPeriod = Math.Max(bollingerPeriod, keltnerPeriod);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SqueezeMomentum"/> class.
        /// </summary>
        /// <param name="bollingerPeriod">The period used for the Bollinger Bands calculation.</param>
        /// <param name="bollingerMultiplier">The multiplier for the Bollinger Bands width.</param>
        /// <param name="keltnerPeriod">The period used for the Average True Range (ATR) calculation in Keltner Channels.</param>
        /// <param name="keltnerMultiplier">The multiplier applied to the ATR for calculating Keltner Channels.</param>
        public SqueezeMomentum(int bollingerPeriod, decimal bollingerMultiplier, int keltnerPeriod, decimal keltnerMultiplier)
            : this($"SM({bollingerPeriod},{bollingerMultiplier},{keltnerPeriod},{keltnerMultiplier})", bollingerPeriod, bollingerMultiplier, keltnerPeriod, keltnerMultiplier)
        {
        }

        /// <summary>
        /// Gets the warm-up period required for the indicator to be ready.
        /// This is determined by the warm-up period of the Bollinger Bands indicator.
        /// </summary>
        public int WarmUpPeriod { get; }

        /// <summary>
        /// Indicates whether the indicator is ready and has enough data for computation.
        /// The indicator is ready when both the Bollinger Bands and the Average True Range are ready.
        /// </summary>
        public override bool IsReady => BollingerBands.IsReady && KeltnerChannels.IsReady;

        /// <summary>
        /// Computes the next value of the indicator based on the input data bar.
        /// </summary>
        /// <param name="input">The input data bar.</param>
        /// <returns>
        /// Returns 1 if the Bollinger Bands are inside the Keltner Channels (squeeze on),
        /// or -1 if the Bollinger Bands are outside the Keltner Channels (squeeze off).
        /// </returns>
        protected override decimal ComputeNextValue(IBaseDataBar input)
        {
            BollingerBands.Update(input);
            KeltnerChannels.Update(input);
            if (!IsReady)
            {
                return decimal.Zero;
            }

            // Calculate Bollinger Bands upper, lower
            var bbUpper = BollingerBands.UpperBand.Current.Value;
            var bbLower = BollingerBands.LowerBand.Current.Value;

            // Calculate Keltner Channels upper and lower bounds
            var kcUpper = KeltnerChannels.UpperBand.Current.Value;
            var kcLower = KeltnerChannels.LowerBand.Current.Value;

            // Determine if the squeeze condition is on or off
            return (kcUpper > bbUpper && kcLower < bbLower) ? 1m : -1m;
        }

        /// <summary>
        /// Resets the state of the indicator, including all sub-indicators.
        /// </summary>
        public override void Reset()
        {
            BollingerBands.Reset();
            KeltnerChannels.Reset();
            base.Reset();
        }
    }
}

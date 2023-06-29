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
 *
*/

using QuantConnect.Data.Market;

namespace QuantConnect.Indicators
{
    /// <summary>
    /// Volume Weighted Average Price (VWAP) Indicator:
    /// It is calculated by adding up the dollars traded for every transaction (price multiplied
    /// by number of shares traded) and then dividing by the total shares traded for the day.
    /// </summary>
    public class VolumeWeightedAveragePriceIndicator : TradeBarIndicator, IIndicatorWarmUpPeriodProvider
    {
        /// <summary>
        /// In this VWAP calculation, typical price is defined by (O + H + L + C) / 4
        /// </summary>
        private readonly int _period;
        protected readonly Identity Price;
        protected readonly Identity Volume;
        protected CompositeIndicator VWAP;

        /// <summary>
        /// Initializes a new instance of the VWAP class with the default name and period
        /// </summary>
        /// <param name="period">The period of the VWAP</param>
        public VolumeWeightedAveragePriceIndicator(int period)
            : this($"VWAP({period})", period)
        {
        }

        /// <summary>
        /// Initializes a new instance of the VWAP class with a given name and period
        /// </summary>
        /// <param name="name">string - the name of the indicator</param>
        /// <param name="period">The period of the VWAP</param>
        public VolumeWeightedAveragePriceIndicator(string name, int period)
            : base(name)
        {
            _period = period;

            Price = new Identity("Price");
            Volume = new Identity("Volume");

            // This class will be using WeightedBy indicator extension
            VWAP = Price.WeightedBy(Volume, period);
        }

        /// <summary>
        /// Gets a flag indicating when this indicator is ready and fully initialized
        /// </summary>
        public override bool IsReady => VWAP.IsReady;

        /// <summary>
        /// Required period, in data points, for the indicator to be ready and fully initialized.
        /// </summary>
        public int WarmUpPeriod => _period;

        /// <summary>
        /// Resets this indicator to its initial state
        /// </summary>
        public override void Reset()
        {
            Price.Reset();
            Volume.Reset();
            VWAP.Reset();
            base.Reset();
        }

        /// <summary>
        /// Computes the next value of this indicator from the given state
        /// </summary>
        /// <param name="input">The input given to the indicator</param>
        /// <returns>A new value for this indicator</returns>
        protected override decimal ComputeNextValue(TradeBar input)
        {
            Price.Update(input.EndTime, GetTimeWeightedAveragePrice(input));
            Volume.Update(input.EndTime, input.Volume);
            return VWAP.Current.Value;
        }

        /// <summary>
        /// Gets an estimated average price to use for the interval covered by the input trade bar.
        /// </summary>
        /// <param name="input">The current trade bar input</param>
        /// <returns>An estimated average price over the trade bar's interval</returns>
        protected virtual decimal GetTimeWeightedAveragePrice(TradeBar input)
        {
            return (input.Open + input.High + input.Low + input.Value) / 4;
        }
    }
}

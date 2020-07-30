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
    /// This indicator computes the n-period Ease of Movement Value using the following:
    /// MID = (high_1 + low_1)/2 - (high_0 + low_0)/2 
    /// RATIO = (currentVolume/10000) / (high_1 - low_1)
    /// EMV = MID/ratio
    /// </summary>
    public class EaseOfMovementValue : TradeBarIndicator, IIndicatorWarmUpPeriodProvider
    {

        private readonly int _period;
        private decimal _previoushighprice { get; private set; }
        private decimal _previouslowprice { get; private set; }

        /// <summary>
        /// Gets a flag indicating when this indicator is ready and fully initialized
        /// </summary>
        public override bool IsReady => Samples >= _period + 1;

        /// <summary>
        /// Required period, in data points, for the indicator to be ready and fully initialized.
        /// </summary>
        public int WarmUpPeriod => _period + 1;


        public override void Reset()
        {
            _previoushighprice = 0;
            _previouslowprice = 0;
            base.Reset();
        }

        /// <summary>
        /// Initializeds a new instance of the EaseOfMovement class using the specufued period
        /// </summary>
        /// <param name="period">The period over which to perform to computation</param>
        public EaseOfMovementValue(int period = 2)
            : this($"EMV({period})", period)
        {
        }

        /// <summary>
        /// Creates a new EaseOfMovement indicator with the specified period
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="period">The period over which to perform to computation</param>
        public EaseOfMovementValue(string name, int period)
            : base(name)
        {
            WarmUpPeriod = period;
            _previousHighMaximum = new Delay(1).MAX(period);
            _previousLowMinimum = new Delay(1).MIN(period);
        }

        /// <summary>
        /// Computes the next value for this indicator from the given state.
        /// </summary>
        /// <param name="window">The window of data held in this indicator</param>
        /// <param name="input">The input value to this indicator on this time step</param>
        /// <returns>A a value for this indicator</returns>
        protected override decimal ComputeNextValue(TradeBar input)
        {
            if (input.High-input.Low == 0 || input.Volume == 0)
            {
                return 0;
            }

            if (_previoushighprice + _previouslowprice == 0)
            {
                _previoushighprice = input.High;
                _previouslowprice = input.Low;
                return 0;
            }

            var MIDvalue = ((input.High + input.Low) / 2) - ((_previoushighprice + _previouslowprice) / 2);
            var MIDratio = ((input.Volume / 10000) / (input.High - input.Low));

            _previoushighprice = input.High;
            _previouslowprice = input.Low;

            return (MIDvalue / MIDratio);
        }
    }
}
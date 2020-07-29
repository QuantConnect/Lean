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

namespace QuantConnect.Indicators
{
    /// <summary>
    /// This indicator computes the n-period Ease of Movement Value using the following:
    /// MID = (high_1 + low_1)/2 - (high_0 + low_0)/2 
    /// RATIO = (currentVolume/10000) / (high_1 - low_1)
    /// EMV = MID/ratio
    /// </summary>
    public class EaseOfMovement : WindowIndicator<BarIndicator>, IIndicatorWarmUpPeriodProvider
    {

        private readonly int _period;
        private readonly Minimum _minimum;
        private readonly Maximum _maximum;


        /// <summary>
        /// Initializeds a new instance of the EaseOfMovement class using the specufued period
        /// </summary>
        /// <param name="period">The period over which to perform to computation</param>
        public EaseOfMovement(int period = 2)
            : this($"EMV({period})", period)
        {
        }

        /// <summary>
        /// Creates a new EaseOfMovement indicator with the specified period
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="period">The period over which to perform to computation</param>
        public EaseOfMovement(string name, int period)
            : base(name, period)
        {
            _period = period;
            _maximum = new Maximum(period);
            _minimum = new Minimum(period);
        }



        /// <summary>
        /// Gets a flag indicating when this indicator is ready and fully initialized
        /// </summary>
        public override bool IsReady => _maximum.IsReady && _minimum.IsReady;

        /// <summary>
        /// Required period, in data points, for the indicator to be ready and fully initialized.
        /// </summary>
        public int WarmUpPeriod => _period;

        /// <summary>
        /// Computes the next value for this indicator from the given state.
        /// </summary>
        /// <param name="window">The window of data held in this indicator</param>
        /// <param name="input">The input value to this indicator on this time step</param>
        /// <returns>A a value for this indicator</returns>
        protected override decimal ComputeNextValue(IReadOnlyWindow<IBaseDataBar> window, IBaseDataBar input)
        {
            var previousMax = 0;
            var previousMin = 0;
            var currentMax = 0;
            var currentMin = 0;

            if (Samples > 1 || input.Time < Current.Time)
            {
                _maximum.Update(new IndicatorDataPoint { Value = Current.High });
                _minimum.Update(new IndicatorDataPoint { Value = Current.Low });
                previousMax = _maximum.Current.Value;
                previousMin = _minimum.Current.Value;

                _maximum.Update(new IndicatorDataPoint { Value = input.High });
                _minimum.Update(new IndicatorDataPoint { Value = input.Low });
                currentMax = _maximum.Current.Value;
                currentMin = _minimum.Current.Value;

            }
            
            
            return (((currentMax + currentMin) / 2) - ((previousMax + previousMin) / 2)) / ((input.Volume/10000) / (currentMax - currentMin));
            
        }

        
    }
}
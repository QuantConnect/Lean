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
using System.Linq;

namespace QuantConnect.Indicators
{
    /// <summary>
    /// Smooth and high sensitive moving Average. This moving average reduce lag of the information
    /// but still being smooth to reduce noises.
    /// Is a weighted moving average, which weights have a Normal shape;
    /// the parameters Sigma and Offset affect the kurtosis and skewness of the weights respectively.
    /// Source: https://www.cjournal.cz/files/308.pdf
    /// </summary>
    /// <seealso cref="IndicatorDataPoint" />
    public class ArnaudLegouxMovingAverage : WindowIndicator<IndicatorDataPoint>, IIndicatorWarmUpPeriodProvider
    {
        private readonly decimal[] _weightVector;

        /// <summary>
        /// Required period, in data points, for the indicator to be ready and fully initialized.
        /// </summary>
        public int WarmUpPeriod => _weightVector.Length;

        /// <summary>
        /// Initializes a new instance of the <see cref="ArnaudLegouxMovingAverage" /> class.
        /// </summary>
        /// <param name="name">string - a name for the indicator</param>
        /// <param name="period">int - the number of periods to calculate the ALMA</param>
        /// <param name="sigma">
        /// int - this parameter is responsible for the shape of the curve coefficients. It affects the weight vector kurtosis.
        /// </param>
        /// <param name="offset">
        /// decimal - This parameter allows regulating the smoothness and high sensitivity of the
        /// Moving Average. The range for this parameter is [0, 1]. It affects the weight vector skewness.
        /// </param>
        public ArnaudLegouxMovingAverage(string name, int period, int sigma = 6, decimal offset = 0.85m)
            : base(name, period)
        {
            if (offset < 0 || offset > 1) throw new ArgumentException($"Offset parameter range is [0,1]. Value: {offset}", nameof(offset));

            var m = Math.Floor(offset * (period - 1));
            var s = period * 1m / sigma;

            var tmpVector = Enumerable.Range(0, period)
                .Select(i => Math.Exp((double) (-(i - m) * (i - m) / (2 * s * s))))
                .ToArray();

            _weightVector = tmpVector
                .Select(i => (decimal) (i / tmpVector.Sum())).Reverse()
                .ToArray();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ArnaudLegouxMovingAverage" /> class.
        /// </summary>
        /// <param name="name">string - a name for the indicator</param>
        /// <param name="period">int - the number of periods to calculate the ALMA.</param>
        public ArnaudLegouxMovingAverage(string name, int period)
            : this(name, period, 6)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ArnaudLegouxMovingAverage" /> class.
        /// </summary>
        /// <param name="period">int - the number of periods to calculate the ALMA</param>
        /// <param name="sigma">
        /// int - this parameter is responsible for the shape of the curve coefficients. It affects the weight
        /// vector kurtosis.
        /// </param>
        /// <param name="offset">
        /// decimal -  This parameter allows regulating the smoothness and high sensitivity of the Moving
        /// Average. The range for this parameter is [0, 1]. It affects the weight vector skewness.
        /// </param>
        public ArnaudLegouxMovingAverage(int period, int sigma, decimal offset = 0.85m)
            : this($"ALMA({period},{sigma},{offset})", period, sigma, offset)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ArnaudLegouxMovingAverage" /> class.
        /// </summary>
        /// <param name="period">int - the number of periods to calculate the ALMA.</param>
        public ArnaudLegouxMovingAverage(int period)
            : this(period, 6)
        {
        }

        /// <summary>
        /// Computes the next value for this indicator from the given state.
        /// </summary>
        /// <param name="window">The window of data held in this indicator</param>
        /// <param name="input">The input value to this indicator on this time step</param>
        /// <returns>
        /// A new value for this indicator
        /// </returns>
        /// <exception cref="System.NotImplementedException"></exception>
        protected override decimal ComputeNextValue(IReadOnlyWindow<IndicatorDataPoint> window,
            IndicatorDataPoint input)
        {
            return IsReady 
                ? window.Select((t, i) => t.Price * _weightVector[i]).Sum()
                : input.Value;
        }
    }
}

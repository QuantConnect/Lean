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
    /// This indicator creates a moving average (middle band) with an upper band and lower band
    /// fixed at k standard deviations above and below the moving average.
    /// </summary>
    public class BollingerBands : Indicator
    {
        /// <summary>
        /// Gets the type of moving average
        /// </summary>
        public MovingAverageType MovingAverageType { get; private set; }

        /// <summary>
        /// Gets the standard deviation
        /// </summary>
        public IndicatorBase<IndicatorDataPoint> StandardDeviation { get; private set; }

        /// <summary>
        /// Gets the middle bollinger band (moving average)
        /// </summary>
        public IndicatorBase<IndicatorDataPoint> MiddleBand { get; private set; }

        /// <summary>
        /// Gets the upper bollinger band (middleBand + k * stdDev)
        /// </summary>
        public IndicatorBase<IndicatorDataPoint> UpperBand { get; private set; }

        /// <summary>
        /// Gets the lower bollinger band (middleBand - k * stdDev)
        /// </summary>
        public IndicatorBase<IndicatorDataPoint> LowerBand { get; private set; }

        /// <summary>
        /// Initializes a new instance of the BollingerBands class
        /// </summary>
        /// <param name="period">The period of the standard deviation and moving average (middle band)</param>
        /// <param name="k">The number of standard deviations specifying the distance between the middle band and upper or lower bands</param>
        /// <param name="movingAverageType">The type of moving average to be used</param>
        public BollingerBands(int period, decimal k, MovingAverageType movingAverageType = MovingAverageType.Simple)
            : this(string.Format("BB({0},{1})", period, k), period, k, movingAverageType)
        {
        }

        /// <summary>
        /// Initializes a new instance of the BollingerBands class
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="period">The period of the standard deviation and moving average (middle band)</param>
        /// <param name="k">The number of standard deviations specifying the distance between the middle band and upper or lower bands</param>
        /// <param name="movingAverageType">The type of moving average to be used</param>
        public BollingerBands(String name, int period, decimal k, MovingAverageType movingAverageType = MovingAverageType.Simple)
            : base(name)
        {
            MovingAverageType = movingAverageType;
            StandardDeviation = new StandardDeviation(name + "_StandardDeviation", period);
            MiddleBand = movingAverageType.AsIndicator(name + "_MiddleBand", period);
            LowerBand = MiddleBand.Minus(StandardDeviation.Times(k), name + "_LowerBand");
            UpperBand = MiddleBand.Plus(StandardDeviation.Times(k), name + "_UpperBand");
        }

        /// <summary>
        /// Gets a flag indicating when this indicator is ready and fully initialized
        /// </summary>
        public override bool IsReady
        {
            get { return MiddleBand.IsReady && UpperBand.IsReady && LowerBand.IsReady; }
        }

        /// <summary>
        /// Computes the next value of the following sub-indicators from the given state:
        /// StandardDeviation, MiddleBand, UpperBand, LowerBand
        /// </summary>
        /// <param name="input">The input given to the indicator</param>
        /// <returns>The input is returned unmodified.</returns>
        protected override decimal ComputeNextValue(IndicatorDataPoint input)
        {
            StandardDeviation.Update(input);
            MiddleBand.Update(input);
            return input;
        }

        /// <summary>
        /// Resets this indicator and all sub-indicators (StandardDeviation, LowerBand, MiddleBand, UpperBand)
        /// </summary>
        public override void Reset()
        {
            StandardDeviation.Reset();
            MiddleBand.Reset();
            UpperBand.Reset();
            LowerBand.Reset();
            base.Reset();
        }
    }
}

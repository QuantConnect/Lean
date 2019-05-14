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
    /// The Acceleration Bands created by Price Headley plots upper and lower envelope bands around a moving average.
    /// </summary>
    /// <seealso cref="Indicators.IndicatorBase{IBaseDataBar}" />
    public class AccelerationBands : IndicatorBase<IBaseDataBar>, IIndicatorWarmUpPeriodProvider
    {
        private readonly decimal _width;

        /// <summary>
        /// Gets the type of moving average
        /// </summary>
        public MovingAverageType MovingAverageType { get; }

        /// <summary>
        /// Gets the middle acceleration band (moving average)
        /// </summary>
        public IndicatorBase<IndicatorDataPoint> MiddleBand { get; }

        /// <summary>
        /// Gets the upper acceleration band  (High * ( 1 + Width * (High - Low) / (High + Low)))
        /// </summary>
        public IndicatorBase<IndicatorDataPoint> UpperBand { get; }

        /// <summary>
        /// Gets the lower acceleration band  (Low * (1 - Width * (High - Low)/ (High + Low)))
        /// </summary>
        public IndicatorBase<IndicatorDataPoint> LowerBand { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AccelerationBands" /> class.
        /// </summary>
        /// <param name="name">The name of this indicator.</param>
        /// <param name="period">The period of the three moving average (middle, upper and lower band).</param>
        /// <param name="width">A coefficient specifying the distance between the middle band and upper or lower bands.</param>
        /// <param name="movingAverageType">Type of the moving average.</param>
        public AccelerationBands(string name, int period, decimal width,
            MovingAverageType movingAverageType = MovingAverageType.Simple)
            : base(name)
        {
            WarmUpPeriod = period;
            _width = width;
            MovingAverageType = movingAverageType;
            MiddleBand = movingAverageType.AsIndicator(name + "_MiddleBand", period);
            LowerBand = movingAverageType.AsIndicator(name + "_LowerBand", period);
            UpperBand = movingAverageType.AsIndicator(name + "_UpperBand", period);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AccelerationBands" /> class.
        /// </summary>
        /// <param name="period">The period of the three moving average (middle, upper and lower band).</param>
        /// <param name="width">A coefficient specifying the distance between the middle band and upper or lower bands.</param>
        public AccelerationBands(int period, decimal width)
            : this($"ABANDS({period},{width})", period, width)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AccelerationBands" /> class.
        /// </summary>
        /// <param name="period">The period of the three moving average (middle, upper and lower band).</param>
        public AccelerationBands(int period)
            : this(period, 4)
        {
        }

        /// <summary>
        /// Gets a flag indicating when this indicator is ready and fully initialized
        /// </summary>
        public override bool IsReady => MiddleBand.IsReady && LowerBand.IsReady && UpperBand.IsReady;

        /// <summary>
        /// Required period, in data points, for the indicator to be ready and fully initialized.
        /// </summary>
        public int WarmUpPeriod { get; }

        /// <summary>
        /// Resets this indicator to its initial state
        /// </summary>
        public override void Reset()
        {
            base.Reset();
            MiddleBand.Reset();
            LowerBand.Reset();
            UpperBand.Reset();
        }

        /// <summary>
        /// Computes the next value of this indicator from the given state
        /// </summary>
        /// <param name="input">The input given to the indicator</param>
        /// <returns>
        /// A new value for this indicator
        /// </returns>
        protected override decimal ComputeNextValue(IBaseDataBar input)
        {
            var coeff = _width * (input.High - input.Low) / (input.High + input.Low);

            LowerBand.Update(input.Time, input.Low * (1 - coeff));
            UpperBand.Update(input.Time, input.High * (1 + coeff));
            MiddleBand.Update(input.Time, input.Close);

            return MiddleBand;
        }
    }
}
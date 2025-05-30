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
    /// The Smoothed Force Index (SFX) is a composite volatility indicator.
    /// It combines the Average True Range (ATR), Standard Deviation of close prices,
    /// and a moving average of the Standard Deviation to provide a smoother volatility measure.
    /// SFX is designed to filter out noise and help detect changes in volatility regimes.
    /// </summary>
    public class SmoothedForceIndex : TradeBarIndicator, IIndicatorWarmUpPeriodProvider
    {
        ///<summary>
        /// Gets the average true range
        /// </summary>
        public AverageTrueRange AverageTrueRange { get; }

        ///<summary>
        /// Gets the standard deviation
        /// </summary>
        public StandardDeviation StandardDeviation { get; }

        ///<summary>
        /// Gets the moving average standard deviation
        /// </summary>
        public IndicatorBase<IndicatorDataPoint> MovingAverageStandardDeviation { get; }

        /// <summary>
        /// Gets a flag indicating when this indicator is ready and fully initialized
        /// </summary>
        public override bool IsReady => AverageTrueRange.IsReady && StandardDeviation.IsReady && MovingAverageStandardDeviation.IsReady; 

        /// <summary>
        /// Required period, in data points, for the indicator to be ready and fully initialized.
        /// </summary>
        public int WarmUpPeriod { get; }

        /// <summary>
        /// Creates a new Smoothed Force Index (SFX) indicator.
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="atrPeriod">The period used to calculate the Average True Range (ATR)</param>
        /// <param name="stdDevPeriod">The period used to calculate the Standard Deviation of close prices</param>
        /// <param name="stdDevSmoothingPeriod">The period used to smooth the Standard Deviation with a moving average</param>
        /// <param name="maType">The type of moving average used to smooth the Standard Deviation</param>
        public SmoothedForceIndex(string name, int atrPeriod, int stdDevPeriod, int stdDevSmoothingPeriod, MovingAverageType maType = MovingAverageType.Simple)
            : base(name)
        {
            AverageTrueRange = new AverageTrueRange(atrPeriod, MovingAverageType.Wilders);
            StandardDeviation = new StandardDeviation(stdDevPeriod);
            MovingAverageStandardDeviation = maType.AsIndicator($"{name}_{maType}", stdDevSmoothingPeriod).Of(StandardDeviation);

            WarmUpPeriod = Math.Max(AverageTrueRange.WarmUpPeriod, Math.Max(StandardDeviation.WarmUpPeriod, stdDevSmoothingPeriod));
        }

        /// <summary>
        /// Creates a new Smoothed Force Index (SFX) indicator with a default name.
        /// </summary>
        /// <param name="atrPeriod">The period used to calculate the Average True Range (ATR)</param>
        /// <param name="stdDevPeriod">The period used to calculate the Standard Deviation of close prices</param>
        /// <param name="stdDevSmoothingPeriod">The period used to smooth the Standard Deviation with a moving average</param>
        /// <param name="maType">The type of moving average used to smooth the Standard Deviation</param>
        public SmoothedForceIndex(int atrPeriod, int stdDevPeriod, int stdDevSmoothingPeriod, MovingAverageType maType = MovingAverageType.Simple)
            : this($"SFX({atrPeriod},{stdDevPeriod},{stdDevSmoothingPeriod})", atrPeriod, stdDevPeriod, stdDevSmoothingPeriod, maType)
        {
        }

        /// <summary>
        /// Computes the next value of this indicator from the given trade bar input.
        /// </summary>
        /// <param name="input">The input given to the indicator</param>
        /// <returns>The input is returned unmodified.</returns>
        protected override decimal ComputeNextValue(TradeBar input)
        {
            AverageTrueRange.Update(input);
            StandardDeviation.Update(new IndicatorDataPoint(input.EndTime, input.Close));
            MovingAverageStandardDeviation.Update(new IndicatorDataPoint(input.EndTime, StandardDeviation.Current.Value));

            return input.Value;
        }

        /// <summary>
        /// Resets this indicator to its initial state
        /// </summary>
        public override void Reset()
        {
            AverageTrueRange.Reset();
            StandardDeviation.Reset();
            MovingAverageStandardDeviation.Reset();
            base.Reset();
        }
    }
}

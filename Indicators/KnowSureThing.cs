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
    /// This indicator creates a moving average (middle band) with an upper band and lower band
    /// fixed at k standard deviations above and below the moving average.
    /// </summary>
    public class KnowSureThing : Indicator, IIndicatorWarmUpPeriodProvider
    {
        /// <summary>
        /// Gets the type of moving average
        /// </summary>
        public MovingAverageType MovingAverageType { get; }

        /// <summary>
        /// Gets the Rate of Change 1
        /// </summary>
        public RateOfChange ROC1 { get; }

        /// <summary>
        /// Gets the Rate of Change 2
        /// </summary>
        public RateOfChange ROC2 { get; }

        /// <summary>
        /// Gets the Rate of Change 3
        /// </summary>
        public RateOfChange ROC3 { get; }

        /// <summary>
        /// Gets the Rate of Change 4
        /// </summary>
        public RateOfChange ROC4 { get; }

        /// <summary>
        /// Gets the smoothed value of Rate of Change 1
        /// </summary>
        public IndicatorBase<IndicatorDataPoint> ROC1MA { get; }

        /// <summary>
        /// Gets the smoothed value of Rate of Change 2
        /// </summary>
        public IndicatorBase<IndicatorDataPoint> ROC2MA { get; }

        /// <summary>
        /// Gets the smoothed value of Rate of Change 3
        /// </summary>
        public IndicatorBase<IndicatorDataPoint> ROC3MA { get; }

        /// <summary>
        /// Gets the smoothed value of Rate of Change 4
        /// </summary>
        public IndicatorBase<IndicatorDataPoint> ROC4MA { get; }

        /// <summary>
        /// Gets the signal line
        /// </summary>
        public IndicatorBase<IndicatorDataPoint> SignalLine { get; }

        /// <summary>
        /// Gets a flag indicating when this indicator is ready and fully initialized
        /// </summary>
        public override bool IsReady => ROC1MA.IsReady && ROC2MA.IsReady && ROC3MA.IsReady && ROC4MA.IsReady && SignalLine.IsReady;

        /// <summary>
        /// Required period, in data points, for the indicator to be ready and fully initialized.
        /// </summary>
        public int WarmUpPeriod { get; }

        /// <summary>
        /// Initializes a new instance of the KnowSureThing class
        /// </summary>
        /// <param name="roc1Period">The period over which to compute ROC1</param>
        /// <param name="roc1MaPeriod">The smoothing period used to smooth the computed ROC1 values</param>
        /// <param name="roc2Period">The period over which to compute ROC2</param>
        /// <param name="roc2MaPeriod">The smoothing period used to smooth the computed ROC2 values</param>
        /// <param name="roc3Period">The period over which to compute ROC3</param>
        /// <param name="roc3MaPeriod">The smoothing period used to smooth the computed ROC3 values</param>
        /// <param name="roc4Period">The period over which to compute ROC4</param>
        /// <param name="roc4MaPeriod">The smoothing period used to smooth the computed ROC4 values</param>
        /// <param name="signalPeriod">The smoothing period used to smooth the signal values</param>
        /// <param name="movingAverageType">Specifies the type of moving average to be used as smoother for KnowSureThing values</param>
        public KnowSureThing(int roc1Period, int roc1MaPeriod, int roc2Period, int roc2MaPeriod,
            int roc3Period, int roc3MaPeriod, int roc4Period, int roc4MaPeriod, int signalPeriod,
            MovingAverageType movingAverageType = MovingAverageType.Simple)
            : this($"KST({roc1Period},{roc1MaPeriod},{roc2Period},{roc2MaPeriod},{roc3Period},{roc3MaPeriod},{roc4Period},{roc4MaPeriod},{signalPeriod},{movingAverageType})", roc1Period, roc1MaPeriod, roc2Period, roc2MaPeriod, roc3Period, roc3MaPeriod, roc4Period, roc4MaPeriod, signalPeriod, movingAverageType)
        {
        }

        /// <summary>
        /// Initializes a new instance of the KnowSureThing class
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="roc1Period">The period over which to compute ROC1</param>
        /// <param name="roc1MaPeriod">The smoothing period used to smooth the computed ROC1 values</param>
        /// <param name="roc2Period">The period over which to compute ROC2</param>
        /// <param name="roc2MaPeriod">The smoothing period used to smooth the computed ROC2 values</param>
        /// <param name="roc3Period">The period over which to compute ROC3</param>
        /// <param name="roc3MaPeriod">The smoothing period used to smooth the computed ROC3 values</param>
        /// <param name="roc4Period">The period over which to compute ROC4</param>
        /// <param name="roc4MaPeriod">The smoothing period used to smooth the computed ROC4 values</param>
        /// <param name="signalPeriod">The smoothing period used to smooth the signal values</param>
        /// <param name="movingAverageType">Specifies the type of moving average to be used as smoother for KnowSureThing values</param>
        public KnowSureThing(string name, int roc1Period, int roc1MaPeriod, int roc2Period, int roc2MaPeriod,
            int roc3Period, int roc3MaPeriod, int roc4Period, int roc4MaPeriod, int signalPeriod,
            MovingAverageType movingAverageType = MovingAverageType.Simple)
            : base(name)
        {
            WarmUpPeriod = (new int[] {roc1Period + roc1MaPeriod, roc2Period + roc2MaPeriod, roc3Period + roc3MaPeriod, roc4Period + roc4MaPeriod }).Max();

            MovingAverageType = movingAverageType;

            ROC1 = new RateOfChange(roc1Period);
            ROC2 = new RateOfChange(roc2Period);
            ROC3 = new RateOfChange(roc3Period);
            ROC4 = new RateOfChange(roc4Period);

            ROC1MA = movingAverageType.AsIndicator(name + "_ROC1MA", roc1MaPeriod);
            ROC2MA = movingAverageType.AsIndicator(name + "_ROC2MA", roc2MaPeriod);
            ROC3MA = movingAverageType.AsIndicator(name + "_ROC3MA", roc3MaPeriod);
            ROC4MA = movingAverageType.AsIndicator(name + "_ROC4MA", roc4MaPeriod);

            SignalLine = movingAverageType.AsIndicator(name + "_SignalLine", signalPeriod);
        }

        /// <summary>
        /// Computes the next value of this indicator from the given state
        /// </summary>
        /// <param name="input">The input given to the indicator</param>
        /// <returns>The next value of the KST based on input.</returns>
        protected override decimal ComputeNextValue(IndicatorDataPoint input)
        {
            ROC1.Update(input);
            ROC2.Update(input);
            ROC3.Update(input);
            ROC4.Update(input);

            if (ROC1.IsReady)
            {
                ROC1MA.Update(input.EndTime, 100 * ROC1.Current.Value);
            }
            if (ROC2.IsReady)
            {
                ROC2MA.Update(input.EndTime, 100 * ROC2.Current.Value);
            }
            if (ROC3.IsReady)
            {
                ROC3MA.Update(input.EndTime, 100 * ROC3.Current.Value);
            }
            if (ROC4.IsReady)
            {
                ROC4MA.Update(input.EndTime, 100 * ROC4.Current.Value);
            }

            var kst = ROC1MA.Current.Value + 2 * ROC2MA.Current.Value + 3 * ROC3MA.Current.Value + 4 * ROC4MA.Current.Value;

            SignalLine.Update(input.EndTime, kst);

            if (!SignalLine.IsReady)
            {
                return 0m;
            }

            return kst;
        }

        /// <summary>
        /// Resets this indicator and all sub-indicators
        /// </summary>
        public override void Reset()
        {
            ROC1.Reset();
            ROC2.Reset();
            ROC3.Reset();
            ROC4.Reset();
            ROC1MA.Reset();
            ROC2MA.Reset();
            ROC3MA.Reset();
            ROC4MA.Reset();
            SignalLine.Reset();
            base.Reset();
        }
    }
}

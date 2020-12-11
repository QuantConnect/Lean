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
    /// The Chaikin Money Flow Index (CMF) is a volume-weighted average of accumulation and distribution over
    /// a specified period.
    ///
    /// CMF = n-day Sum of [(((C - L) - (H - C)) / (H - L)) x Vol] / n-day Sum of Vol
    ///
    /// Where:
    /// n = number of periods, typically 21
    /// H = high
    /// L = low
    /// C = close
    /// Vol = volume
    /// 
    /// https://www.fidelity.com/learning-center/trading-investing/technical-analysis/technical-indicator-guide/cmf
    /// </summary>
    public class ChaikinMoneyFlow : TradeBarIndicator, IIndicatorWarmUpPeriodProvider
    {
        /// <summary>
        /// Holds the point-wise flow-sum and volume terms. 
        /// </summary>
        private readonly Sum _flowRatioSum;

        private readonly Sum _volumeSum;

        /// <summary>
        /// Gets a flag indicating when this indicator is ready and fully initialized
        /// </summary>
        public override bool IsReady => _flowRatioSum.IsReady;

        /// <summary>
        /// Required period, in data points, for the indicator to be ready and fully initialized.
        /// </summary>
        public int WarmUpPeriod { get; }

        /// <summary>
        /// Resets this indicator to its initial state
        /// </summary>
        public override void Reset()
        {
            _volumeSum.Reset();
            _flowRatioSum.Reset();
            base.Reset();
        }

        /// <summary>
        /// Initializes a new instance of the ChaikinMoneyFlow class
        /// </summary>
        /// <param name="name">A name for the indicator</param>
        /// <param name="period">The period over which to perform computation</param>
        public ChaikinMoneyFlow(string name, int period)
            : base($"CMF({name})")
        {
            WarmUpPeriod = period;
            _flowRatioSum = new Sum(period);
            _volumeSum = new Sum(period);
        }

        /// <summary>
        /// Computes the next value for this indicator from the given state.
        /// </summary>
        /// <param name="input">The input value to this indicator on this time step</param>
        /// <returns>A new value for this indicator</returns>
        protected override decimal ComputeNextValue(TradeBar input)
        {
            var denominator = (input.High - input.Low);
            var flowRatio = denominator > 0
                ? input.Volume * (input.Close - input.Low - (input.High - input.Close)) / denominator
                : 0m;

            _flowRatioSum.Update(input.EndTime, flowRatio);
            _volumeSum.Update(input.EndTime, input.Volume);

            return !IsReady || _volumeSum == 0m ? 0m : _flowRatioSum / _volumeSum;
        }
    }
}
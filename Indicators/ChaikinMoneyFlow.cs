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

using System.Collections.Generic;
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
    /// </summary>
    public class ChaikinMoneyFlow : WindowIndicator<TradeBar>, IIndicatorWarmUpPeriodProvider
    {
        /// <summary>
        /// Holds the point-wise flow and volume terms. 
        /// </summary>
        private readonly RollingWindow<List<decimal>> _rollingFlow;


        /// <summary>
        /// Initializes a new instance of the ChaikinMoneyFlow class
        /// </summary>
        /// <param name="name">A name for the indicator</param>
        /// <param name="period">The period over which to perform computation</param>
        public ChaikinMoneyFlow(string name, int period)
            : base($"CMF({name})", period)
        {
            WarmUpPeriod = period + 1;
            _rollingFlow = new RollingWindow<List<decimal>>(period);
        }


        /// <summary>
        /// Gets a flag indicating when this indicator is ready and fully initialized
        /// </summary>
        public override bool IsReady => Samples > Period;

        /// <summary>
        /// Required period, in data points, for the indicator to be ready and fully initialized.
        /// </summary>
        public int WarmUpPeriod { get; }

        /// <summary>
        /// Resets this indicator to its initial state
        /// </summary>
        public override void Reset()
        {
            _rollingFlow.Reset();
            base.Reset();
        }

        /// <summary>
        /// Computes the next value for this indicator from the given state.
        /// </summary>
        /// <param name="window">The window of data held in this indicator</param>
        /// <param name="input">The input value to this indicator on this time step</param>
        /// <returns>A new value for this indicator</returns>
        protected override decimal ComputeNextValue(IReadOnlyWindow<TradeBar> window, TradeBar input)
        {
            var denominator = (input.High - input.Low);
            var flowRatio = denominator > 0
                ? input.Volume * (input.Close - input.Low - (input.High - input.Close)) / denominator : 0m;
            var inputVol = input.Volume;
            var addFlow = new List<decimal> {flowRatio, inputVol};
            _rollingFlow.Add(addFlow);

            if (IsReady)
            {
                var newFlow = 0m;
                var newVol = 0m;

                foreach (var flowTerm in _rollingFlow)
                {
                    newFlow += flowTerm[0];
                    newVol += flowTerm[1];
                }


                return newVol > 0m ? newFlow / newVol : 0m;
            }

            return 0m;
        }
    }
}

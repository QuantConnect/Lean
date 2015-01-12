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
    /// This indicator computes the n-period change in a value using the following:
    /// (value_0 - value_n)/(sma_n)
    /// 
    /// Due to the scaling of this indicator's output, if the sma_n is equal to zero, then
    /// zero will be returned
    /// </summary>
    public class MomentumPercent : Momentum
    {
        /// <summary>
        /// Gets the average used in the denominator to scale the momentum into a percent change
        /// </summary>
        public SimpleMovingAverage Average { get; private set; }

        /// <summary>
        /// Creates a new MomentumPercent indicator with the specified period
        /// </summary>
        /// <param name="period">The period over which to perform to computation</param>
        public MomentumPercent(int period)
            : this("MOM%" + period, period)
        {
        }

        /// <summary>
        /// Creates a new MomentumPercent indicator with the specified period
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="period">The period over which to perform to computation</param>
        public MomentumPercent(string name, int period)
            : base(name, period)
        {
            Average = new SimpleMovingAverage(period);
        }

        /// <summary>
        /// Computes the next value for this indicator from the given state.
        /// </summary>
        /// <param name="window">The window of data held in this indicator</param>
        /// <param name="input">The input value to this indicator on this time step</param>
        /// <returns>A new value for this indicator</returns>
        protected override decimal ComputeNextValue(IReadOnlyWindow<IndicatorDataPoint> window, IndicatorDataPoint input)
        {
            Average.Update(input);
            var absoluteChange = base.ComputeNextValue(window, input);

            if (Average == 0m)
            {
                return 0m;
            }

            return absoluteChange/Average;
        }

        /// <summary>
        /// Resets this indicator to its initial state
        /// </summary>
        public override void Reset()
        {
            Average.Reset();
            base.Reset();
        }
    }
}
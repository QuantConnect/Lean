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


using QuantConnect.Data;

namespace QuantConnect.Indicators
{
    /// <summary>
    /// This indicator is capable of wiring up two separate indicators into a single indicator
    /// such that data will be pumped into the First, and the output of the First will be pumped
    /// into the Second, after the First IsReady
    /// </summary>
    public class SequentialIndicator<TFirst> : IndicatorBase<TFirst>
        where TFirst : BaseData
    {
        /// <summary>
        /// Gets the first indicator to receive data
        /// </summary>
        public IndicatorBase<TFirst> First { get; private set; }

        /// <summary>
        /// Gets the second indicator that receives the output from the first as its input data
        /// </summary>
        public IndicatorBase<IndicatorDataPoint> Second { get; private set; }

        /// <summary>
        /// Gets a flag indicating when this indicator is ready and fully initialized
        /// </summary>
        public override bool IsReady
        {
            get { return Second.IsReady && First.IsReady; }
        }

        /// <summary>
        /// Creates a new SequentialIndicator that will pipe the output of the first into the second
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="first">The first indicator to receive data</param>
        /// <param name="second">The indicator to receive the first's output data</param>
        public SequentialIndicator(string name, IndicatorBase<TFirst> first, IndicatorBase<IndicatorDataPoint> second)
            : base(name)
        {
            First = first;
            Second = second;
        }

        /// <summary>
        /// Creates a new SequentialIndicator that will pipe the output of the first into the second
        /// </summary>
        /// <param name="first">The first indicator to receive data</param>
        /// <param name="second">The indicator to receive the first's output data</param>
        public SequentialIndicator(IndicatorBase<TFirst> first, IndicatorBase<IndicatorDataPoint> second)
            : base(string.Format("SEQUENTIAL({0}->{1})", first.Name, second.Name))
        {
            First = first;
            Second = second;
        }

        /// <summary>
        /// Computes the next value of this indicator from the given state
        /// </summary>
        /// <param name="input">The input given to the indicator</param>
        /// <returns>A new value for this indicator</returns>
        protected override decimal ComputeNextValue(TFirst input)
        {
            First.Update(input);
            if (!First.IsReady)
            {
                // if the first isn't ready just send out a default value
                return 0m;
            }

            Second.Update(First.Current);
            return Second.Current.Value;
        }

        /// <summary>
        /// Resets this indicator to its initial state
        /// </summary>
        public override void Reset()
        {
            First.Reset();
            Second.Reset();
            base.Reset();
        }
    }
}
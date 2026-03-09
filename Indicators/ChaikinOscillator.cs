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
    /// This class is an alias for AccumulationDistributionOscillator (also known as Chaikin Oscillator).
    /// </summary>
    public class ChaikinOscillator : AccumulationDistributionOscillator
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ChaikinOscillator"/> class using the specified parameters
        /// </summary> 
        /// <param name="fastPeriod">The fast moving average period</param>
        /// <param name="slowPeriod">The slow moving average period</param>
        public ChaikinOscillator(int fastPeriod, int slowPeriod)
            : base($"ChaikinOscillator({fastPeriod},{slowPeriod})", fastPeriod, slowPeriod)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ChaikinOscillator"/> class with a custom name
        /// </summary> 
        /// <param name="name">The name of this indicator</param>
        /// <param name="fastPeriod">The fast moving average period</param>
        /// <param name="slowPeriod">The slow moving average period</param>
        public ChaikinOscillator(string name, int fastPeriod, int slowPeriod)
            : base(name, fastPeriod, slowPeriod)
        {
        }
    }
}
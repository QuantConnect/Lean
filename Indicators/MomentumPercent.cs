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
    /// (value_0 - value_n)/(value_n)
    /// 
    /// This indicator yeilds the same results of RateOfChange
    /// </summary>
    public class MomentumPercent : RateOfChange
    {
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

        }
    }
}
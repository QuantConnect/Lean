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
using QuantConnect.Data;

namespace QuantConnect.Indicators
{
    /// <summary>
    /// The functional indicator is used to lift any function into an indicator. This can be very useful
    /// when trying to combine output of several indicators, or for expression a mathematical equation
    /// </summary>
    /// <typeparam name="T">The input type for this indicator</typeparam>
    public class FunctionalIndicator<T> : IndicatorBase<T>
        where T : IBaseData
    {
        /// <summary>function implementation of the IndicatorBase.IsReady property</summary>
        private readonly Func<IndicatorBase<T>, bool> _isReady;
        /// <summary>Action used to reset this indicator completely along with any indicators this one is dependent on</summary>
        private readonly Action _reset;
        /// <summary>function implementation of the IndicatorBase.ComputeNextValue method</summary>
        private readonly Func<T, decimal> _computeNextValue;

        /// <summary>
        /// Creates a new FunctionalIndicator using the specified functions as its implementation.
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="computeNextValue">A function accepting the input value and returning this indicator's output value</param>
        /// <param name="isReady">A function accepting this indicator and returning true if the indicator is ready, false otherwise</param>
        public FunctionalIndicator(string name, Func<T, decimal> computeNextValue, Func<IndicatorBase<T>, bool> isReady)
            : base(name)
        {
            _computeNextValue = computeNextValue;
            _isReady = isReady;
        }

        /// <summary>
        /// Creates a new FunctionalIndicator using the specified functions as its implementation.
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="computeNextValue">A function accepting the input value and returning this indicator's output value</param>
        /// <param name="isReady">A function accepting this indicator and returning true if the indicator is ready, false otherwise</param>
        /// <param name="reset">Function called to reset this indicator and any indicators this is dependent on</param>
        public FunctionalIndicator(string name, Func<T, decimal> computeNextValue, Func<IndicatorBase<T>, bool> isReady, Action reset)
            : base(name)
        {
            _computeNextValue = computeNextValue;
            _isReady = isReady;
            _reset = reset;
        }

        /// <summary>
        /// Gets a flag indicating when this indicator is ready and fully initialized
        /// </summary>
        public override bool IsReady
        {
            get { return _isReady(this); }
        }

        /// <summary>
        /// Computes the next value of this indicator from the given state
        /// </summary>
        /// <param name="input">The input given to the indicator</param>
        /// <returns>A new value for this indicator</returns>
        protected override decimal ComputeNextValue(T input)
        {
            return _computeNextValue(input);
        }

        /// <summary>
        /// Resets this indicator to its initial state, optionally using the reset action passed via the constructor
        /// </summary>
        public override void Reset()
        {
            if (_reset != null)
            {
                // if a reset function was specified then use that
                _reset.Invoke();
            }
            base.Reset();
        }
    }
}
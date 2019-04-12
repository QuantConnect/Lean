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
    /// Represents an indicator that acts on a rolling window of data
    /// </summary>
    public abstract class WindowIndicator<T> : IndicatorBase<T>
        where T : IBaseData
    {
        // a window of data over a certain look back period
        private readonly RollingWindow<T> _window;

        /// <summary>
        /// Gets the period of this window indicator
        /// </summary>
        public int Period => _window.Size;

        /// <summary>
        /// Initializes a new instance of the WindowIndicator class
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="period">The number of data points to hold in the window</param>
        protected WindowIndicator(string name, int period)
            : base(name)
        {
            _window = new RollingWindow<T>(period);
        }

        /// <summary>
        /// Gets a flag indicating when this indicator is ready and fully initialized
        /// </summary>
        public override bool IsReady => _window.IsReady;

        /// <summary>
        /// Computes the next value of this indicator from the given state
        /// </summary>
        /// <param name="input">The input given to the indicator</param>
        /// <returns>A new value for this indicator</returns>
        protected override decimal ComputeNextValue(T input)
        {
            _window.Add(input);
            return ComputeNextValue(_window, input);
        }

        /// <summary>
        ///     Resets this indicator to its initial state
        /// </summary>
        public override void Reset()
        {
            base.Reset();
            _window.Reset();
        }

        /// <summary>
        /// Computes the next value for this indicator from the given state.
        /// </summary>
        /// <param name="window">The window of data held in this indicator</param>
        /// <param name="input">The input value to this indicator on this time step</param>
        /// <returns>A new value for this indicator</returns>
        protected abstract decimal ComputeNextValue(IReadOnlyWindow<T> window, T input);
    }
}
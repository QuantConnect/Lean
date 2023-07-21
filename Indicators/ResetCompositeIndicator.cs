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

namespace QuantConnect.Indicators
{
    /// <summary>
    /// Class that extends CompositeIndicator to execute a given action once is reset
    /// </summary>
    public class ResetCompositeIndicator : CompositeIndicator
    {
        /// <summary>
        /// Action to execute once this indicator is reset
        /// </summary>
        private Action _extraResetAction;

        /// <summary>
        /// Creates a new ResetCompositeIndicator capable of taking the output from the left and right indicators
        /// and producing a new value via the composer delegate specified
        /// </summary>
        /// <param name="left">The left indicator for the 'composer'</param>
        /// <param name="right">The right indicator for the 'composer'</param>
        /// <param name="composer">Function used to compose the left and right indicators</param>
        /// <param name="extraResetAction">Action to execute once the composite indicator is reset</param>
        public ResetCompositeIndicator(IndicatorBase left, IndicatorBase right, IndicatorComposer composer, Action extraResetAction)
            : this($"RESET_COMPOSE({left.Name},{right.Name})", left, right, composer, extraResetAction)
        {
        }

        /// <summary>
        /// Creates a new CompositeIndicator capable of taking the output from the left and right indicators
        /// and producing a new value via the composer delegate specified
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="left">The left indicator for the 'composer'</param>
        /// <param name="right">The right indicator for the 'composer'</param>
        /// <param name="composer">Function used to compose the left and right indicators</param>
        /// <param name="extraResetAction">Action to execute once the indicator is reset</param>
        public ResetCompositeIndicator(string name, IndicatorBase left, IndicatorBase right, IndicatorComposer composer, Action extraResetAction)
            : base(name, left, right, composer)
        {
            _extraResetAction = extraResetAction;
        }

        /// <summary>
        /// Resets this indicator and invokes the given reset action
        /// </summary>
        public override void Reset()
        {
            base.Reset();
            _extraResetAction.Invoke();
        }
    }
}

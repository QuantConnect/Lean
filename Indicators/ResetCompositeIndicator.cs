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
using System;

namespace QuantConnect.Indicators
{
    /// <summary>
    /// Class that extends CompositeIndicator to execute a given action once is reset
    /// </summary>
    public class ResetCompositeIndicator : IndicatorBase
    {
        /// <summary>
        /// Action to execute once the given CompositeIndicator is reset
        /// </summary>
        private Action _extraResetAction;

        /// <summary>
        /// The CompositeIndicator that will fire the given action once is reset
        /// </summary>
        private CompositeIndicator _compositeIndicator;

        /// <summary>
        /// Gets current value of the given CompositeIndicator
        /// </summary>
        public IndicatorDataPoint Current => _compositeIndicator.Current;

        /// <summary>
        /// Gets a flag indicating when the given CompositeIndicator is ready
        /// </summary>
        public override bool IsReady => _compositeIndicator.IsReady;

        /// <summary>
        /// Creates a new ResetCompositeIndicator with the given CompositeIndicator and Action
        /// </summary>
        /// <param name="compositeIndicator">The CompositeIndicator that will fire the given action once is reset</param>
        /// <param name="extraResetAction">Action to be executed once the given CompositeIndicator is reset</param>
        public ResetCompositeIndicator(CompositeIndicator compositeIndicator, Action extraResetAction)
        {
            _extraResetAction = extraResetAction;
            _compositeIndicator = compositeIndicator;
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// </summary>
        /// <returns>
        /// true if the specified object  is equal to the current object; otherwise, false.
        /// </returns>
        /// <param name="obj">The object to compare with the current object. </param>
        public override bool Equals(object obj)
        {
            return _compositeIndicator.Equals(obj);
        }

        /// <summary>
        /// Resets the given CompositeIndicator and invokes the given action
        /// </summary>
        public override void Reset()
        {
            _compositeIndicator.Reset();
            _extraResetAction.Invoke();
        }

        /// <summary>
        /// Updates the given CompositeIndicator
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public override bool Update(IBaseData input)
        {
            return _compositeIndicator.Update(input);
        }
    }
}

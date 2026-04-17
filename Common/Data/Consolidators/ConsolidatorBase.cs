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

namespace QuantConnect.Data.Consolidators
{
    /// <summary>
    /// Provides a base implementation for consolidators, including a built-in rolling window
    /// that stores the history of consolidated bars.
    /// </summary>
    public abstract class ConsolidatorBase : WindowBase<IBaseData>
    {
        /// <summary>
        /// Gets the most recently consolidated piece of data. This will be null if this consolidator
        /// has not produced any data yet. Setting this property adds the value to the rolling window.
        /// </summary>
        public IBaseData Consolidated
        {
            get
            {
                return Window.Count > 0 ? Window[0] : null;
            }
            protected set
            {
                Window.Add(value);
            }
        }

        /// <summary>
        /// Resets this consolidator, clearing consolidated data and the rolling window.
        /// </summary>
        public virtual void Reset()
        {
            ResetWindow();
        }
    }
}

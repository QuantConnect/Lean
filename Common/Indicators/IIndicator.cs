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
    /// KEEPING THIS INTERFACE FOR BACKWARDS COMPATIBILITY.
    /// Represents an indicator that can receive data updates and emit events when the value of
    /// the indicator has changed.
    /// </summary>
    public interface IIndicator<T> : IComparable<IIndicator<T>>, IComparable, IIndicator
        where T : IBaseData
    {
    }

    /// <summary>
    /// Represents an indicator that can receive data updates and emit events when the value of
    /// the indicator has changed.
    /// </summary>
    public interface IIndicator
    {
        /// <summary>
        /// Event handler that fires after this indicator is updated
        /// </summary>
        event IndicatorUpdatedHandler Updated;

        /// <summary>
        /// Gets a name for this indicator
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets a flag indicating when this indicator is ready and fully initialized
        /// </summary>
        bool IsReady { get; }

        /// <summary>
        /// Gets the current state of this indicator. If the state has not been updated
        /// then the time on the value will equal DateTime.MinValue.
        /// </summary>
        IndicatorDataPoint Current { get; }

        /// <summary>
        /// Gets the number of samples processed by this indicator
        /// </summary>
        long Samples { get; }

        /// <summary>
        /// Updates the state of this indicator with the given value and returns true
        /// if this indicator is ready, false otherwise
        /// </summary>
        /// <param name="input">The value to use to update this indicator</param>
        /// <returns>True if this indicator is ready, false otherwise</returns>
        bool Update(IBaseData input);

        /// <summary>
        /// Resets this indicator to its initial state
        /// </summary>
        void Reset();
    }
}
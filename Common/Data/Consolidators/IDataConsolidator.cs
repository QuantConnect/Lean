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

namespace QuantConnect.Data.Consolidators
{
    /// <summary>
    /// Event handler type for the IDataConsolidator.DataConsolidated event
    /// </summary>
    /// <param name="sender">The consolidator that fired the event</param>
    /// <param name="consolidated">The consolidated piece of data</param>
    public delegate void DataConsolidatedHandler(object sender, IBaseData consolidated);

    /// <summary>
    /// Represents a type capable of taking BaseData updates and firing events containing new
    /// 'consolidated' data. These types can be used to produce larger bars, or even be used to
    /// transform the data before being sent to another component. The most common usage of these
    /// types is with indicators.
    /// </summary>
    public interface IDataConsolidator : IDisposable
    {
        /// <summary>
        /// Gets the most recently consolidated piece of data. This will be null if this consolidator
        /// has not produced any data yet.
        /// </summary>
        IBaseData Consolidated { get; }

        /// <summary>
        /// Gets a clone of the data being currently consolidated
        /// </summary>
        IBaseData WorkingData { get; }

        /// <summary>
        /// Gets the type consumed by this consolidator
        /// </summary>
        Type InputType { get; }

        /// <summary>
        /// Gets the type produced by this consolidator
        /// </summary>
        Type OutputType { get; }

        /// <summary>
        /// Updates this consolidator with the specified data
        /// </summary>
        /// <param name="data">The new data for the consolidator</param>
        void Update(IBaseData data);

        /// <summary>
        /// Scans this consolidator to see if it should emit a bar due to time passing
        /// </summary>
        /// <param name="currentLocalTime">The current time in the local time zone (same as <see cref="BaseData.Time"/>)</param>
        void Scan(DateTime currentLocalTime);

        /// <summary>
        /// Event handler that fires when a new piece of data is produced
        /// </summary>
        event DataConsolidatedHandler DataConsolidated;
    }
}
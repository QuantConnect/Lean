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
    /// Base class for consolidators registered on behalf of another one, adapting the subscription data
    /// before forwarding it. Users hold the wrapped consolidator, so the adapter equals it and shares its
    /// hash code, and <see cref="ConsolidatorIdentityComparer"/> treats both as the same registration.
    /// </summary>
    public abstract class ConsolidatorAdapterBase : IDataConsolidator
    {
        /// <summary>
        /// The wrapped consolidator the user registered
        /// </summary>
        public IDataConsolidator Consolidator { get; }

        /// <summary>
        /// Gets the most recently consolidated piece of data produced by the wrapped consolidator
        /// </summary>
        public IBaseData Consolidated => Consolidator.Consolidated;

        /// <summary>
        /// Gets a clone of the data being currently consolidated by the wrapped consolidator
        /// </summary>
        public IBaseData WorkingData => Consolidator.WorkingData;

        /// <summary>
        /// Gets the type consumed by this adapter
        /// </summary>
        public abstract Type InputType { get; }

        /// <summary>
        /// Gets the type produced by the wrapped consolidator
        /// </summary>
        public Type OutputType => Consolidator.OutputType;

        /// <summary>
        /// Event handler that fires when the wrapped consolidator produces a new piece of data
        /// </summary>
        public event DataConsolidatedHandler DataConsolidated
        {
            add { Consolidator.DataConsolidated += value; }
            remove { Consolidator.DataConsolidated -= value; }
        }

        /// <summary>
        /// Creates a new adapter for the given consolidator
        /// </summary>
        /// <param name="consolidator">The consolidator to adapt</param>
        protected ConsolidatorAdapterBase(IDataConsolidator consolidator)
        {
            Consolidator = consolidator;
        }

        /// <summary>
        /// Adapts the specified data and updates the wrapped consolidator with it
        /// </summary>
        /// <param name="data">The new data for the adapter</param>
        public abstract void Update(IBaseData data);

        /// <summary>
        /// Scans the wrapped consolidator to see if it should emit a bar due to time passing
        /// </summary>
        /// <param name="currentLocalTime">The current time in the local time zone (same as <see cref="BaseData.Time"/>)</param>
        public void Scan(DateTime currentLocalTime)
        {
            Consolidator.Scan(currentLocalTime);
        }

        /// <summary>
        /// Resets the wrapped consolidator
        /// </summary>
        public void Reset()
        {
            Consolidator.Reset();
        }

        /// <summary>
        /// Disposes the wrapped consolidator
        /// </summary>
        public void Dispose()
        {
            Consolidator.Dispose();
        }

        /// <summary>
        /// The adapter equals the consolidator it wraps, and anything that consolidator equals,
        /// such as the python object a <see cref="Python.DataConsolidatorPythonWrapper"/> was created from
        /// </summary>
        /// <param name="obj">The object to compare with</param>
        /// <returns>True if the object is this adapter or equals the wrapped consolidator</returns>
        public override bool Equals(object obj)
        {
            return ReferenceEquals(this, obj) || Consolidator.Equals(obj);
        }

        /// <summary>
        /// The hash code of the wrapped consolidator, consistent with <see cref="Equals(object)"/>
        /// </summary>
        /// <returns>The wrapped consolidator hash code</returns>
        public override int GetHashCode()
        {
            return Consolidator.GetHashCode();
        }
    }
}
